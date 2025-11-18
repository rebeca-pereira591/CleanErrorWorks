using Errors.Abstractions.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public sealed class ValidationExceptionMapper
    : ExceptionProblemDetailsMapper<ValidationException>
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, ValidationException ex)
    {
        var vpd = new ValidationProblemDetails(ex.Errors.ToModelState())
        {
            Type = ex.Code.TypeUri,
            Title = ex.Code.Title,
            Status = (int)(ex.PreferredStatus ?? HttpStatusCode.UnprocessableContent),
            Detail = ex.Detail,
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };
        vpd.Extensions["traceId"] = ctx.TraceIdentifier;
        vpd.Extensions["code"] = ex.Code.Code;

        return ((HttpStatusCode)vpd.Status!, vpd);
    }
}