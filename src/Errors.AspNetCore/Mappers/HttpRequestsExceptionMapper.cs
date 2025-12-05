using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Maps <see cref="HttpRequestException"/> instances to gateway error responses.
/// </summary>
[ExceptionMapper(priority: 300)]
public sealed class HttpRequestExceptionMapper(IExceptionSanitizer sanitizer) : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is HttpRequestException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var e = (HttpRequestException)ex;
        var status = e.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => (HttpStatusCode)429,
            HttpStatusCode.RequestTimeout => HttpStatusCode.GatewayTimeout,
            _ => HttpStatusCode.BadGateway
        };

        var sanitized = sanitizer.Sanitize(ctx, ex, "External dependency call failed.", treatPreferredDetailAsSensitive: false);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType("/errors/http/upstream")
            .WithTitle("Upstream HTTP error")
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode("INFRA-HTTP-UPSTREAM")
            .WithTraceId()
            .WithExtension("upstreamStatus", (int?)(e.StatusCode ?? 0))
            .Build();

        return (status, problem);
    }
}
