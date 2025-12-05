using System.Diagnostics;

namespace Observability.OpenTelemetry.Telemetry;

/// <summary>
/// No-op implementation used when telemetry services are not registered.
/// </summary>
public sealed class NullActivityTagger : IActivityTagger
{
    public void Apply(Activity activity, ActivityTagContext context)
    {
    }
}

/// <summary>
/// No-op event factory that avoids null checks when OpenTelemetry is disabled.
/// </summary>
public sealed class NullActivityEventFactory : IActivityEventFactory
{
    public ActivityEvent CreateExceptionEvent(Exception exception, ActivityExceptionEventContext context)
        => new("exception");

    public ActivityEvent CreateProblemDetailsEvent(ActivityProblemDetailsContext context)
        => new("problem-details");
}
