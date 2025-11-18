using Microsoft.AspNetCore.Diagnostics;  
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace Errors.AspNetCore;

public sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionProblemDetailsMapper> mappers,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env                               // para decidir si incluimos stacktrace
) : IExceptionHandler
{
    private readonly IExceptionProblemDetailsMapper[] _chain = mappers.ToArray();

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1) Mapear Exception -> (status, ProblemDetails)
        var mapper = _chain.First(m => m.CanHandle(exception)); // UnknownExceptionMapper debe ir de último
        var (status, problem) = mapper.Map(httpContext, exception);

        // 2) Correlación visible para cliente y para trazas
        var errorId = $"err-{Guid.NewGuid():N}";
        problem.Extensions["errorId"] = errorId;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        problem.Instance ??= $"urn:problem:instance:{errorId}";

        // header útiles para front / soporte
        httpContext.Response.Headers["x-error-id"] = errorId;
        httpContext.Response.Headers["x-trace-id"] = httpContext.TraceIdentifier;

        // 3) Enriquecer el Activity (span) actual con tags + eventos
        var activity = Activity.Current ?? httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        problem.Extensions.TryGetValue("code", out var codeObj);
        var errorCode = codeObj as string ?? "UNKNOWN_ERROR";

        if (activity is not null)
        {
            activity.SetTag("app.trace_id", activity.TraceId.ToString());
            activity.SetTag("app.span_id", activity.SpanId.ToString());
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag("app.error.id", errorId);
            activity.SetTag("app.error.code", errorCode);
            if (!string.IsNullOrWhiteSpace(problem.Type)) activity.SetTag("app.problem.type", problem.Type!);
            if (!string.IsNullOrWhiteSpace(problem.Title)) activity.SetTag("app.problem.title", problem.Title!);
            activity.SetTag("http.response.status_code", (int)status);
            activity.SetTag("app.request.path", httpContext.Request.Path.ToString());

            // Evento estándar 'exception' (Jaeger -> pestaña Logs)
            var includeDetails = env.IsDevelopment();
            activity.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    ["exception.type"] = exception.GetType().FullName,
                    ["exception.message"] = includeDetails ? exception.Message : "Redacted (non-Development)",
                    ["exception.stacktrace"] = includeDetails ? exception.ToString() : string.Empty
                }));

            // Evento con ProblemDetails para query rápida por errorId/code
            activity.AddEvent(new ActivityEvent(
                "problem-details",
                tags: new ActivityTagsCollection
                {
                    ["error.id"] = errorId,
                    ["error.code"] = errorCode,
                    ["problem.type"] = problem.Type ?? "",
                    ["problem.title"] = problem.Title ?? "",
                    ["problem.status"] = problem.Status ?? 0,
                    ["path"] = httpContext.Request.Path.ToString()
                }));
        }

        // 4) Logging estructurado (con Scope)
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            ["SpanId"] = Activity.Current?.SpanId.ToString(),
            ["Path"] = httpContext.Request.Path.Value,
            ["ErrorId"] = errorId,
            ["ErrorCode"] = errorCode
        }))
        {
            var lvl = (int)status >= 500 ? LogLevel.Error : LogLevel.Warning;
            logger.Log(lvl, exception,
                "Exception mapped to ProblemDetails {Status} {Type} ({ErrorCode})",
                (int)status, problem.Type, errorCode);
        }

        // 5) Respuesta RFC 7807
        httpContext.Response.StatusCode = (int)status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}