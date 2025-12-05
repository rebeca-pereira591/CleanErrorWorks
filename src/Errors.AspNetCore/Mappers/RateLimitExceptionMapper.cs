using Errors.Abstractions.Exceptions;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Maps <see cref="RateLimitException"/> instances to ProblemDetails responses.
/// </summary>
[ExceptionMapper(priority: 600)]
public sealed class RateLimitExceptionMapper(IExceptionSanitizer sanitizer) : ExceptionProblemDetailsMapper<RateLimitException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, RateLimitException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.TooManyRequests;
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
