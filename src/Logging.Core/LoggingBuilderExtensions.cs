using Errors.Logging.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Errors.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddDefaultLogging(
        this ILoggingBuilder builder,
        IConfiguration? configuration = null,
        Action<LoggingConventionOptions>? configure = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));

        var options = new LoggingConventionOptions();
        configure?.Invoke(options);

        LoggingBuilderConfigurator.Apply(builder, configuration, options);
        return builder;
    }
}
