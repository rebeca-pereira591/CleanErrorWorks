# Observability.OpenTelemetry

## Overview
`Observability.OpenTelemetry` bootstraps OpenTelemetry tracing and metrics with sensible defaults (resource attributes, sampling, exporters) and exposes span tag/event builders used by the error handling pipeline.

## Usage
Install the package and register the default pipeline:

```bash
 dotnet add package CleanErrorWorks.Observability.OpenTelemetry
```

```csharp
builder.Services.AddDefaultOpenTelemetry(builder.Configuration, options =>
{
    options.ServiceName = "checkout-api";
    options.Exporters.ConsoleEnabled = true;
});
```

## Options & Configuration
`OpenTelemetryOptions` groups all tunables:

```csharp
builder.Services.AddDefaultOpenTelemetry(builder.Configuration, options =>
{
    options.ServiceVersion = "1.2.3";
    options.Environment = builder.Environment.EnvironmentName;
    options.EnableMetrics = true;
    options.EnableTracing = true;

    options.Sampling.Strategy = SamplingStrategy.ParentBasedTraceIdRatio;
    options.Sampling.Probability = 0.25;

    options.Exporters.Otlp.Enabled = true;
    options.Exporters.Otlp.Endpoint = "http://otel-collector:4318";
    options.Exporters.ConsoleEnabled = builder.Environment.IsDevelopment();
});
```

Each exporter (`OtlpExporterOptions`, `TempoExporterOptions`, `ApplicationInsightsExporterOptions`) exposes endpoints, protocols, headers, and connection strings.

## Extensibility
- **Custom taggers/enrichers:** Implement `IActivityTagger` or `IActivityEventFactory` and register them before calling `AddDefaultOpenTelemetry`.
- **Additional ActivitySources:** Add values to `OpenTelemetryOptions.ActivitySources` for instrumentation beyond ASP.NET Core/HttpClient.
- **Exporter hooks:** Append delegates to `TracingEnrichers` or `MetricsEnrichers` to register additional instrumentation or processors.
- **ProblemDetails telemetry:** The default `ActivitySpanEnricher` pushes `exception.*` and `problem.*` attributes (type, title, status, detail, code, category, errorId, traceId, instance, sqlErrorNumber). Replace the enricher if you want to trim or add attributes before OTLP export.

## Examples
```csharp
builder.Services.AddDefaultOpenTelemetry(builder.Configuration, options =>
{
    options.ResourceAttributes["service.instance.id"] = Environment.MachineName;
    options.TracingEnrichers.Add(tp => tp.AddSource("MyCompany.BackgroundJobs"));
    options.Exporters.Tempo.Enabled = true;
    options.Exporters.Tempo.Endpoint = "http://tempo:4318";
});
```

## Dependencies
- Depends on `OpenTelemetry` SDK packages and optional exporters (Azure Monitor, OTLP).
- Provides `IActivityTagger` and `IActivityEventFactory` to `Errors.AspNetCore` for span enrichment.
