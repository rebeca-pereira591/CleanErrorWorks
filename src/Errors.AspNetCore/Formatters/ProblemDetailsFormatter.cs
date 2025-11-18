using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Errors.AspNetCore.Formatters;

public sealed class ProblemDetailsFormatter(IOptions<ProblemDetailsExtensionValidationOptions> validationOptions) : IProblemDetailsFormatter
{
    private readonly ProblemDetailsExtensionValidationOptions _validationOptions = validationOptions.Value ?? new ProblemDetailsExtensionValidationOptions();

    public ProblemDetailsFormattingResult Format(HttpContext httpContext, ProblemDetails problemDetails, HttpStatusCode statusCode)
    {
        var errorId = $"err-{Guid.NewGuid():N}";
        problemDetails.Extensions["errorId"] = errorId;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Instance ??= $"urn:problem:instance:{errorId}";

        httpContext.Response.Headers["x-error-id"] = errorId;
        httpContext.Response.Headers["x-trace-id"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        ProblemDetailsExtensionsValidator.Validate(problemDetails, _validationOptions);

        var errorCode = ExtractErrorCode(problemDetails.Extensions);
        return new ProblemDetailsFormattingResult(errorId, errorCode);
    }

    public ValueTask WriteAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken)
        => new(httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken));

    private static string ExtractErrorCode(IDictionary<string, object?> extensions)
    {
        if (extensions.TryGetValue("code", out var codeObj) && codeObj is string code && !string.IsNullOrWhiteSpace(code))
            return code;

        return "UNKNOWN_ERROR";
    }
}
