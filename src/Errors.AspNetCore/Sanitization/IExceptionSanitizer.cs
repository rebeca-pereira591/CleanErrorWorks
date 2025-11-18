using Microsoft.AspNetCore.Http;

namespace Errors.AspNetCore.Sanitization;

public interface IExceptionSanitizer
{
    ExceptionSanitizationResult Sanitize(HttpContext httpContext, Exception exception, string? preferredDetail = null, bool treatPreferredDetailAsSensitive = true);
}
