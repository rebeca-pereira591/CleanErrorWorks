using Errors.Abstractions;
using System.Net;

namespace Errors.AspNetCore.Classifiers;

/// <summary>
/// Provides mappings between SQL Server error numbers and HTTP/ProblemDetails metadata.
/// </summary>
public sealed class SqlServerErrorClassifier
{
    /// <summary>
    /// Maps a SQL Server error number to HTTP semantics and a structured <see cref="ErrorCode"/>.
    /// </summary>
    /// <param name="number">SQL Server error number.</param>
    /// <returns>A tuple containing HTTP status, error code, and a transient flag.</returns>
    public (HttpStatusCode status, ErrorCode code, bool transient) Classify(int number)
        => number switch
        {
            1205 => (HttpStatusCode.Conflict, new("INFRA-SQL-DEADLOCK", "SQL Deadlock", "/errors/infra/sql/deadlock"), true),
            -2 => (HttpStatusCode.GatewayTimeout, new("INFRA-SQL-TIMEOUT", "SQL Timeout", "/errors/infra/sql/timeout"), true),
            2627 => (HttpStatusCode.Conflict, new("INFRA-SQL-DUPKEY", "Duplicate key", "/errors/infra/sql/duplicate"), false),
            2601 => (HttpStatusCode.Conflict, new("INFRA-SQL-DUPKEY", "Duplicate key", "/errors/infra/sql/duplicate"), false),
            547 => (HttpStatusCode.UnprocessableContent, new("INFRA-SQL-CONSTRAINT", "Constraint violation", "/errors/infra/sql/constraint"), false),
            18456 => (HttpStatusCode.ServiceUnavailable, new("INFRA-SQL-AUTH", "SQL login failed", "/errors/infra/sql/auth"), true),
            4060 => (HttpStatusCode.ServiceUnavailable, new("INFRA-SQL-DBUNAVAILABLE", "Cannot open database", "/errors/infra/sql/unavailable"), true),
            _ => (HttpStatusCode.InternalServerError, new("INFRA-SQL-UNKNOWN", "SQL Error", "/errors/infra/sql/unknown"), false),
        };
}
