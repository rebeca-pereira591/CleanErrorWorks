namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Defines how exception details should be redacted or exposed.
/// </summary>
/// <remarks>
/// By default all error details are surfaced (full message, source, and stack trace). Consumers can opt in to redaction
/// in Production environments by toggling the provided flags.
/// </remarks>
public sealed class ExceptionSanitizerOptions
{
    public string RedactedDetail { get; set; } = "An unexpected error occurred.";

    /// <summary>
    /// Gets or sets a value indicating whether telemetry/logs should be sanitized (defaults to false for maximum insight).
    /// </summary>
    public bool SanitizeTelemetry { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether API responses should be sanitized (defaults to true for safety).
    /// </summary>
    public bool SanitizeApiResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether stack traces should be redacted when sanitization is enabled.
    /// </summary>
    public bool RedactStackTraces { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether preferred detail strings are treated as sensitive.
    /// When true, details are only exposed for types listed in <see cref="SafeExceptionTypeNames"/> or
    /// when <see cref="AllowExceptionDetails"/> evaluates to true.
    /// </summary>
    public bool TreatPreferredDetailAsSensitive { get; set; } = false;

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
