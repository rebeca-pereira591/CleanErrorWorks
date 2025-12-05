using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Observability.OpenTelemetry.Telemetry;
using System.Diagnostics;
using System.Net;

namespace Errors.AspNetCore.Enrichers;

/// <summary>
/// Default implementation that annotates <see cref="Activity"/> instances with error metadata and ProblemDetails extensions.
/// </summary>
/// <remarks>
/// Adds <c>exception.*</c> (message, stack trace, source) attributes in Development/Demo environments plus <c>problem.*</c> fields
/// (type, title, status, detail, code, category, error id, trace id, instance, SQL error number) so OTLP exporters surface the full payload.
/// </remarks>
public sealed class ActivitySpanEnricher(
    IActivityTagger tagger,
    IActivityEventFactory eventFactory,
    ActivitySource activitySource,
    IExceptionSanitizer sanitizer,
    IHostEnvironment environment) : ISpanEnricher
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

        var tagContext = new ActivityTagContext(
            httpContext.TraceIdentifier,
            httpContext.Request.Path.Value ?? string.Empty,
            (int)statusCode,
            errorId,
            errorCode,
            problemDetails.Type,
            problemDetails.Title,
            environment.EnvironmentName);

        tagger.Apply(activity, tagContext);

        var sanitized = sanitizer.Sanitize(httpContext, exception, problemDetails.Detail, treatPreferredDetailAsSensitive: false);
        var isDeveloperEnv = IsDeveloperExperienceEnvironment(environment);

        AddExceptionTags(activity, exception, sanitized, isDeveloperEnv);
        AddProblemDetailsTags(activity, problemDetails, statusCode, errorId, httpContext.TraceIdentifier);

        var exceptionEventContext = new ActivityExceptionEventContext(
            errorId,
            errorCode,
            environment.EnvironmentName,
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

    private static bool IsDeveloperExperienceEnvironment(IHostEnvironment environment)
        => environment.IsDevelopment()
           || string.Equals(environment.EnvironmentName, "Demo", StringComparison.OrdinalIgnoreCase);

    private static void AddExceptionTags(Activity activity, Exception exception, ExceptionSanitizationResult sanitized, bool includeSensitive)
    {
        var safeMessage = includeSensitive ? exception.Message : sanitized.Detail;
        if (!string.IsNullOrWhiteSpace(safeMessage))
        {
            activity.SetTag("exception.message", safeMessage);
        }

        if (includeSensitive && sanitized.IncludeStackTrace && !string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            activity.SetTag("exception.stacktrace", exception.StackTrace);
        }

        if (includeSensitive && !string.IsNullOrWhiteSpace(exception.Source))
        {
            activity.SetTag("exception.source", exception.Source);
        }
    }

    private static void AddProblemDetailsTags(Activity activity, ProblemDetails problemDetails, HttpStatusCode fallbackStatus, string errorId, string traceId)
    {
        SetTagIfValue(activity, "problem.type", problemDetails.Type);
        SetTagIfValue(activity, "problem.title", problemDetails.Title);
        SetTagIfValue(activity, "problem.status", problemDetails.Status ?? (int)fallbackStatus);
        SetTagIfValue(activity, "problem.detail", problemDetails.Detail);
        SetTagIfValue(activity, "problem.instance", problemDetails.Instance);
        SetTagIfValue(activity, "problem.error_id", errorId);
        SetTagIfValue(activity, "problem.trace_id", traceId);

        SetExtensionTag(activity, problemDetails, "code", "problem.code");
        SetExtensionTag(activity, problemDetails, "category", "problem.category");
        SetExtensionTag(activity, problemDetails, "errorId", "problem.error_id");
        SetExtensionTag(activity, problemDetails, "traceId", "problem.trace_id");
        SetExtensionTag(activity, problemDetails, "sqlErrorNumber", "problem.sql_error_number");
    }

    private static void SetExtensionTag(Activity activity, ProblemDetails problemDetails, string extensionKey, string tagName)
    {
        if (!problemDetails.Extensions.TryGetValue(extensionKey, out var value) || value is null)
        {
            return;
        }

        if (value is string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return;
            activity.SetTag(tagName, str);
            return;
        }

        if (value is int or long or double or float or bool)
        {
            activity.SetTag(tagName, value);
            return;
        }

        activity.SetTag(tagName, value.ToString());
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
}
