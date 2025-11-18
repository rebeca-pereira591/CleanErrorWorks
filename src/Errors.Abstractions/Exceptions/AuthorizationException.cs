using System.Net;

namespace Errors.Abstractions.Exceptions;

public sealed class AuthorizationException: Exception, IAppError
{
    public ErrorCode Code { get; } = new("AUTH", "Unauthorized/Forbidden", "/errors/auth");
    public string? Detail => Message;
    public bool IsTransient => false;
    public HttpStatusCode? PreferredStatus { get; }


    public AuthorizationException(string message, bool forbidden = false) : base(message)
    { PreferredStatus = forbidden ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized; }
}
