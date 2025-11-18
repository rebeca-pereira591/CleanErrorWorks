using Errors.Abstractions.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public sealed class AuthorizationExceptionMapper
    : ExceptionProblemDetailsMapper<AuthorizationException>
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, AuthorizationException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.Forbidden;
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