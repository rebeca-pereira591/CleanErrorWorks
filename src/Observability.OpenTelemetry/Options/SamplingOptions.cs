namespace Observability.OpenTelemetry.Options;

/// <summary>
/// Controls how traces are sampled before export.
/// </summary>
public sealed class SamplingOptions
{
    public SamplingStrategy Strategy { get; set; } = SamplingStrategy.ParentBasedTraceIdRatio;

    public double Probability { get; set; } = 1.0d;
}

/// <summary>
/// Lists the supported sampling strategies exposed via <see cref="SamplingOptions"/>.
/// </summary>
public enum SamplingStrategy
{
    AlwaysOn,
    AlwaysOff,
    TraceIdRatio,
    ParentBasedTraceIdRatio
}
