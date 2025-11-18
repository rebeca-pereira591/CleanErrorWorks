using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Observability.OpenTelemetry;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultOpenTelemetry(
        this IServiceCollection services,
        IConfiguration config,
        string? serviceName = null)
    {
        var name = serviceName ?? AppDomain.CurrentDomain.FriendlyName;

        var resource = ResourceBuilder.CreateDefault().AddService(
            serviceName: name,
            serviceVersion: typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            serviceInstanceId: Environment.MachineName);

        // Lee endpoint y protocolo desde config (opcional)
        var endpoint = config["OpenTelemetry:Otlp:Endpoint"]; // p.ej. http://localhost:4318  (base)
        var protoStr = config["OpenTelemetry:Otlp:Protocol"]; // "http" | "grpc" (opcional)

        // Default: HTTP/Protobuf a 4318
        var useHttp = string.Equals(protoStr, "http", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(protoStr);

        services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(name))
            .WithTracing(b =>
            {
                b.SetResourceBuilder(resource)
                 .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                 .AddHttpClientInstrumentation()
                 .AddOtlpExporter(o =>
                 {
                     if (useHttp)
                     {
                         o.Protocol = OtlpExportProtocol.HttpProtobuf;
                         var baseUrl = endpoint ?? "http://localhost:4318";
                         // 👇 si no trae /v1/traces, lo agregamos
                         if (!baseUrl.Contains("/v1/"))
                             baseUrl = baseUrl.TrimEnd('/') + "/v1/traces";
                         o.Endpoint = new Uri(baseUrl);
                     }
                     else
                     {
                         o.Protocol = OtlpExportProtocol.Grpc;
                         o.Endpoint = new Uri(endpoint ?? "http://localhost:4317");
                     }
                 });
            })
            .WithMetrics(b =>
            {
                b.SetResourceBuilder(resource)
                 .AddRuntimeInstrumentation()
                 .AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddOtlpExporter(o =>
                 {
                     if (useHttp)
                     {
                         o.Protocol = OtlpExportProtocol.HttpProtobuf;
                         var baseUrl = endpoint ?? "http://localhost:4318";
                         // 👇 para métricas, /v1/metrics
                         if (!baseUrl.Contains("/v1/"))
                             baseUrl = baseUrl.TrimEnd('/') + "/v1/metrics";
                         o.Endpoint = new Uri(baseUrl);
                     }
                     else
                     {
                         o.Protocol = OtlpExportProtocol.Grpc;
                         o.Endpoint = new Uri(endpoint ?? "http://localhost:4317");
                     }
                 });
            });

        return services;
    }
}