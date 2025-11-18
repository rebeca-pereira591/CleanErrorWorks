namespace Observability.OpenTelemetry.Options;

public sealed class SamplingOptions
{
    public SamplingStrategy Strategy { get; set; } = SamplingStrategy.ParentBasedTraceIdRatio;

    public double Probability { get; set; } = 1.0d;
}

public enum SamplingStrategy
{
    AlwaysOn,
    AlwaysOff,
    TraceIdRatio,
    ParentBasedTraceIdRatio
}
