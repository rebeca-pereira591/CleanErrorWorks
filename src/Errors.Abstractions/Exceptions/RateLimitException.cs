using System.Net;

namespace Errors.Abstractions.Exceptions;

/// <summary>
/// Represents breaches of throttling or quota policies.
/// </summary>
public sealed class RateLimitException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("RATE_LIMIT", "Too Many Requests", "/errors/rate-limit");

    public string? Detail => Message;

    public bool IsTransient => true;

    public HttpStatusCode? PreferredStatus => HttpStatusCode.TooManyRequests;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="message">Explanation describing the quota violation.</param>
    public RateLimitException(string message) : base(message) { }
}
