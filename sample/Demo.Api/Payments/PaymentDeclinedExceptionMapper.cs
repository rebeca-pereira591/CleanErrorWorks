using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Mappers;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Demo.Api.Payments;

[ExceptionMapper(priority: 200, IsFallback = false)]
public sealed class PaymentDeclinedExceptionMapper(IExceptionSanitizer sanitizer)
    : ExceptionProblemDetailsMapper<PaymentDeclinedException>(sanitizer)
{
    protected override (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, PaymentDeclinedException ex)
    {
        var status = ex.PreferredStatus ?? HttpStatusCode.PaymentRequired;
        var sanitized = Sanitizer.Sanitize(ctx, ex, ex.Detail, treatPreferredDetailAsSensitive: true);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType(ex.Code.TypeUri ?? "https://httpstatuses.io/402")
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