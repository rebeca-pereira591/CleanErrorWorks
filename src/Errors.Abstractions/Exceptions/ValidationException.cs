using System.Net;

namespace Errors.Abstractions.Exceptions;

/// <summary>
/// Represents input validation failures, including a detailed field-to-error mapping.
/// </summary>
public sealed class ValidationException : Exception, IAppError
{
    public ErrorCode Code { get; } = new("VALIDATION", "Validation failed", "/errors/validation");

    public string? Detail => Message;

    public bool IsTransient => false;

    public HttpStatusCode? PreferredStatus => HttpStatusCode.UnprocessableContent;

    /// <summary>
    /// Gets the per-field validation errors keyed by member name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">Primary reason returned to the caller.</param>
    /// <param name="errors">Detailed validation errors.</param>
    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
    { Errors = errors; }
}
