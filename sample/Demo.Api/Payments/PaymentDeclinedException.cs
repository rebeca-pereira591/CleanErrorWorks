using Errors.Abstractions;
using System.Net;

namespace Demo.Api.Payments;

public sealed class PaymentDeclinedException : Exception, IAppError
{
    public PaymentDeclinedException(string? detail = null, bool isTransient = false, HttpStatusCode? preferredStatus = null)
        : base(detail)
    {
        Detail = detail;
        IsTransient = isTransient;
        PreferredStatus = preferredStatus ?? HttpStatusCode.PaymentRequired;
    }

    public ErrorCode Code { get; } = new("payment_declined", "Payment was declined", typeUri: "https://httpstatuses.io/402");

    public string? Detail { get; }

    public bool IsTransient { get; }

    public HttpStatusCode? PreferredStatus { get; }
}