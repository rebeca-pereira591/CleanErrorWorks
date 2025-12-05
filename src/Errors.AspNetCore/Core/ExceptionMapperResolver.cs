using Errors.AspNetCore.Registry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Core;

/// <summary>
/// Uses <see cref="IExceptionMapperRegistry"/> to find the best mapper for a given exception instance.
/// </summary>
public sealed class ExceptionMapperResolver(IExceptionMapperRegistry registry) : IExceptionMapperResolver
{
    public (HttpStatusCode StatusCode, ProblemDetails ProblemDetails) Resolve(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var mapper = registry.Resolve(exception);
        return mapper.Map(httpContext, exception);
    }
}
