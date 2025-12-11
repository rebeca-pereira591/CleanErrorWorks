CleanErrorWorks — modular error handling, logging, and OpenTelemetry telemetry
==============================================================================

1. Overview
-----------

CleanErrorWorks brings together three complementary pillars for .NET services:

- **Errors** — strongly typed contracts, ASP.NET Core exception mapping, and ProblemDetails formatting so every API responds consistently.
- **Logging** — sensible conventions (providers, minimum levels, correlation) that give teams structured logs out of the box.
- **Observability** — OpenTelemetry wiring plus multi-backend exporters that can be toggled from configuration.

`sample/Demo.Api` stitches the pillars together. It is intentionally minimal: `Program.cs` wires the DI extensions and exposes endpoints you can hit to trigger validation failures, authorization errors, SQL exceptions, and success paths. Use it as a regression playground while iterating locally.


2. Project structure and responsibilities
-----------------------------------------

| Project | Path | Responsibility | Key APIs |
| --- | --- | --- | --- |
| Errors.Abstractions | `src/Errors.Abstractions` | Contracts (`IAppError`), rich exceptions (e.g., `AuthorizationException`), result helpers. | Typed exception types, shared interfaces. |
| Errors.AspNetCore | `src/Errors.AspNetCore` | ASP.NET Core glue for ProblemDetails, sanitization, classifiers, registry, DI extension `AddErrorHandling`. | `ServiceCollectionExtension.AddErrorHandling`, mappers, formatters. |
| Logging.Core | `src/Logging.Core` | Logging conventions + DI extension `AddDefaultLogging`. Clears providers, applies options, adds console/debug providers plus hooks for custom enrichers. | `LoggingBuilderExtensions.AddDefaultLogging`. |
| Observability.OpenTelemetry | `src/Observability.OpenTelemetry` | OpenTelemetry options, resource attribution, ActivitySources, exporter toggles driven by configuration via `AddDefaultOpenTelemetry`. | `ServiceCollectionExtensions.AddDefaultOpenTelemetry`. |
| Demo.Api | `sample/Demo.Api` | Reference API using local `ProjectReference`s so changes in the libraries are exercised instantly. Hosts example endpoints for every exception mapper. | `Program.cs`, controllers/endpoints, SQL repro service. |

> **Architecture note**: `Demo.Api` references the projects locally (see `Demo.Api.csproj`). When you run `dotnet run` from `sample/Demo.Api`, you always execute the current source without needing to push packages.


3. Error handling: behaviors, mappers y sanitización
----------------------------------------------------

- **Global pipeline**: `Program.cs` calls `builder.Services.AddErrorHandling()` and the middleware pipeline uses `app.UseExceptionHandler()`. This ensures every unhandled exception flows through `GlobalExceptionHandler`, the mapper registry, and the sanitizer before a ProblemDetails JSON is returned.
- **Default mappers**: built-in classes map `ValidationException`, `AuthorizationException`, `NotFoundException`, `SqlException`, `HttpRequestException`, `TimeoutRejectedException`, and domain/unknown fallbacks. See `src/Errors.AspNetCore/Mappers`.
- **Sanitization**: `IExceptionSanitizer` strips sensitive fields (e.g., SQL connection strings) and validates allowed extensions via `ProblemDetailsExtensionsValidator`.
- **Precedence & fallbacks**:
  1. Custom mappers (registered via DI) run first.
  2. Built-in mappers evaluated in registration order.
  3. `AppErrorFallbackMapper` handles domain errors.
  4. `UnknownExceptionMapper` returns a sanitized 500 ProblemDetails with a friendly message (production) or full detail (development).

### Adding a custom mapper

```csharp
using Errors.AspNetCore.Mappers;
using Errors.Abstractions.Exceptions;

public sealed class PaymentDeclinedMapper : ExceptionProblemDetailsMapper<PaymentDeclinedException>
{
    protected override ProblemDetails Map(PaymentDeclinedException exception, HttpContext context)
    {
        return new ProblemDetailsBuilder()
            .WithStatus(StatusCodes.Status402PaymentRequired)
            .WithTitle("Payment declined")
            .WithDetail(exception.Message)
            .WithExtension("reasonCode", exception.ReasonCode)
            .Build();
    }
}
```

