using Errors.Abstractions;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Maps any remaining <see cref="IAppError"/> implementations that lack a specific mapper.
/// </summary>
[ExceptionMapper(priority: 100, IsFallback = true)]
public sealed class AppErrorFallbackMapper(IExceptionSanitizer sanitizer) : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is IAppError;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var e = (IAppError)ex;
        var status = e.PreferredStatus ?? HttpStatusCode.InternalServerError;

        var sanitized = sanitizer.Sanitize(ctx, ex, e.Detail, treatPreferredDetailAsSensitive: true);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType(e.Code.TypeUri)
            .WithTitle(e.Code.Title)
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode(e.Code.Code)
            .WithTraceId()
            .Build();

        return (status, problem);
    }
}
