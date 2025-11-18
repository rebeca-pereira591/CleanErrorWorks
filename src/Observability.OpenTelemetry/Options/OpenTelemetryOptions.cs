namespace Observability.OpenTelemetry.Options;

public sealed class OpenTelemetryOptions
{
    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? ServiceInstanceId { get; set; } = System.Environment.MachineName;
    public string Environment { get; set; } = "Production";

    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;

    public SamplingOptions Sampling { get; } = new();
    public ExporterOptions Exporters { get; } = new();
    public IDictionary<string, object> ResourceAttributes { get; } = new Dictionary<string, object>();

    public IList<string> ActivitySources { get; } = new List<string>();
    public IList<Action<global::OpenTelemetry.Trace.TracerProviderBuilder>> TracingEnrichers { get; } = new List<Action<global::OpenTelemetry.Trace.TracerProviderBuilder>>();
    public IList<Action<global::OpenTelemetry.Metrics.MeterProviderBuilder>> MetricsEnrichers { get; } = new List<Action<global::OpenTelemetry.Metrics.MeterProviderBuilder>>();

    public string ActivitySourceName => ServiceName ?? AppDomain.CurrentDomain.FriendlyName;

    public OpenTelemetryOptions()
    {
        ResourceAttributes["deployment.environment"] = Environment;
    }
}
