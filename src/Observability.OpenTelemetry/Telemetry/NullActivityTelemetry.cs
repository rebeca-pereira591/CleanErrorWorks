using System.Diagnostics;

namespace Observability.OpenTelemetry.Telemetry;

public sealed class NullActivityTagger : IActivityTagger
{
    public void Apply(Activity activity, ActivityTagContext context)
    {
    }
}

public sealed class NullActivityEventFactory : IActivityEventFactory
{
    public ActivityEvent CreateExceptionEvent(Exception exception, ActivityExceptionEventContext context)
        => new("exception");

    public ActivityEvent CreateProblemDetailsEvent(ActivityProblemDetailsContext context)
        => new("problem-details");
}
