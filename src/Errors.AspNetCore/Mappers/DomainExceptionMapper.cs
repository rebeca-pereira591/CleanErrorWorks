using Errors.Abstractions.Exceptions;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Maps <see cref="DomainException"/> into ProblemDetails responses.
/// </summary>
[ExceptionMapper(priority: 500)]
public sealed class DomainExceptionMapper(IExceptionSanitizer sanitizer) : ExceptionProblemDetailsMapper<DomainException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, DomainException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.BadRequest;
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
