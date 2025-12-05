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

builder.Services.AddErrorHandling(); // defaults now expose full details for logs + telemetry

var app = builder.Build();
app.UseExceptionHandler();
```

## Options & Configuration
The package exposes several option objects:

- `ExceptionSanitizerOptions` – controls whether details/stack traces are exposed, the redacted message, and safe exception type names.
- `ProblemDetailsExtensionValidationOptions` – limits number, depth, and length of extensions injected into ProblemDetails instances.
- `ErrorHandlingOptions` – gateway for configuring the two option types from `AddErrorHandling`.

### Environment-specific sanitization
By default the sanitizer exposes the entire exception object (message, type, stack trace, and source), which means ProblemDetails, logs, and OTLP spans always contain the full error story. If you need to trim details in Production, opt in via `ExceptionSanitizerOptions`:

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddErrorHandling(options =>
    {
        options.ConfigureExceptionSanitizer(sanitizer =>
        {
            sanitizer.TreatPreferredDetailAsSensitive = true;
            sanitizer.RedactStackTraces = true;
            sanitizer.RedactedDetail = "We hit a snag. Please retry.";
        });
    });
}
else
{
    builder.Services.AddErrorHandling(); // Development/Demo keeps full diagnostic context
}
```

Example configuration:

```csharp
builder.Services.AddErrorHandling(options =>
{
    options.ConfigureExceptionSanitizer(sanitizer =>
    {
        sanitizer.TreatPreferredDetailAsSensitive = true;
        sanitizer.RedactStackTraces = true;
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
| `exception.type`, `exception.message`, `exception.stacktrace`, `exception.source`, `exception.full` | Raw exception data (including `Exception.ToString()` in `exception.full`). Sanitization options can suppress type/source/stack. |
| `problem.code`, `problem.title`, `problem.status`, `problem.detail`, `problem.instance`, `problem.trace_id`, `problem.error_id`, `problem.sql_error_number`, `problem.category` | Sanitized ProblemDetails payload exactly as returned to Swagger clients. |

To customize the exported attributes, register your own `ISpanEnricher` or replace the `IActivityTagger/IActivityEventFactory` implementations.

> **Sanitization reminder:** The `ExceptionSanitizerOptions` flags (`TreatPreferredDetailAsSensitive`, `RedactStackTraces`, `RedactedDetail`) control how much of the raw exception appears in telemetry. Leaving the defaults keeps everything visible; enable the flags in Production to redact.

Example span attributes rendered in Jaeger:

```text
exception.type = CleanErrorWorks.Payments.PaymentDeclinedException
exception.message = Card issuer rejected transaction 12345
exception.full = CleanErrorWorks.Payments.PaymentDeclinedException: Card issuer...\n   at Demo.Api...
problem.code = PAYMENTS-DECLINED
problem.status = 402
problem.detail = Card issuer rejected transaction 12345
problem.trace_id = 01HKS... 
problem.error_id = err-6b86b273
```

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
