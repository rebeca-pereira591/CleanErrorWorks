using Errors.Abstractions.Exceptions;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

[ExceptionMapper(priority: 900)]
public sealed class ValidationExceptionMapper(IExceptionSanitizer sanitizer)
    : ExceptionProblemDetailsMapper<ValidationException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, ValidationException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.UnprocessableContent;

        var validationDetails = new ValidationProblemDetails(ex.Errors.ToModelState());
        var sanitized = Sanitizer.Sanitize(ctx, ex, ex.Detail, treatPreferredDetailAsSensitive: true);

        var problem = ProblemDetailsBuilder.FromProblemDetails(ctx, validationDetails)
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
