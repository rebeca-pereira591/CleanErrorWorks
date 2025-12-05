namespace Errors.AspNetCore.Sanitization;

/// <summary>
/// Represents the outcome of sanitizing an exception.
/// </summary>
/// <param name="Detail">The safe detail string.</param>
/// <param name="IncludeStackTrace">Indicates whether the stack trace may be emitted.</param>
public sealed record ExceptionSanitizationResult(string Detail, bool IncludeStackTrace);
