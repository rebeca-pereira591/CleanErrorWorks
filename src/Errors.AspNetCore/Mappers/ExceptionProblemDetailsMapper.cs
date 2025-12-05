using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Provides shared mapper behavior such as sanitization for specific exception types.
/// </summary>
/// <typeparam name="TException">Exception type handled by the mapper.</typeparam>
public abstract class ExceptionProblemDetailsMapper<TException> : IExceptionProblemDetailsMapper
    where TException : Exception
{
    protected ExceptionProblemDetailsMapper(IExceptionSanitizer sanitizer)
    {
        Sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    protected IExceptionSanitizer Sanitizer { get; }

    public bool CanHandle(Exception ex) => ex is TException;

    /// <inheritdoc />
    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
        => MapTyped(ctx, (TException)ex);

    /// <summary>
    /// Maps the strongly typed exception to <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="ctx">Current request context.</param>
    /// <param name="ex">Exception instance.</param>
    /// <returns>The mapped HTTP status and ProblemDetails.</returns>
    protected abstract (HttpStatusCode, ProblemDetails) MapTyped(HttpContext ctx, TException ex);
}
