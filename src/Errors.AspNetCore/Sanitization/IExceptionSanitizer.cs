using Microsoft.AspNetCore.Http;

namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Sanitizes exception details to remove sensitive information before surfacing to clients.
/// By default the implementation returns the full exception message and stack trace; consumers can opt in to redaction via <see cref="ExceptionSanitizerOptions"/>.
/// </summary>
public interface IExceptionSanitizer
{
    /// <summary>
    /// Sanitizes the supplied exception and returns a safe detail string.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <param name="exception">Exception to sanitize.</param>
    /// <param name="preferredDetail">Preferred detail string if available.</param>
    /// <param name="treatPreferredDetailAsSensitive">True to treat the preferred detail as sensitive.</param>
    /// <returns>The sanitized detail plus a stack-trace inclusion flag.</returns>
    ExceptionSanitizationResult Sanitize(HttpContext httpContext, Exception exception, string? preferredDetail = null, bool treatPreferredDetailAsSensitive = true);
}
