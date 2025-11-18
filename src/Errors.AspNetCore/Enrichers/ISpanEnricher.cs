using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Enrichers;

/// <summary>
/// Adds telemetry information related to a handled exception.
/// </summary>
public interface ISpanEnricher
{
    void Enrich(HttpContext httpContext, Exception exception, ProblemDetails problemDetails, HttpStatusCode statusCode, string errorId, string errorCode);
}
