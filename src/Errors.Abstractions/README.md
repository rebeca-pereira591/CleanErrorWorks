# Errors.Abstractions

## Overview
`Errors.Abstractions` contains the shared contracts that unify error handling across the CleanErrorWorks stack. It defines `ErrorCode`, the `IAppError` surface, strongly typed domain exceptions, and the `Result<T>` monad used by APIs and libraries to communicate failures without throwing.

## Usage
Install the package into your project:

```bash
 dotnet add package CleanErrorWorks.Errors.Abstractions
```

Reference the contracts from any domain or infrastructure layer:

```csharp
using Errors.Abstractions;
using Errors.Abstractions.Exceptions;

var code = new ErrorCode("PAYMENT-001", "Payment declined", "/errors/payment/declined");
throw new DomainException(code, "Card issuer rejected the authorization");
```

## Options & Configuration
This package does not expose options on its own. Instead, the data structures are consumed by the other CleanErrorWorks packages which offer configuration knobs. You typically configure options in `Errors.AspNetCore` (sanitization) and `Observability.OpenTelemetry` (telemetry).

## Extensibility
- **Custom errors:** Implement `IAppError` or derive from `Exception` and implement the interface to provide project-specific metadata.
- **Custom error codes:** Create additional `ErrorCode` instances that include a documentation URI.
- **Result helpers:** Wrap business operations in `Result<T>` and add your own extension methods (e.g., `Map`, `Bind`) to keep pipelines fluent.

## Examples
```csharp
public sealed record PaymentFailed(string TransactionId) : IAppError
{
    public ErrorCode Code { get; } = new("PAYMENT-FAILED", "Payment failed", "/errors/payment/failed");
    public string? Detail => $"Transaction {TransactionId} failed";
    public bool IsTransient => false;
    public HttpStatusCode? PreferredStatus => HttpStatusCode.PaymentRequired;
}

public Result<Order> PlaceOrder(Order order)
{
    if (!order.IsValid)
    {
        return Result<Order>.Fail(new PaymentFailed(order.Id));
    }

    // persist order...
    return Result<Order>.Ok(order);
}
```

## Dependencies
- No direct runtime dependencies other than the .NET BCL.
- Referenced by `Errors.AspNetCore`, `Logging.Core`, `Observability.OpenTelemetry`, and `Demo.Api`.
