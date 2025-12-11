using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Final fallback mapper that hides unexpected exception details.
/// </summary>
[ExceptionMapper(priority: 0, IsFallback = true)]
public sealed class UnknownExceptionMapper(IExceptionSanitizer sanitizer) : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception _) => true;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var status = HttpStatusCode.InternalServerError;
        var sanitized = sanitizer.Sanitize(ctx, ex, ex.Message, treatPreferredDetailAsSensitive: true);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType("/errors/unexpected")
            .WithTitle("Unexpected error")
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode("UNEXPECTED_ERROR")
            .WithTraceId()
            .WithExtension("category", "Unhandled")
            .Build();

        return (status, problem);
    }
}
