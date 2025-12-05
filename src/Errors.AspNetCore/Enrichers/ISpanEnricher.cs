using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Enrichers;

/// <summary>
/// Adds telemetry information related to a handled exception.
/// </summary>
public interface ISpanEnricher
{
    /// <summary>
    /// Adds telemetry information related to a handled exception.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">Exception being handled.</param>
    /// <param name="problemDetails">ProblemDetails produced by the mapper.</param>
    /// <param name="statusCode">HTTP status code that will be emitted.</param>
    /// <param name="errorId">Generated error identifier.</param>
    /// <param name="errorCode">Domain specific error code.</param>
    void Enrich(HttpContext httpContext, Exception exception, ProblemDetails problemDetails, HttpStatusCode statusCode, string errorId, string errorCode);
}
