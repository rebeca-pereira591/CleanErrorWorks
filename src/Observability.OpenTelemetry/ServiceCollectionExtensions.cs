using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Observability.OpenTelemetry.Telemetry;
using Observability.OpenTelemetry.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using OtlpExporterOptionsBuilder = OpenTelemetry.Exporter.OtlpExporterOptions;

namespace Observability.OpenTelemetry;

/// <summary>
/// Provides extension methods for wiring the CleanErrorWorks OpenTelemetry defaults.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds tracing and metrics instrumentation configured via <see cref="OpenTelemetryOptions"/>.
    /// </summary>
    /// <param name="services">Service collection being configured.</param>
    /// <param name="configuration">Optional configuration source bound to <c>OpenTelemetry</c>.</param>
    /// <param name="configure">Optional callback for programmatic adjustments.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDefaultOpenTelemetry(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<OpenTelemetryOptions>? configure = null)
    {
        var options = new OpenTelemetryOptions();
        if (configuration is not null)
        {
            configuration.GetSection("OpenTelemetry").Bind(options);
        }
        configure?.Invoke(options);

        options.ResourceAttributes["deployment.environment"] = options.Environment;

        services.AddSingleton(options);
        services.AddSingleton<IOptions<OpenTelemetryOptions>>(Microsoft.Extensions.Options.Options.Create(options));

        services.RemoveAll<ActivitySource>();
        services.TryAddSingleton(_ => new ActivitySource(options.ActivitySourceName));

        services.TryAddSingleton<IActivityTagger, ActivityTagger>();
        services.TryAddSingleton<IActivityEventFactory, ActivityEventFactory>();

        var resourceBuilder = BuildResource(options);

        var otelBuilder = services.AddOpenTelemetry();
        otelBuilder.ConfigureResource(rb => rb.AddAttributes(resourceBuilder.Build().Attributes));

        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(builder => ConfigureTracing(builder, options, resourceBuilder));
        }

        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(builder => ConfigureMetrics(builder, options, resourceBuilder));
        }

        return services;
    }

    private static ResourceBuilder BuildResource(OpenTelemetryOptions options)
    {
        var resource = ResourceBuilder.CreateDefault()
            .AddService(options.ServiceName ?? AppDomain.CurrentDomain.FriendlyName,
                serviceVersion: options.ServiceVersion ?? typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: options.ServiceInstanceId);

        resource.AddAttributes(options.ResourceAttributes.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)));
        return resource;
    }

    private static void ConfigureTracing(TracerProviderBuilder builder, OpenTelemetryOptions options, ResourceBuilder resource)
    {
        builder.SetResourceBuilder(resource)
               .AddSource(options.ActivitySourceName)
               .SetSampler(CreateSampler(options.Sampling))
               .AddAspNetCoreInstrumentation(o => o.RecordException = true)
               .AddHttpClientInstrumentation();

        foreach (var additional in options.ActivitySources)
        {
            builder.AddSource(additional);
        }

        ConfigureTracingExporters(builder, options);

        foreach (var enricher in options.TracingEnrichers)
        {
            enricher(builder);
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder builder, OpenTelemetryOptions options, ResourceBuilder resource)
    {
        builder.SetResourceBuilder(resource)
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation();

        ConfigureMetricExporters(builder, options);

        foreach (var enricher in options.MetricsEnrichers)
        {
            enricher(builder);
        }
    }

    private static Sampler CreateSampler(SamplingOptions options)
        => options.Strategy switch
        {
            SamplingStrategy.AlwaysOff => new AlwaysOffSampler(),
            SamplingStrategy.TraceIdRatio => new TraceIdRatioBasedSampler(options.Probability),
            SamplingStrategy.ParentBasedTraceIdRatio => new ParentBasedSampler(new TraceIdRatioBasedSampler(options.Probability)),
            _ => new AlwaysOnSampler()
        };

    private static void ConfigureTracingExporters(TracerProviderBuilder builder, OpenTelemetryOptions options)
    {
        if (options.Exporters.Otlp.Enabled)
        {
            builder.AddOtlpExporter(o => ConfigureOtlp(o, options.Exporters.Otlp, isMetrics: false));
        }

        if (options.Exporters.Tempo.Enabled)
        {
            builder.AddOtlpExporter(o => ConfigureOtlp(o, options.Exporters.Tempo, isMetrics: false));
        }

        if (options.Exporters.ConsoleEnabled)
        {
            builder.AddConsoleExporter();
        }

        if (options.Exporters.ApplicationInsights.Enabled && !string.IsNullOrWhiteSpace(options.Exporters.ApplicationInsights.ConnectionString))
        {
            builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = options.Exporters.ApplicationInsights.ConnectionString);
        }
    }

    private static void ConfigureMetricExporters(MeterProviderBuilder builder, OpenTelemetryOptions options)
    {
        if (options.Exporters.Otlp.Enabled)
        {
            builder.AddOtlpExporter(o => ConfigureOtlp(o, options.Exporters.Otlp, isMetrics: true));
        }

        if (options.Exporters.Tempo.Enabled)
        {
            builder.AddOtlpExporter(o => ConfigureOtlp(o, options.Exporters.Tempo, isMetrics: true));
        }

        if (options.Exporters.ConsoleEnabled)
        {
            builder.AddConsoleExporter();
        }

        if (options.Exporters.ApplicationInsights.Enabled && !string.IsNullOrWhiteSpace(options.Exporters.ApplicationInsights.ConnectionString))
        {
            builder.AddAzureMonitorMetricExporter(o => o.ConnectionString = options.Exporters.ApplicationInsights.ConnectionString);
        }
    }

    private static void ConfigureOtlp(OtlpExporterOptionsBuilder exporterOptionsBuilder, Options.OtlpExporterOptions options, bool isMetrics)
    {
        exporterOptionsBuilder.Protocol = ParseProtocol(options.Protocol);
        var endpoint = options.Endpoint?.TrimEnd('/') ?? "http://localhost:4318";

        if (exporterOptionsBuilder.Protocol == OtlpExportProtocol.HttpProtobuf)
        {
            var suffix = isMetrics ? "/v1/metrics" : "/v1/traces";
            if (!endpoint.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
            {
                endpoint += suffix;
            }
        }

        exporterOptionsBuilder.Endpoint = new Uri(endpoint);
        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            exporterOptionsBuilder.Headers = options.Headers;
        }
    }

    private static OtlpExportProtocol ParseProtocol(string? protocol)
    {
        if (string.Equals(protocol, "grpc", StringComparison.OrdinalIgnoreCase))
        {
            return OtlpExportProtocol.Grpc;
        }

        return OtlpExportProtocol.HttpProtobuf;
    }
}
