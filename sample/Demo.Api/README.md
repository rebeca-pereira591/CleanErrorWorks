# Demo.Api

## Overview
`Demo.Api` is a sample minimal API that exercises the CleanErrorWorks stack. It exposes endpoints that intentionally trigger validation, domain, SQL, and downstream HTTP failures so you can observe how `Errors.AspNetCore`, `Logging.Core`, and `Observability.OpenTelemetry` behave together.

## Usage
Run the API locally:

```bash
dotnet run --project sample/Demo.Api/Demo.Api.csproj
```

Invoke sample endpoints such as `/boom`, `/validation`, `/sql/duplicate`, or `/ext/slow` to see error handling in action.

## Options & Configuration
`Program.cs` wires the other packages:

```csharp
builder.Services
    .AddControllers()
    .AddErrorHandling()
    .AddDefaultLogging(builder.Configuration)
    .AddDefaultOpenTelemetry(builder.Configuration);
```

Update `appsettings.json` to point to your SQL instance (used by `SqlTestService`).

## Extensibility
- Add new endpoints that throw custom `IAppError` implementations to verify mapper behavior.
- Register additional hosted services or HttpClients to experiment with resiliency plus telemetry.

## Examples
Trigger a validation failure:

```bash
curl http://localhost:5000/validation
```

Trigger a SQL duplicate error:

```bash
curl -X POST http://localhost:5000/sql/duplicate
```

## Dependencies
- References `Errors.Abstractions`, `Errors.AspNetCore`, `Logging.Core`, and `Observability.OpenTelemetry`.
- Uses WireMock.Net to simulate downstream HTTP services.
