using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Errors.Logging.Internal;

internal static class LoggingBuilderConfigurator
{
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
