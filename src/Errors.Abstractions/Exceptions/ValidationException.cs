using System.Net;

namespace Errors.Abstractions.Exceptions;
public sealed class ValidationException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("VALIDATION", "Validation failed", "/errors/validation");
    public string? Detail => Message;
    public bool IsTransient => false;
    public HttpStatusCode? PreferredStatus => HttpStatusCode.UnprocessableContent;


    public IReadOnlyDictionary<string, string[]> Errors { get; }


    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
    { Errors = errors; }
}
