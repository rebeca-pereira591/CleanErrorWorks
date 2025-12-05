namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Defines how exception details should be redacted or exposed.
/// </summary>
/// <remarks>
/// The default sanitizer treats both <c>Development</c> and <c>Demo</c> environments as developer-facing,
/// which means stack traces, the full exception message, and the exception source are surfaced automatically.
/// </remarks>
public sealed class ExceptionSanitizerOptions
{
    public string RedactedDetail { get; set; } = "An unexpected error occurred.";

    public bool IncludeStackTraceInDevelopment { get; set; } = true;

    public ISet<string> SafeExceptionTypeNames { get; } = new HashSet<string>
    {
        typeof(Errors.Abstractions.Exceptions.DomainException).FullName!,
        typeof(Errors.Abstractions.Exceptions.ValidationException).FullName!,
        typeof(Errors.Abstractions.Exceptions.AuthorizationException).FullName!,
        typeof(Errors.Abstractions.Exceptions.RateLimitException).FullName!,
        typeof(Errors.Abstractions.Exceptions.NotFoundException).FullName!,
        typeof(Errors.Abstractions.IAppError).FullName!
    };

    public Func<Exception, bool>? AllowExceptionDetails { get; set; }
        = ex => ex is Errors.Abstractions.IAppError;
}
