using System.Net;

namespace Errors.Abstractions;

public interface IAppError
{
    ErrorCode Code { get; }
    string? Detail { get; }
    bool IsTransient { get; }
    HttpStatusCode? PreferredStatus { get; }
}