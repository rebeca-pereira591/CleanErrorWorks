using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Core;

/// <summary>
/// Resolves the <see cref="ProblemDetails"/> that best represents a specific exception.
/// </summary>
public interface IExceptionMapperResolver
{
    /// <summary>
    /// Maps an exception to the appropriate HTTP status code and <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="exception">Exception about to be surfaced.</param>
    /// <returns>A tuple containing the HTTP status and problem payload.</returns>
    (HttpStatusCode StatusCode, ProblemDetails ProblemDetails) Resolve(HttpContext httpContext, Exception exception);
}
