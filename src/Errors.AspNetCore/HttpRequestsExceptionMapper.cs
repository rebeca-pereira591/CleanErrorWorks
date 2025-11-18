using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public sealed class HttpRequestExceptionMapper : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is HttpRequestException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var e = (HttpRequestException)ex;
        var status = e.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => (HttpStatusCode)429,
            HttpStatusCode.RequestTimeout => HttpStatusCode.GatewayTimeout,
            _ => HttpStatusCode.BadGateway
        };

        var pd = new ProblemDetails
        {
            Type = "/errors/http/upstream",
            Title = "Upstream HTTP error",
            Status = (int)status,
            Detail = "External dependency call failed.",
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = "INFRA-HTTP-UPSTREAM";
        pd.Extensions["upstreamStatus"] = (int?)(e.StatusCode ?? 0);
        return (status, pd);
    }
}