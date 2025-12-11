namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Represents the outcome of sanitizing an exception.
/// </summary>
/// <param name="Detail">The safe detail string.</param>
/// <param name="IncludeStackTrace">Indicates whether the stack trace may be emitted.</param>
/// <param name="IsRedacted">Indicates whether any portion of the exception was redacted.</param>
public sealed record ExceptionSanitizationResult(string Detail, bool IncludeStackTrace, bool IsRedacted);
