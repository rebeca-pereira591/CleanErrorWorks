using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;

namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Default implementation that exposes full exception details unless consumers opt in to redaction via <see cref="ExceptionSanitizerOptions"/>.
/// </summary>
public sealed class ExceptionSanitizer : IExceptionSanitizer
{
    private readonly ExceptionSanitizerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionSanitizer"/> class.
    /// </summary>
    /// <param name="options">Sanitizer options.</param>
    public ExceptionSanitizer(IOptions<ExceptionSanitizerOptions> options)
    {
        _options = options.Value ?? new ExceptionSanitizerOptions();
    }

    public ExceptionSanitizationResult Sanitize(HttpContext httpContext, Exception exception, string? preferredDetail = null, bool treatPreferredDetailAsSensitive = true)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var treatSensitivePreference = treatPreferredDetailAsSensitive || _options.TreatPreferredDetailAsSensitive;
        var includeSensitiveDetails = !treatSensitivePreference || ShouldAllowDetails(exception);

        var detail = includeSensitiveDetails
            ? BuildDetailedMessage(preferredDetail, exception)
            : _options.RedactedDetail;

        var includeStackTrace = includeSensitiveDetails && !_options.RedactStackTraces;

        var isRedacted = !includeSensitiveDetails || _options.RedactStackTraces;
        return new ExceptionSanitizationResult(detail, includeStackTrace, isRedacted);
    }

    private bool ShouldAllowDetails(Exception exception)
    {
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

    private string BuildDetailedMessage(string? preferredDetail, Exception exception)
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
        if (!string.IsNullOrWhiteSpace(preferredDetail))
        {
            return $"{detailBase} (Source: {source})";
        }

        if (_options.SanitizeApiResponses && !_options.SanitizeTelemetry)
        {
            return exceptionMessage;
        }

        return $"{detailBase} (Source: {source})";
    }
}
