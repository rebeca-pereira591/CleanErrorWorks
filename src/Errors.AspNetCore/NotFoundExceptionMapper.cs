using Errors.Abstractions.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public sealed class NotFoundExceptionMapper
    : ExceptionProblemDetailsMapper<NotFoundException>
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, NotFoundException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.NotFound;
        var pd = new ProblemDetails
        {
            Type = ex.Code.TypeUri,
            Title = ex.Code.Title,
            Status = (int)status,
            Detail = ex.Detail,
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = ex.Code.Code;
        return (status, pd);
    }
}