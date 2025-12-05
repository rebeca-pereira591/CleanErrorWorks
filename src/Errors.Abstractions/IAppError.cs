using System.Net;

namespace Errors.Abstractions;

/// <summary>
/// Defines a structured error that can be surfaced to API callers or logged internally.
/// </summary>
public interface IAppError
{
    /// <summary>
    /// Gets the canonical error code for the failure.
    /// </summary>
    ErrorCode Code { get; }

    /// <summary>
    /// Gets a user or operator friendly detail string.
    /// </summary>
    string? Detail { get; }

    /// <summary>
    /// Gets a value indicating whether the error results from a transient condition.
    /// </summary>
    bool IsTransient { get; }

    /// <summary>
    /// Gets an optional preferred HTTP status code that should accompany the error.
    /// </summary>
    HttpStatusCode? PreferredStatus { get; }
}