Register it next to other services:

```csharp
builder.Services.AddErrorHandling(options =>
{
    options.RegisterMapper<PaymentDeclinedMapper>();
});
```

If multiple mappers target the same exception type, the first registered wins. For domain-specific detail levels, hook `ProblemDetailsExtensionValidationOptions` to allow extra extensions in Development only.


4. Logging conventions
----------------------

- **What gets logged**:
  - Startup: providers are cleared, console + debug are added, minimum level comes from configuration.
  - Requests: ASP.NET Core emits `RequestStarting`/`RequestFinished` including trace IDs when OpenTelemetry is configured.
  - Exceptions: the exception handler logs sanitized summaries, plus mapper decisions when `LogLevel.Debug` or lower.
- **Structured fields**: correlation IDs, `TraceId`, `SpanId`, route template, HTTP status, and mapped error codes if available.
- **Extending logging**:
  - **Custom provider**:
    ```csharp
    builder.Services.AddDefaultLogging(cfg =>
    {
        cfg.Providers.Add(lb => lb.AddSeq("http://localhost:5341"));
    });
    ```
  - **Minimum levels**: update `appsettings*.json` under `Logging:LogLevel`. Example to silence exception handler noise:
    ```json
    {
      "Logging": {
        "LogLevel": {
          "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None"
        }
      }
    }
    ```
  - **Enrichers**: implement an `Action<ILoggingBuilder>` that adds enrichers (e.g., `builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();` then push custom scopes).


5. OpenTelemetry: configuración y exportación a múltiples backends
-------------------------------------------------------------------

- **Options model** (`OpenTelemetryOptions`):
  - `ServiceName`, `ServiceVersion`, `Environment`, `ResourceAttributes`, `ActivitySourceName`.
  - `Exporters` section toggles Console, OTLP, Tempo, Zipkin, Jaeger, and Azure Monitor (requires credentials).
  - `TracingEnrichers` and `MetricsEnrichers` hooks let you add custom tags or meters.
- **Instrumentation**: ASP.NET Core, HttpClient, and runtime instruments are enabled by default. `ActivitySource` instances register via DI so custom libraries can emit spans.
- **Exporters & ports**:

  | Exporter | Endpoint | Default port | Notes |
  | --- | --- | --- | --- |
  | Console | N/A | stdout | For local smoke tests. |
  | OTLP | `http://localhost:4318` | 4318 (HTTP) / 4317 (gRPC) | Feeds the collector (`otelcol-config.yaml`). |
  | Tempo | `http://tempo:4318` | container-only 4318 | Collector forwards to Tempo. |
  | Zipkin | `http://zipkin:9411/api/v2/spans` | 9411 | Works directly or via collector. |
  | Jaeger | `http://jaeger:4318` (HTTP OTLP) / `http://jaeger:14250` (gRPC) | 4318/14250 | Use whichever protocol matches your collector. |

- **Switching backends**: only change `appsettings.json` under `OpenTelemetry:Exporters`. No code edits required.
- **Collector + Docker**: `docker-compose.yml` and `otelcol-config.yaml` ship a ready-to-run stack (Collector, Jaeger, Zipkin, Tempo, Grafana, SQL Server). Run:

```bash
docker compose up -d otel-collector jaeger zipkin tempo grafana
docker logs -f otel-collector
```

  Collector logs (debug exporter) confirm spans are flowing. If you see `dns error: no such host`, check container names and that the services are part of the same compose project.
- **Troubleshooting**:
  - **Missing spans**: ensure `OpenTelemetry:Exporters:ConsoleEnabled` is `true` to double-check local emission, then inspect collector logs.
  - **Connectivity**: verify ports 4317/4318/9411/16686 are free. In WSL, `localhost` hits Windows; use `http://host.docker.internal` if the collector runs in Docker Desktop.
  - **Azure Monitor**: the repo references `Azure.Monitor.OpenTelemetry.Exporter`. If you do not have credentials for the private TeamSoftware feed, temporarily disable that feed (NuGet.Config) or provide a PAT—otherwise `dotnet restore` fails with `NU1301`.
  - **Swagger**: UI lives under `/swagger`. Spans tagged with `http.route = /swagger/index.html`.


