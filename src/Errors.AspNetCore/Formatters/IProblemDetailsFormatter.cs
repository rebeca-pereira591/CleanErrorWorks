using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Formatters;

/// <summary>
/// Formats RFC 7807 responses so they can be serialized consistently.
/// </summary>
public interface IProblemDetailsFormatter
{
    /// <summary>
    /// Applies headers, error identifiers, and validations before writing the response body.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="problemDetails">Problem payload to decorate.</param>
    /// <param name="statusCode">HTTP status that will be emitted.</param>
    /// <returns>A <see cref="ProblemDetailsFormattingResult"/> with identifiers.</returns>
    ProblemDetailsFormattingResult Format(HttpContext httpContext, ProblemDetails problemDetails, HttpStatusCode statusCode);

    /// <summary>
    /// Writes the hydrated <see cref="ProblemDetails"/> to the HTTP response stream.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="problemDetails">Problem payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous operation.</returns>
    ValueTask WriteAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken);
}
