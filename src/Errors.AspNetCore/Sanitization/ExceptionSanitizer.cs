using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Default implementation that redacts sensitive details unless configured otherwise.
/// </summary>
/// <remarks>
/// Treats <c>Development</c> and <c>Demo</c> hosts as developer experience environments, exposing the exception message, source,
/// and stack trace so enriched telemetry can be inspected in Jaeger/Zipkin/Tempo, while Production stays redacted.
/// </remarks>
public sealed class ExceptionSanitizer : IExceptionSanitizer
{
    private readonly IHostEnvironment _environment;
    private readonly ExceptionSanitizerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionSanitizer"/> class.
    /// </summary>
    /// <param name="environment">Host environment used for development detection.</param>
    /// <param name="options">Sanitizer options.</param>
    public ExceptionSanitizer(IHostEnvironment environment, IOptions<ExceptionSanitizerOptions> options)
    {
        _environment = environment;
        _options = options.Value ?? new ExceptionSanitizerOptions();
    }

    public ExceptionSanitizationResult Sanitize(HttpContext httpContext, Exception exception, string? preferredDetail = null, bool treatPreferredDetailAsSensitive = true)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var includeSensitiveDetails = ShouldIncludeSensitiveDetails(exception);
        var detail = ResolveDetail(preferredDetail, exception, includeSensitiveDetails, treatPreferredDetailAsSensitive);

        var includeStackTrace = includeSensitiveDetails && _options.IncludeStackTraceInDevelopment && IsDeveloperExperienceEnvironment();

        return new ExceptionSanitizationResult(detail, includeStackTrace);
    }

    private string ResolveDetail(string? preferredDetail, Exception exception, bool includeSensitiveDetails, bool treatPreferredDetailAsSensitive)
    {
        if (!string.IsNullOrWhiteSpace(preferredDetail))
        {
            if (!treatPreferredDetailAsSensitive || includeSensitiveDetails)
            {
                return includeSensitiveDetails
                    ? BuildDetailedMessage(preferredDetail, exception)
                    : preferredDetail;
            }

            return _options.RedactedDetail;
        }

        return includeSensitiveDetails
            ? BuildDetailedMessage(null, exception)
            : _options.RedactedDetail;
    }

    private bool ShouldIncludeSensitiveDetails(Exception exception)
    {
        if (IsDeveloperExperienceEnvironment())
        {
            return true;
        }

        if (_options.SafeExceptionTypeNames.Contains(exception.GetType().FullName ?? string.Empty))
        {
            return true;
        }

        if (_options.AllowExceptionDetails is not null && _options.AllowExceptionDetails(exception))
        {
            return true;
        }

        return false;
    }

    private bool IsDeveloperExperienceEnvironment()
        => _environment.IsDevelopment()
           || string.Equals(_environment.EnvironmentName, "Demo", StringComparison.OrdinalIgnoreCase);

    private static string BuildDetailedMessage(string? preferredDetail, Exception exception)
    {
        var exceptionMessage = string.IsNullOrWhiteSpace(exception.Message)
            ? "An exception was thrown."
            : exception.Message;

        var detailBase = preferredDetail;
        if (string.IsNullOrWhiteSpace(detailBase))
        {
            detailBase = exceptionMessage;
        }
        else if (!detailBase.Contains(exceptionMessage, StringComparison.Ordinal))
        {
            detailBase = $"{detailBase} | {exceptionMessage}";
        }

        var source = string.IsNullOrWhiteSpace(exception.Source) ? "unknown" : exception.Source;
        return $"{detailBase} (Source: {source})";
    }
}
