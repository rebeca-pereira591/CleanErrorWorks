using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

[ExceptionMapper(priority: 200)]
public sealed class TimeoutRejectedExceptionMapper(IExceptionSanitizer sanitizer) : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is TimeoutRejectedException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var status = HttpStatusCode.GatewayTimeout;
        var sanitized = sanitizer.Sanitize(ctx, ex, "The upstream service did not respond in time.", treatPreferredDetailAsSensitive: false);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType("/errors/http/timeout")
            .WithTitle("External request timed out")
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode("INFRA-HTTP-TIMEOUT")
            .WithTraceId()
            .Build();

        return (status, problem);
    }
}
