# Logging.Core

## Overview
`Logging.Core` delivers a repeatable set of logging conventions: clearing default providers, binding configuration, enforcing minimum levels, and offering typed log helpers. It keeps logging configuration consistent across microservices.

## Usage
Reference the package and call the extension:

```bash
 dotnet add package CleanErrorWorks.Logging.Core
```

```csharp
builder.Services.AddDefaultLogging(builder.Configuration, options =>
{
    options.MinimumLevel = LogLevel.Debug;
});
```

## Options & Configuration
`LoggingConventionOptions` exposes the knobs:

```csharp
builder.Services.AddDefaultLogging(builder.Configuration, options =>
{
    options.ClearProviders = true;
    options.UseConfiguration = true;
    options.ConfigurationSectionName = "Logging";
    options.MinimumLevel = LogLevel.Information;

    options.Providers.Add(lb => lb.AddConsole());
    options.Providers.Add(lb => lb.AddDebug());

    options.ConfigureBuilder = lb => lb.AddFilter("System", LogLevel.Warning);
});
```

## Extensibility
- **Providers:** Add custom delegates to `LoggingConventionOptions.Providers` to register sinks such as Seq or Application Insights.
- **Source generators:** Use `ApiLog` (or add your own partial logging classes) to create structured log statements with compile-time safety.
- **Wrapper:** Replace or extend `HttpLogger` if you need to inject additional scopes before calling the inner `ILogger`.

## Examples
```csharp
builder.Logging.AddDefaultLogging(builder.Configuration, options =>
{
    options.Providers.Add(lb => lb.AddAzureWebAppDiagnostics());
});

public static partial class CheckoutLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Warning, Message = "Order {OrderId} expired")]
    public static partial void OrderExpired(HttpLogger logger, Guid orderId);
}
```

## Dependencies
- Depends on `Microsoft.Extensions.Logging` abstractions.
- Consumed by `Demo.Api` and indirectly supports `Errors.AspNetCore` by providing configured `ILogger` instances.
