using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Mappers;

public abstract class ExceptionProblemDetailsMapper<TException> : IExceptionProblemDetailsMapper
    where TException : Exception
{
    protected ExceptionProblemDetailsMapper(IExceptionSanitizer sanitizer)
    {
        Sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    protected IExceptionSanitizer Sanitizer { get; }

    public bool CanHandle(Exception ex) => ex is TException;
    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
        => MapTyped(ctx, (TException)ex);

    protected abstract (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, TException ex);
}
