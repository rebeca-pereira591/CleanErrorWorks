namespace Errors.AspNetCore.Formatters;

/// <summary>
/// Represents the identifiers generated while formatting a ProblemDetails response.
/// </summary>
/// <param name="ErrorId">Server-generated error identifier.</param>
/// <param name="ErrorCode">Domain-specific code extracted from extensions.</param>
public sealed record ProblemDetailsFormattingResult(string ErrorId, string ErrorCode);
