using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Errors.Logging.Internal;

internal static class LoggingBuilderConfigurator
{
    /// <summary>
    /// Applies the configured conventions to the provided <see cref="ILoggingBuilder"/>.
    /// </summary>
    /// <param name="builder">Builder being configured.</param>
    /// <param name="configuration">Optional configuration source.</param>
    /// <param name="options">Convention options.</param>
    public static void Apply(ILoggingBuilder builder, IConfiguration? configuration, LoggingConventionOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        options ??= new LoggingConventionOptions();

        if (options.ClearProviders)
        {
            builder.ClearProviders();
        }

        if (options.UseConfiguration && configuration is not null)
        {
            var section = configuration.GetSection(options.ConfigurationSectionName);
            if (section.Exists())
            {
                builder.AddConfiguration(section);
            }
        }

        foreach (var provider in options.Providers)
        {
            provider(builder);
        }

        builder.SetMinimumLevel(options.MinimumLevel);

        options.ConfigureBuilder?.Invoke(builder);
    }
}
