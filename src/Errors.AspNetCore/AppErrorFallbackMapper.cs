using Errors.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public sealed class AppErrorFallbackMapper : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is IAppError;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var e = (IAppError)ex;
        var status = e.PreferredStatus ?? HttpStatusCode.InternalServerError;

        var pd = new ProblemDetails
        {
            Type = e.Code.TypeUri,
            Title = e.Code.Title,
            Status = (int)status,
            Detail = e.Detail,
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = e.Code.Code;
        return (status, pd);
    }
}
