using System.Net;

namespace Errors.Abstractions.Exceptions;

/// <summary>
/// Represents domain rule violations surfaced as structured errors.
/// </summary>
public sealed class DomainException : Exception, IAppError
{
    public ErrorCode Code { get; }

    public string? Detail => Message;

    public bool IsTransient { get; init; }

    public HttpStatusCode? PreferredStatus { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="code">Domain-specific error metadata.</param>
    /// <param name="message">Human-readable explanation.</param>
    /// <param name="transient">True when the failure may succeed on retry.</param>
    /// <param name="status">Optional HTTP status override.</param>
    public DomainException(ErrorCode code, string message, bool transient = false, HttpStatusCode? status = null)
    : base(message)
    { Code = code; IsTransient = transient; PreferredStatus = status; }
}
