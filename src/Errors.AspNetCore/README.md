# Errors.AspNetCore

## Overview
`Errors.AspNetCore` plugs the CleanErrorWorks error model into ASP.NET Core. It registers a global exception handler, sanitizes exception details, maps failures to RFC 7807 `ProblemDetails`, enriches telemetry, and emits structured logs.

## Usage
Install the package and call the extension during startup:

```bash
 dotnet add package CleanErrorWorks.Errors.AspNetCore
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddErrorHandling(options =>
{
    options.ConfigureExceptionSanitizer(opt => opt.IncludeStackTraceInDevelopment = true);
});

var app = builder.Build();
app.UseExceptionHandler();
```

## Options & Configuration
The package exposes several option objects:

- `ExceptionSanitizerOptions` – controls whether details/stack traces are exposed, the redacted message, and safe exception type names.
- `ProblemDetailsExtensionValidationOptions` – limits number, depth, and length of extensions injected into ProblemDetails instances.
- `ErrorHandlingOptions` – gateway for configuring the two option types from `AddErrorHandling`.

### Environment-specific sanitization
By default the sanitizer treats both `Development` and `Demo` environments as “developer experience” environments. In those environments the middleware keeps the full exception message, adds the exception source to the ProblemDetails detail, and emits stack traces so Jaeger/Zipkin/Tempo can show full diagnostics. In Production (or any other environment name) the sanitizer reverts to `RedactedDetail` and strips stack traces.

You can still override the defaults if needed:

```csharp
if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Demo")
{
    builder.Services.AddErrorHandling(); // full details, stack traces, telemetry tags
}
else
{
    builder.Services.AddErrorHandling(options =>
    {
        options.ConfigureExceptionSanitizer(sanitizer =>
        {
            sanitizer.RedactedDetail = "We hit a snag. Please retry.";
            sanitizer.IncludeStackTraceInDevelopment = false; // keep prod clean
        });
    });
}
```

Example configuration:

```csharp
builder.Services.AddErrorHandling(options =>
{
    options.ConfigureExceptionSanitizer(sanitizer =>
    {
        sanitizer.RedactedDetail = "We hit a snag. Please retry.";
        sanitizer.AllowExceptionDetails = ex => ex is ValidationException;
    });

    options.ConfigureProblemDetailsExtensions(validation =>
    {
        validation.MaxExtensions = 10;
        validation.MaxStringLength = 1024;
    });
});
```

### Telemetry enrichment
`ActivitySpanEnricher` now adds the following span attributes so Jaeger/Zipkin/Tempo can display every ProblemDetails field:

| Attribute | Description |
|-----------|-------------|
| `exception.message`, `exception.stacktrace`, `exception.source` | Raw exception data (only populated in Development/Demo). |
| `problem.type`, `problem.title`, `problem.status`, `problem.detail`, `problem.instance` | Core RFC 7807 fields (sanitized in Production). |
| `problem.code`, `problem.category`, `problem.error_id`, `problem.trace_id`, `problem.sql_error_number` | Metadata surfaced through `ProblemDetails.Extensions`. |

To customize the exported attributes, register your own `ISpanEnricher` or replace the `IActivityTagger/IActivityEventFactory` implementations.

## Extensibility
- **Custom mappers:** Implement `IExceptionProblemDetailsMapper`, decorate with `ExceptionMapperAttribute`, and register via DI to override or extend behavior.
- **Custom sanitizers:** Provide your own `IExceptionSanitizer` implementation if you need per-tenant rules.
- **Span enrichers:** Implement `ISpanEnricher` to add tracing data (or wrap/replace `ActivitySpanEnricher`).
- **Mapper registry tweaks:** Use priorities and fallbacks to control ordering when multiple mappers can handle the same exception type.

## Examples
Map a bespoke exception:

```csharp
[ExceptionMapper(priority: 950)]
public sealed class PaymentExceptionMapper(IExceptionSanitizer sanitizer)
    : ExceptionProblemDetailsMapper<PaymentException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, PaymentException ex)
    {
        var sanitized = Sanitizer.Sanitize(ctx, ex, ex.Detail);
        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType(ex.Code.TypeUri)
            .WithTitle(ex.Code.Title)
            .WithDetail(sanitized.Detail)
            .WithStatus(HttpStatusCode.PaymentRequired)
            .WithCode(ex.Code.Code)
            .WithTraceId()
            .Build();
        return (HttpStatusCode.PaymentRequired, problem);
    }
}
```
Register the mapper:

```csharp
builder.Services.AddErrorHandling().AddSingleton<IExceptionProblemDetailsMapper, PaymentExceptionMapper>();
```

## Dependencies
- Depends on `Errors.Abstractions` for error contracts.
- Consumes `Observability.OpenTelemetry` telemetry services (`IActivityTagger`, `IActivityEventFactory`).
- Logs via the abstractions configured by `Logging.Core`.
