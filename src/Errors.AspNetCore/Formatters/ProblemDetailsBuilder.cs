using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore.Formatters;

/// <summary>
/// Fluent helper for constructing <see cref="ProblemDetails"/> instances with consistent defaults.
/// </summary>
public sealed class ProblemDetailsBuilder
{
    private readonly HttpContext _httpContext;
    private readonly ProblemDetails _details;

    private ProblemDetailsBuilder(HttpContext httpContext, ProblemDetails details)
    {
        _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _details = details ?? throw new ArgumentNullException(nameof(details));
    }

    /// <summary>
    /// Creates a new builder with a fresh <see cref="ProblemDetails"/> instance.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <returns>A new builder.</returns>
    public static ProblemDetailsBuilder Create(HttpContext httpContext)
        => new(httpContext, new ProblemDetails());

    /// <summary>
    /// Creates a builder that wraps an existing <see cref="ProblemDetails"/> instance.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="problemDetails">Pre-existing problem payload.</param>
    /// <returns>A builder preloaded with the supplied details.</returns>
    public static ProblemDetailsBuilder FromProblemDetails(HttpContext httpContext, ProblemDetails problemDetails)
        => new(httpContext, problemDetails);

    /// <summary>
    /// Sets the <c>type</c> URI when provided.
    /// </summary>
    /// <param name="type">Problem type.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithType(string? type)
    {
        if (!string.IsNullOrWhiteSpace(type))
            _details.Type = type;
        return this;
    }

    /// <summary>
    /// Sets the <c>title</c> when provided.
    /// </summary>
    /// <param name="title">Problem title.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithTitle(string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
            _details.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the <c>detail</c> when provided.
    /// </summary>
    /// <param name="detail">Detailed explanation.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithDetail(string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
            _details.Detail = detail;
        return this;
    }

    /// <summary>
    /// Sets or creates the <c>instance</c> URI.
    /// </summary>
    /// <param name="instance">Optional instance identifier.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithInstance(string? instance = null)
    {
        _details.Instance = string.IsNullOrWhiteSpace(instance)
            ? $"urn:problem:instance:{Guid.NewGuid()}"
            : instance;
        return this;
    }

    /// <summary>
    /// Assigns the HTTP status code.
    /// </summary>
    /// <param name="status">HTTP status.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithStatus(HttpStatusCode status)
    {
        _details.Status = (int)status;
        return this;
    }

    /// <summary>
    /// Stores the domain-specific error code in extensions.
    /// </summary>
    /// <param name="code">Error code string.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            _details.Extensions["code"] = code;
        return this;
    }

    /// <summary>
    /// Adds the trace identifier extension, defaulting to the current request trace id.
    /// </summary>
    /// <param name="traceId">Optional custom trace id.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithTraceId(string? traceId = null)
    {
        _details.Extensions["traceId"] = string.IsNullOrWhiteSpace(traceId)
            ? _httpContext.TraceIdentifier
            : traceId;
        return this;
    }

    /// <summary>
    /// Adds a custom extension entry.
    /// </summary>
    /// <param name="key">Extension key.</param>
    /// <param name="value">Extension value.</param>
    /// <returns>The current builder.</returns>
    public ProblemDetailsBuilder WithExtension(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        _details.Extensions[key] = value;
        return this;
    }

    /// <summary>
    /// Finalizes and returns the configured <see cref="ProblemDetails"/>.
    /// </summary>
    /// <returns>The built problem payload.</returns>
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
