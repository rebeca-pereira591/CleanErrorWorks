using System.Net;

namespace Errors.Abstractions.Exceptions;

public sealed class RateLimitException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("RATE_LIMIT", "Too Many Requests", "/errors/rate-limit");
    public string? Detail => Message;
    public bool IsTransient => true;
    public HttpStatusCode? PreferredStatus => HttpStatusCode.TooManyRequests;


    public RateLimitException(string message) : base(message) { }
}