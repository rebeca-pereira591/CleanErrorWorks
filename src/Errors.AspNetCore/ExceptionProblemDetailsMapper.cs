using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public abstract class ExceptionProblemDetailsMapper<TException> : IExceptionProblemDetailsMapper
    where TException : Exception
{
    public bool CanHandle(Exception ex) => ex is TException;
    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
        => MapTyped(ctx, (TException)ex);

    protected abstract (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, TException ex);
}