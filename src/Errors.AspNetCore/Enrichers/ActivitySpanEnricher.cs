using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Observability.OpenTelemetry.Telemetry;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Errors.AspNetCore.Enrichers;

/// <summary>
/// Default implementation that annotates <see cref="Activity"/> instances with error metadata and ProblemDetails extensions.
/// </summary>
/// <remarks>
/// Adds <c>exception.*</c> (type, message, stack trace, source, full) attributes plus <c>problem.*</c> fields
/// (code, title, status, detail, instance, trace_id, error_id, sql_error_number, category) so OTLP exporters surface the full payload.
/// </remarks>
public sealed class ActivitySpanEnricher(
    IActivityTagger tagger,
    IActivityEventFactory eventFactory,
    ActivitySource activitySource,
    IExceptionSanitizer sanitizer) : ISpanEnricher
{
    public void Enrich(HttpContext httpContext, Exception exception, ProblemDetails problemDetails, HttpStatusCode statusCode, string errorId, string errorCode)
    {
        var activity = Activity.Current ?? httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        var ownsActivity = false;

        if (activity is null)
        {
            activity = activitySource.StartActivity("errors.enricher", ActivityKind.Internal);
            ownsActivity = activity is not null;
        }

        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error);
        var environmentName = httpContext.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName ?? "unknown";

        var tagContext = new ActivityTagContext(
            httpContext.TraceIdentifier,
            httpContext.Request.Path.Value ?? string.Empty,
            (int)statusCode,
            errorId,
            errorCode,
            problemDetails.Type,
            problemDetails.Title,
            environmentName);

        tagger.Apply(activity, tagContext);

        var sanitized = sanitizer.Sanitize(httpContext, exception, problemDetails.Detail, treatPreferredDetailAsSensitive: false);

        AddExceptionTags(activity, exception, sanitized);
        AddProblemDetailsTags(activity, problemDetails, statusCode, errorId, httpContext.TraceIdentifier);

        var exceptionEventContext = new ActivityExceptionEventContext(
            errorId,
            errorCode,
            environmentName,
            sanitized.Detail,
            sanitized.IncludeStackTrace);

        var problemContext = new ActivityProblemDetailsContext(
            errorId,
            errorCode,
            problemDetails.Type,
            problemDetails.Title,
            (int)(problemDetails.Status ?? (int)statusCode),
            httpContext.Request.Path.Value ?? string.Empty);

        activity.AddEvent(eventFactory.CreateExceptionEvent(exception, exceptionEventContext));
        activity.AddEvent(eventFactory.CreateProblemDetailsEvent(problemContext));

        if (ownsActivity)
        {
            activity.Stop();
        }
    }

    private static void AddExceptionTags(Activity activity, Exception exception, ExceptionSanitizationResult sanitized)
    {
        var exceptionType = sanitized.IsRedacted ? null : exception.GetType().FullName;
        var message = string.IsNullOrWhiteSpace(exception.Message) ? sanitized.Detail : exception.Message;
        var normalizedMessage = NormalizeMultiline(message);
        var normalizedStack = sanitized.IncludeStackTrace && !sanitized.IsRedacted
            ? NormalizeMultiline(exception.StackTrace)
            : null;
        var fullException = sanitized.IsRedacted
            ? sanitized.Detail
            : NormalizeMultiline(exception.ToString());

        SetTagIfValue(activity, "exception.type", exceptionType);
        SetTagIfValue(activity, "exception.message", normalizedMessage);
        SetTagIfValue(activity, "exception.stacktrace", normalizedStack);
        SetTagIfValue(activity, "exception.source", sanitized.IsRedacted ? null : exception.Source);
        SetTagIfValue(activity, "exception.full", fullException);
    }

    private static void AddProblemDetailsTags(Activity activity, ProblemDetails problemDetails, HttpStatusCode fallbackStatus, string errorId, string traceId)
    {
        SetTagIfValue(activity, "problem.type", problemDetails.Type);
        SetTagIfValue(activity, "problem.code", GetExtensionValue(problemDetails, "code"));
        SetTagIfValue(activity, "problem.title", problemDetails.Title);
        SetTagIfValue(activity, "problem.status", problemDetails.Status ?? (int)fallbackStatus);
        SetTagIfValue(activity, "problem.detail", NormalizeMultiline(problemDetails.Detail));
        SetTagIfValue(activity, "problem.instance", problemDetails.Instance);
        SetTagIfValue(activity, "problem.trace_id", GetExtensionValue(problemDetails, "traceId") ?? traceId);
        SetTagIfValue(activity, "problem.error_id", GetExtensionValue(problemDetails, "errorId") ?? errorId);
        SetTagIfValue(activity, "problem.sql_error_number", GetExtensionValue(problemDetails, "sqlErrorNumber"));
        SetTagIfValue(activity, "problem.category", GetExtensionValue(problemDetails, "category"));
    }

    private static void SetTagIfValue(Activity activity, string tagName, object? value)
    {
        if (value is null) return;
        if (value is string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return;
            activity.SetTag(tagName, str);
            return;
        }

        activity.SetTag(tagName, value);
    }

    private static string? GetExtensionValue(ProblemDetails problemDetails, string key)
    {
        if (!problemDetails.Extensions.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string str when string.IsNullOrWhiteSpace(str) => null,
            string str => str,
            _ => value.ToString()
        };
    }

    private static string? NormalizeMultiline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = value.Replace("\r\n", "\n");
        var segments = normalized.Split('\n');
        var builder = new StringBuilder();
        string? previous = null;

        foreach (var segment in segments)
        {
            if (string.Equals(segment, previous, StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine(segment);
            previous = segment;
        }

        return builder.ToString().TrimEnd();
    }
}
