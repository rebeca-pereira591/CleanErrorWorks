using Errors.Abstractions.Exceptions;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

[ExceptionMapper(priority: 700)]
public sealed class AuthorizationExceptionMapper(IExceptionSanitizer sanitizer)
    : ExceptionProblemDetailsMapper<AuthorizationException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, AuthorizationException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.Forbidden;
        var sanitized = Sanitizer.Sanitize(ctx, ex, ex.Detail, treatPreferredDetailAsSensitive: true);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType(ex.Code.TypeUri)
            .WithTitle(ex.Code.Title)
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode(ex.Code.Code)
            .WithTraceId()
            .Build();

        return (status, problem);
    }
}
