using System.Diagnostics;

namespace Observability.OpenTelemetry.Telemetry;

/// <summary>
/// Applies span tags that describe HTTP and error context.
/// </summary>
public interface IActivityTagger
{
    /// <summary>
    /// Applies tags to the supplied <see cref="Activity"/> instance.
    /// </summary>
    /// <param name="activity">Activity to enrich.</param>
    /// <param name="context">Metadata describing the current request.</param>
    void Apply(Activity activity, ActivityTagContext context);
}

/// <summary>
/// Creates OpenTelemetry-friendly <see cref="ActivityEvent"/> instances.
/// </summary>
public interface IActivityEventFactory
{
    /// <summary>
    /// Creates an event containing exception metadata.
    /// </summary>
    /// <param name="exception">Exception being logged.</param>
    /// <param name="context">Supporting error metadata.</param>
    /// <returns>An <see cref="ActivityEvent"/> instance.</returns>
    ActivityEvent CreateExceptionEvent(Exception exception, ActivityExceptionEventContext context);

    /// <summary>
    /// Creates an event that describes the produced ProblemDetails payload.
    /// </summary>
    /// <param name="context">Problem details values to record.</param>
    /// <returns>An <see cref="ActivityEvent"/> instance.</returns>
    ActivityEvent CreateProblemDetailsEvent(ActivityProblemDetailsContext context);
}
