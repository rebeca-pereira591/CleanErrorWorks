using System.Net;

namespace Errors.Abstractions.Exceptions;

public sealed class DomainException : Exception, IAppError
{
    public ErrorCode Code { get; }
    public string? Detail => Message;
    public bool IsTransient { get; init; }
    public HttpStatusCode? PreferredStatus { get; init; }


    public DomainException(ErrorCode code, string message, bool transient = false, HttpStatusCode? status = null)
    : base(message)
    { Code = code; IsTransient = transient; PreferredStatus = status; }
}
