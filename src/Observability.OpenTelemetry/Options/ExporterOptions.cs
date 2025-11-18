namespace Observability.OpenTelemetry.Options;

public sealed class ExporterOptions
{
    public OtlpExporterOptions Otlp { get; } = new() { Enabled = true, Endpoint = "http://localhost:4318" };

    public TempoExporterOptions Tempo { get; } = new();

    public ApplicationInsightsExporterOptions ApplicationInsights { get; } = new();

    public bool ConsoleEnabled { get; set; }
        = false;
}

public class OtlpExporterOptions
{
    public bool Enabled { get; set; }
        = false;

    public string Protocol { get; set; } = "http"; // http | grpc

    public string Endpoint { get; set; } = "http://localhost:4318";

    public string? Headers { get; set; }
        = null;
}

public sealed class TempoExporterOptions : OtlpExporterOptions
{
    public TempoExporterOptions()
    {
        Endpoint = "http://localhost:4318";
    }
}

public sealed class ApplicationInsightsExporterOptions
{
    public bool Enabled { get; set; }
        = false;

    public string? ConnectionString { get; set; }
        = null;
}