6. Demo.Api: integración mínima y endpoints de prueba
----------------------------------------------------

### Program.cs snippet

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddErrorHandling();
builder.Services.AddDefaultLogging(builder.Configuration);
builder.Services.AddDefaultOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();
app.Run();
```

### Sample endpoints

| Route | Description |
| --- | --- |
| `GET /ok` | Healthy response with trace ID. |
| `GET /boom` | Throws generic `Exception` → sanitized 500. |
| `GET /not-found/{id}` | Throws `NotFoundException`. |
| `GET /unauthorized` / `/forbidden` | Authorization scenarios. |
| `GET /validation` | Throws `ValidationException` with field errors. |
| `GET /ext/*` | Invokes `ExternalClient` to exercise resilience handlers. |
| `POST /sql/*` | Triggers SQL unique, FK, timeout, deadlock, unavailable cases. |
| `GET /health` | Simple health check. |

### Running the demo

```bash
pushd sample/Demo.Api
dotnet run
# in another terminal
curl http://localhost:5000/boom
curl http://localhost:5000/validation
```

Watch the console output for logs and spans. If the collector is up, open Jaeger (`http://localhost:16686`) or Zipkin (`http://localhost:9411`) to inspect traces.

### OpenTelemetry configuration (appsettings.json)

```json
{
  "OpenTelemetry": {
    "ServiceName": "Demo.Api",
    "ServiceVersion": "1.0.0",
    "Environment": "Development",
    "Exporters": {
      "ConsoleEnabled": true,
      "Otlp": {
        "Enabled": true,
        "Endpoint": "http://localhost:4318",
        "Protocol": "http/protobuf"
      },
      "Tempo": {
        "Enabled": false,
        "Endpoint": "http://tempo:4318",
        "Protocol": "http/protobuf"
      },
      "Zipkin": {
        "Enabled": false,
        "Endpoint": "http://zipkin:9411/api/v2/spans"
      },
      "Jaeger": {
        "Enabled": false,
        "Endpoint": "http://jaeger:14250"
      }
    }
  }
}
```


7. Configuration guide
----------------------

- **OpenTelemetry keys**:
  - `ServiceName`, `ServiceVersion`, `Environment`: used to populate resource attributes (`service.name`, `service.version`, `deployment.environment`).
  - `Exporters.ConsoleEnabled`: quick toggle to verify instrumentation emits spans/logs.
  - `Exporters.<Name>.Enabled`: enable additional sinks without redeploying.
  - `Exporters.Otlp.Protocol`: `http/protobuf` or `grpc`.
- **Example setups**:
  - Console-only:
    ```json
    { "OpenTelemetry": { "Exporters": { "ConsoleEnabled": true, "Otlp": { "Enabled": false } } } }
    ```
  - Console + Collector:
    ```json
    { "OpenTelemetry": { "Exporters": { "ConsoleEnabled": true, "Otlp": { "Enabled": true, "Endpoint": "http://localhost:4318" } } } }
    ```
  - OTLP + Zipkin:
    ```json
    { "OpenTelemetry": { "Exporters": { "Otlp": { "Enabled": true }, "Zipkin": { "Enabled": true, "Endpoint": "http://zipkin:9411/api/v2/spans" } } } }
    ```
  - OTLP + Jaeger (HTTP):
    ```json
    { "OpenTelemetry": { "Exporters": { "Otlp": { "Enabled": true }, "Jaeger": { "Enabled": true, "Endpoint": "http://jaeger:4318" } } } }
    ```
  - OTLP + Tempo + Grafana:
    ```json
    { "OpenTelemetry": { "Exporters": { "Otlp": { "Enabled": true }, "Tempo": { "Enabled": true, "Endpoint": "http://tempo:4318" } } } }
    ```
- **Environment overrides**:
  - Set `ASPNETCORE_ENVIRONMENT=Production` to automatically sanitize messages and apply production logging defaults.
  - Use `dotnet run --environment=Development` for local debugging.
  - Add `appsettings.{Environment}.json` to override the OpenTelemetry section per environment.


8. Extensibility recipes
------------------------

### New ProblemDetails mapper
1. Create a class inheriting `ExceptionProblemDetailsMapper<TException>`.
2. Override `Map` to build the ProblemDetails.
3. Register it via `AddErrorHandling(options => options.RegisterMapper<MyMapper>())`.

### Logging enricher
```csharp
builder.Services.AddDefaultLogging(cfg =>
{
    cfg.Providers.Add(builder =>
    {
        builder.AddConsole();
        builder.Services.AddSingleton<ILoggerProvider, DomainContextLoggerProvider>();
    });
});
```

Wrap domain IDs inside `using (logger.BeginScope("OrderId:{OrderId}", orderId))` to enrich every log.

### Telemetry enricher
```csharp
builder.Services.AddDefaultOpenTelemetry(builder.Configuration, options =>
{
    options.TracingEnrichers.Add(tpBuilder =>
    {
        tpBuilder.AddSource("MyDomain.ActivitySource");
    });

    options.MetricsEnrichers.Add(mpBuilder =>
    {
        mpBuilder.AddMeter("MyDomain.Metrics");
    });
});
```

### Adding a new exporter
1. Extend `ExporterOptions` with a new section.
2. Update `OpenTelemetryOptions.Exporters` to include defaults.
3. In `ServiceCollectionExtensions.ConfigureTracingExporters` and `ConfigureMetricExporters`, add the exporter when `Enabled` is true.
4. Document the configuration keys in this README.


9. Packaging and versioning strategy (future)
---------------------------------------------

- Active development uses `<ProjectReference>` in `Demo.Api` for fast iteration (no NuGet restore needed beyond third-party packages).
- Once a milestone is ready, run:

```bash
dotnet pack src/Errors.Abstractions/Errors.Abstractions.csproj -c Release
dotnet pack src/Errors.AspNetCore/Errors.AspNetCore.csproj -c Release
dotnet pack src/Logging.Core/Logging.Core.csproj -c Release
dotnet pack src/Observability.OpenTelemetry/Observability.OpenTelemetry.csproj -c Release
```

- Publish packages to your feed (NuGet.org, Azure Artifacts, etc.) and switch `Demo.Api` back to `<PackageReference>` if you need to emulate production consumption.
- `Directory.Packages.props` centralizes package versions to avoid drift across projects.


10. Docker compose quickstart
-----------------------------

`docker-compose.yml` ships a complete playground. The most common workflow:

```bash
docker compose up -d otel-collector jaeger zipkin tempo grafana
# Optional SQL server for the sample database:
docker compose up -d mssql
```

- **UIs**:
  - Jaeger: <http://localhost:16686>
  - Zipkin: <http://localhost:9411>
  - Grafana: <http://localhost:3000> (user/pass `admin` / `admin`)
- **Collector config** (`otelcol-config.yaml` excerpt):

```yaml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318
      grpc:
        endpoint: 0.0.0.0:4317

exporters:
  otlphttp/jaeger:
    endpoint: http://jaeger:4318
  zipkin:
    endpoint: http://zipkin:9411/api/v2/spans
  otlphttp/tempo:
    endpoint: http://tempo:4318
  debug:
    verbosity: detailed

service:
  pipelines:
    traces:
      receivers: [otlp]
      exporters: [otlphttp/jaeger, zipkin, otlphttp/tempo, debug]
```

Observe collector logs (`docker logs otel-collector -f`) for span routing diagnostics.


11. FAQ
-------

- **Why no automated tests yet?** The focus is on delivering the building blocks; once the APIs stabilize we plan to add integration tests for mappers, logging conventions, and OpenTelemetry pipelines.
- **I don’t see any spans in Jaeger/Zipkin.** Confirm `Exporters.ConsoleEnabled` is `true`. If console output shows spans but Jaeger is empty, ensure the collector is running and `Exporters.Otlp.Endpoint` points to the collector (not Jaeger directly unless you intend to).
- **Logs are noisy.** Suppress specific categories (exception handler, gRPC diagnostics) via `Logging:LogLevel`. For example, set `"Grpc.Net.Client": "Warning"`.
- **How do I set `deployment.environment`?** Update `OpenTelemetry:Environment` in configuration or set `OTEL_RESOURCE_ATTRIBUTES=deployment.environment=Production`. The DI extension copies the value into resource attributes automatically.
