using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Sanitization;

namespace Errors.AspNetCore.Extensions;

public sealed class ErrorHandlingOptions
{
    public Action<ExceptionSanitizerOptions>? ConfigureExceptionSanitizer { get; set; }
        = null;

    public Action<ProblemDetailsExtensionValidationOptions>? ConfigureProblemDetailsExtensions { get; set; }
        = null;
}
