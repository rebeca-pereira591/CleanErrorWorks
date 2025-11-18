using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Observability.OpenTelemetry.Telemetry;
using System;
using System.Diagnostics;
using System.Net;

namespace Errors.AspNetCore.Enrichers;

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
}
