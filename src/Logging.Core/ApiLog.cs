using Microsoft.Extensions.Logging;

namespace Errors.Logging;

/// <summary>
/// Provides strongly-typed logging helpers for HTTP pipelines.
/// </summary>
public static partial class ApiLog
{
    /// <summary>
    /// Logs an unhandled exception captured by the middleware pipeline.
    /// </summary>
    /// <param name="logger">Typed logger wrapper.</param>
    /// <param name="path">Request path.</param>
    /// <param name="ex">Captured exception.</param>
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception at {Path}")]
    public static partial void Unhandled(HttpLogger logger, string path, Exception ex);
}

/// <summary>
/// Thin wrapper around <see cref="ILogger"/> to align with source generator requirements.
/// </summary>
/// <remarks>
/// <para>The wrapper exists because the source generator expects a type with <see cref="ILogger"/> semantics.</para>
/// </remarks>
public sealed class HttpLogger(ILogger logger) : ILogger
{
    private readonly ILogger _inner = logger;

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state)!;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);
}
