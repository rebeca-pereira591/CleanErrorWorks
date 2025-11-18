using System.Diagnostics;

namespace Observability.OpenTelemetry.Telemetry;

public interface IActivityTagger
{
    void Apply(Activity activity, ActivityTagContext context);
}

public interface IActivityEventFactory
{
    ActivityEvent CreateExceptionEvent(Exception exception, ActivityExceptionEventContext context);

    ActivityEvent CreateProblemDetailsEvent(ActivityProblemDetailsContext context);
}
