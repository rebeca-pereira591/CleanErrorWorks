using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Errors.Logging;

/// <summary>
/// Provides DI-friendly helpers for registering logging defaults across services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default logging pipeline and applies optional configuration delegates.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">Optional configuration source for logging sections.</param>
    /// <param name="configure">Optional callback that mutates <see cref="LoggingConventionOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDefaultLogging(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<LoggingConventionOptions>? configure = null)
    {
        services.AddLogging(builder => builder.AddDefaultLogging(configuration, configure));
        return services;
    }
}
