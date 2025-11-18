using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Errors.AspNetCore.Formatters;

public sealed class ProblemDetailsBuilder
{
    private readonly HttpContext _httpContext;
    private readonly ProblemDetails _details;

    private ProblemDetailsBuilder(HttpContext httpContext, ProblemDetails details)
    {
        _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _details = details ?? throw new ArgumentNullException(nameof(details));
    }

    public static ProblemDetailsBuilder Create(HttpContext httpContext)
        => new(httpContext, new ProblemDetails());

    public static ProblemDetailsBuilder FromProblemDetails(HttpContext httpContext, ProblemDetails problemDetails)
        => new(httpContext, problemDetails);

    public ProblemDetailsBuilder WithType(string? type)
    {
        if (!string.IsNullOrWhiteSpace(type))
            _details.Type = type;
        return this;
    }

    public ProblemDetailsBuilder WithTitle(string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
            _details.Title = title;
        return this;
    }

    public ProblemDetailsBuilder WithDetail(string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
            _details.Detail = detail;
        return this;
    }

    public ProblemDetailsBuilder WithInstance(string? instance = null)
    {
        _details.Instance = string.IsNullOrWhiteSpace(instance)
            ? $"urn:problem:instance:{Guid.NewGuid()}"
            : instance;
        return this;
    }

    public ProblemDetailsBuilder WithStatus(HttpStatusCode status)
    {
        _details.Status = (int)status;
        return this;
    }

    public ProblemDetailsBuilder WithCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            _details.Extensions["code"] = code;
        return this;
    }

    public ProblemDetailsBuilder WithTraceId(string? traceId = null)
    {
        _details.Extensions["traceId"] = string.IsNullOrWhiteSpace(traceId)
            ? _httpContext.TraceIdentifier
            : traceId;
        return this;
    }

    public ProblemDetailsBuilder WithExtension(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        _details.Extensions[key] = value;
        return this;
    }

    public ProblemDetails Build()
    {
        _details.Instance ??= $"urn:problem:instance:{Guid.NewGuid()}";

        if (!_details.Extensions.ContainsKey("traceId"))
        {
            _details.Extensions["traceId"] = _httpContext.TraceIdentifier;
        }

        return _details;
    }
}
