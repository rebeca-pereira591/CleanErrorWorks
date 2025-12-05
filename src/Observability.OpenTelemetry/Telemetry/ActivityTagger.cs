using Microsoft.Extensions.Options;
using Observability.OpenTelemetry.Options;
using System.Diagnostics;

namespace Observability.OpenTelemetry.Telemetry;

/// <summary>
/// Default strategy that enriches <see cref="Activity"/> instances with CleanErrorWorks specific tags.
/// </summary>
public sealed class ActivityTagger(IOptions<OpenTelemetryOptions> optionsAccessor) : IActivityTagger
{
    private readonly string _environmentName = optionsAccessor.Value.Environment;

    public void Apply(Activity activity, ActivityTagContext context)
    {
        if (activity is null) return;

        activity.SetTag("cleanerrorworks.trace_id", context.TraceIdentifier);
        activity.SetTag("http.request.path", context.RequestPath);
        activity.SetTag("http.response.status_code", context.StatusCode);
        activity.SetTag("cleanerrorworks.error.id", context.ErrorId);
        activity.SetTag("cleanerrorworks.error.code", context.ErrorCode);
        if (!string.IsNullOrWhiteSpace(context.ProblemType))
            activity.SetTag("cleanerrorworks.problem.type", context.ProblemType);
        if (!string.IsNullOrWhiteSpace(context.ProblemTitle))
            activity.SetTag("cleanerrorworks.problem.title", context.ProblemTitle);

        activity.SetTag("deployment.environment", context.EnvironmentName ?? _environmentName);
    }
}

/// <summary>
/// Produces <see cref="ActivityEvent"/> instances that capture exception and ProblemDetails information.
/// </summary>
public sealed class ActivityEventFactory(IOptions<OpenTelemetryOptions> optionsAccessor) : IActivityEventFactory
{
    private readonly string _environmentName = optionsAccessor.Value.Environment;

    public ActivityEvent CreateExceptionEvent(Exception exception, ActivityExceptionEventContext context)
    {
        var tags = new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = context.Detail,
            ["error.id"] = context.ErrorId,
            ["error.code"] = context.ErrorCode,
            ["deployment.environment"] = context.EnvironmentName ?? _environmentName
        };

        if (context.IncludeStackTrace)
        {
            tags["exception.stacktrace"] = exception.ToString();
        }

        return new ActivityEvent("exception", tags: tags);
    }

    public ActivityEvent CreateProblemDetailsEvent(ActivityProblemDetailsContext context)
    {
        var tags = new ActivityTagsCollection
        {
            ["error.id"] = context.ErrorId,
            ["error.code"] = context.ErrorCode,
            ["http.response.status_code"] = context.StatusCode,
            ["http.request.path"] = context.RequestPath
        };

        if (!string.IsNullOrWhiteSpace(context.ProblemType))
            tags["problem.type"] = context.ProblemType;
        if (!string.IsNullOrWhiteSpace(context.ProblemTitle))
            tags["problem.title"] = context.ProblemTitle;

        return new ActivityEvent("problem-details", tags: tags);
    }
}
