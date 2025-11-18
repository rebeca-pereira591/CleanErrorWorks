using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Core;

/// <summary>
/// Resolves the <see cref="ProblemDetails"/> that best represents a specific exception.
/// </summary>
public interface IExceptionMapperResolver
{
    (HttpStatusCode StatusCode, ProblemDetails ProblemDetails) Resolve(HttpContext httpContext, Exception exception);
}
