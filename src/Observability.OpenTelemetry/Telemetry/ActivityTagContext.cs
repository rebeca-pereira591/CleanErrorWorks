namespace Observability.OpenTelemetry.Telemetry;

/// <summary>
/// Describes the HTTP and error context needed to apply Activity tags.
/// </summary>
public sealed record ActivityTagContext(
    string TraceIdentifier,
    string RequestPath,
    int StatusCode,
    string ErrorId,
    string ErrorCode,
    string? ProblemType,
    string? ProblemTitle,
    string EnvironmentName);

/// <summary>
/// Holds ProblemDetails information for generating telemetry events.
/// </summary>
public sealed record ActivityProblemDetailsContext(
    string ErrorId,
    string ErrorCode,
    string? ProblemType,
    string? ProblemTitle,
    int StatusCode,
    string RequestPath);

/// <summary>
/// Represents the metadata required to build exception-related Activity events.
/// </summary>
public sealed record ActivityExceptionEventContext(
    string ErrorId,
    string ErrorCode,
    string EnvironmentName,
    string Detail,
    bool IncludeStackTrace);
