using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;
using System.Net;

namespace Errors.AspNetCore;

public sealed class TimeoutRejectedExceptionMapper : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is TimeoutRejectedException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var status = HttpStatusCode.GatewayTimeout;
        var pd = new ProblemDetails
        {
            Type = "/errors/http/timeout",
            Title = "External request timed out",
            Status = (int)status,
            Detail = "The upstream service did not respond in time.",
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = "INFRA-HTTP-TIMEOUT";
        return (status, pd);
    }
}