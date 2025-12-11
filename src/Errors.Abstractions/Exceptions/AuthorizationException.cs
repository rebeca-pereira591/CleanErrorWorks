using System.Net;

namespace Errors.Abstractions.Exceptions;

/// <summary>
/// Represents access violations that should translate into HTTP 401/403 responses.
/// </summary>
public sealed class AuthorizationException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("AUTH", "Unauthorized/Forbidden", "/errors/auth");

    public string? Detail => Message;

    public bool IsTransient => false;

    public HttpStatusCode? PreferredStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    /// <param name="message">Reason provided to the caller.</param>
    /// <param name="forbidden">True to emit 403 Forbidden, false for 401 Unauthorized.</param>
    public AuthorizationException(string message, bool forbidden = false) : base(message)
    { PreferredStatus = forbidden ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized; }
}
