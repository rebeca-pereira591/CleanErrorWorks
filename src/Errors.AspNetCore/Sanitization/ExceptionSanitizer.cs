using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Errors.AspNetCore.Sanitization;

public sealed class ExceptionSanitizer : IExceptionSanitizer
{
    private readonly IHostEnvironment _environment;
    private readonly ExceptionSanitizerOptions _options;

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

        var includeStackTrace = includeSensitiveDetails && _options.IncludeStackTraceInDevelopment && _environment.IsDevelopment();

        return new ExceptionSanitizationResult(detail, includeStackTrace);
    }

    private string ResolveDetail(string? preferredDetail, Exception exception, bool includeSensitiveDetails, bool treatPreferredDetailAsSensitive)
    {
        if (!string.IsNullOrWhiteSpace(preferredDetail))
        {
            if (!treatPreferredDetailAsSensitive || includeSensitiveDetails)
            {
                return preferredDetail;
            }

            return _options.RedactedDetail;
        }

        return includeSensitiveDetails ? exception.Message : _options.RedactedDetail;
    }

    private bool ShouldIncludeSensitiveDetails(Exception exception)
    {
        if (_environment.IsDevelopment())
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
}
