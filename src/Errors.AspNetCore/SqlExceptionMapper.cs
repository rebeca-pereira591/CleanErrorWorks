using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;

namespace Errors.AspNetCore;

public sealed class SqlExceptionMapper(SqlServerErrorClassifier classifier)
    : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is SqlException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var sql = (SqlException)ex;
        var (status, code, _) = classifier.Classify(sql.Number);

        var pd = new ProblemDetails
        {
            Type = code.TypeUri,
            Title = code.Title,
            Status = (int)status,
            Detail = "A database error occurred.",
            Instance = $"urn:problem:instance:{Guid.NewGuid()}"
        };

        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["sqlErrorNumber"] = sql.Number;
        pd.Extensions["code"] = code.Code;

        return (status, pd);
    }
}