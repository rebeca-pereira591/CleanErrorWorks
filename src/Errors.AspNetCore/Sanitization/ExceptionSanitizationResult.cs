namespace Errors.AspNetCore.Sanitization;

public sealed record ExceptionSanitizationResult(string Detail, bool IncludeStackTrace);
