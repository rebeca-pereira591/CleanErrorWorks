using System.Net;

namespace Errors.Abstractions.Exceptions;

/// <summary>
/// Represents missing resources that callers can remediate by adjusting their request.
/// </summary>
public sealed class NotFoundException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("NOT_FOUND", "Resource not found", "/errors/not-found");

    public string? Detail => Message;

    public bool IsTransient => false;

    public HttpStatusCode? PreferredStatus => HttpStatusCode.NotFound;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">Explanation returned to the caller.</param>
    public NotFoundException(string message) : base(message) { }
}
