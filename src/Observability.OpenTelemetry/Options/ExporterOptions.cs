namespace Observability.OpenTelemetry.Options;

/// <summary>
/// Groups exporter-specific settings for tracing and metrics.
/// </summary>
public sealed class ExporterOptions
{
    public OtlpExporterOptions Otlp { get; } = new() { Enabled = true, Endpoint = "http://localhost:4318" };

    public TempoExporterOptions Tempo { get; } = new();

    public ApplicationInsightsExporterOptions ApplicationInsights { get; } = new();

    public bool ConsoleEnabled { get; set; }
        = false;
}

/// <summary>
/// Represents settings for an OTLP exporter target.
/// </summary>
public class OtlpExporterOptions
{
    public bool Enabled { get; set; }
        = false;

    public string Protocol { get; set; } = "http"; // http | grpc

    public string Endpoint { get; set; } = "http://localhost:4318";

    public string? Headers { get; set; }
        = null;
}

/// <summary>
/// Provides strongly typed defaults for Tempo-compatible OTLP export.
/// </summary>
public sealed class TempoExporterOptions : OtlpExporterOptions
{
    public TempoExporterOptions()
    {
        Endpoint = "http://localhost:4318";
    }
}

/// <summary>
/// Configures Azure Application Insights export behavior.
/// </summary>
public sealed class ApplicationInsightsExporterOptions
{
    public bool Enabled { get; set; }
        = false;

    public string? ConnectionString { get; set; }
        = null;
}
