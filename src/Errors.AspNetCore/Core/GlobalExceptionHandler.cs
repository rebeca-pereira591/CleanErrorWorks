using Errors.AspNetCore.Enrichers;
using Errors.AspNetCore.Formatters;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Errors.AspNetCore.Core;

/// <summary>
/// Centralizes exception handling for ASP.NET Core by mapping, formatting, logging, and enriching telemetry for failures.
/// </summary>
public sealed class GlobalExceptionHandler(
    IExceptionMapperResolver mapperResolver,
    ISpanEnricher spanEnricher,
    IProblemDetailsFormatter problemDetailsFormatter,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    /// <summary>
    /// Resolves the incoming exception into <see cref="ProblemDetails"/>, enriches telemetry, logs the event, and writes the response.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="exception">Exception to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the exception was fully handled.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, problem) = mapperResolver.Resolve(httpContext, exception);

        var formattingResult = problemDetailsFormatter.Format(httpContext, problem, status);

        spanEnricher.Enrich(httpContext, exception, problem, status, formattingResult.ErrorId, formattingResult.ErrorCode);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            ["SpanId"] = Activity.Current?.SpanId.ToString(),
            ["Path"] = httpContext.Request.Path.Value,
            ["ErrorId"] = formattingResult.ErrorId,
            ["ErrorCode"] = formattingResult.ErrorCode
        }))
        {
            var lvl = (int)status >= 500 ? LogLevel.Error : LogLevel.Warning;
            logger.Log(lvl, exception,
                "Exception mapped to ProblemDetails {Status} {Type} ({ErrorCode})",
                (int)status, problem.Type, formattingResult.ErrorCode);
        }

        await problemDetailsFormatter.WriteAsync(httpContext, problem, cancellationToken);
        return true;
    }
}
