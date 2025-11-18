namespace Observability.OpenTelemetry.Telemetry;

public sealed record ActivityTagContext(
    string TraceIdentifier,
    string RequestPath,
    int StatusCode,
    string ErrorId,
    string ErrorCode,
    string? ProblemType,
    string? ProblemTitle,
    string EnvironmentName);

public sealed record ActivityProblemDetailsContext(
    string ErrorId,
    string ErrorCode,
    string? ProblemType,
    string? ProblemTitle,
    int StatusCode,
    string RequestPath);

public sealed record ActivityExceptionEventContext(
    string ErrorId,
    string ErrorCode,
    string EnvironmentName,
    string Detail,
    bool IncludeStackTrace);
