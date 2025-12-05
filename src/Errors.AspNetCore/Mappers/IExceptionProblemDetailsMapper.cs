using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Resolves exceptions into <see cref="ProblemDetails"/> payloads.
/// </summary>
public interface IExceptionProblemDetailsMapper
{
    /// <summary>
    /// Determines whether the mapper can handle the supplied exception instance.
    /// </summary>
    /// <param name="ex">Exception to inspect.</param>
    /// <returns><c>true</c> when the mapper can handle the exception.</returns>
    bool CanHandle(Exception ex);

    /// <summary>
    /// Converts the exception to <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="ctx">Current request context.</param>
    /// <param name="ex">Exception to map.</param>
    /// <returns>The resulting HTTP status and ProblemDetails.</returns>
    (HttpStatusCode Status, ProblemDetails Details) Map(HttpContext ctx, Exception ex);
}
