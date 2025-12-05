using Errors.AspNetCore.Classifiers;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;

namespace Errors.AspNetCore.Mappers;

/// <summary>
/// Maps <see cref="SqlException"/> instances using <see cref="SqlServerErrorClassifier"/>.
/// </summary>
[ExceptionMapper(priority: 450)]
public sealed class SqlExceptionMapper(SqlServerErrorClassifier classifier, IExceptionSanitizer sanitizer)
    : IExceptionProblemDetailsMapper
{
    public bool CanHandle(Exception ex) => ex is SqlException;

    public (HttpStatusCode, ProblemDetails) Map(HttpContext ctx, Exception ex)
    {
        var sql = (SqlException)ex;
        var (status, code, _) = classifier.Classify(sql.Number);

        var sanitized = sanitizer.Sanitize(ctx, ex, "A database error occurred.", treatPreferredDetailAsSensitive: false);

        var problem = ProblemDetailsBuilder.Create(ctx)
            .WithType(code.TypeUri)
            .WithTitle(code.Title)
            .WithDetail(sanitized.Detail)
            .WithStatus(status)
            .WithInstance()
            .WithCode(code.Code)
            .WithTraceId()
            .WithExtension("sqlErrorNumber", sql.Number)
            .Build();

        return (status, problem);
    }
}
