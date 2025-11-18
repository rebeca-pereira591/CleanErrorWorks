using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Errors.Logging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultLogging(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<LoggingConventionOptions>? configure = null)
    {
        services.AddLogging(builder => builder.AddDefaultLogging(configuration, configure));
        return services;
    }
}
