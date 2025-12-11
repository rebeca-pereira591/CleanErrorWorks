using Errors.Logging.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Errors.Logging;

/// <summary>
/// Extends <see cref="ILoggingBuilder"/> with CleanErrorWorks defaults.
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Applies common providers, minimum levels, and optional configuration bindings.
    /// </summary>
    /// <param name="builder">Target logging builder.</param>
    /// <param name="configuration">Optional configuration source for <c>Logging</c> section.</param>
    /// <param name="configure">Optional callback to tweak <see cref="LoggingConventionOptions"/>.</param>
    /// <returns>The same <see cref="ILoggingBuilder"/>.</returns>
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
