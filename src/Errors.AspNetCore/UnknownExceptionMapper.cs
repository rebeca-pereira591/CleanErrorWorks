using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Errors.AspNetCore;

public sealed class UnknownExceptionMapper(IHostEnvironment env) : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception _) => true;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var status = HttpStatusCode.InternalServerError;
        var detail = env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";

        var pd = new ProblemDetails
        {
            Type = "/errors/unexpected",
            Title = "Unexpected error",
            Status = (int)status,
            Detail = detail,
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = "UNEXPECTED_ERROR";
        pd.Extensions["category"] = "Unhandled";
        return (status, pd);
    }
}