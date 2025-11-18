using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logging.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoggingConventions(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddJsonConsole(o =>
            {
                o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
                o.IncludeScopes = true;
                o.JsonWriterOptions = new() { Indented = false };
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return services;
    }
}