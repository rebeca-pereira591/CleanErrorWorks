using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Errors.AspNetCore.Formatters;

/// <summary>
/// Formats RFC 7807 responses so they can be serialized consistently.
/// </summary>
public interface IProblemDetailsFormatter
{
    ProblemDetailsFormattingResult Format(HttpContext httpContext, ProblemDetails problemDetails, HttpStatusCode statusCode);

    ValueTask WriteAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken);
}
