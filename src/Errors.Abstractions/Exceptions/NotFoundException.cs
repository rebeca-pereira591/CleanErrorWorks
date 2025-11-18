using System.Net;

namespace Errors.Abstractions.Exceptions;

public sealed class NotFoundException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("NOT_FOUND", "Resource not found", "/errors/not-found");
    public string? Detail => Message;
    public bool IsTransient => false;
    public HttpStatusCode? PreferredStatus => HttpStatusCode.NotFound;


    public NotFoundException(string message) : base(message) { }
}
