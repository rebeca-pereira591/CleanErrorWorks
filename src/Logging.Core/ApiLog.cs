using Microsoft.Extensions.Logging;

namespace Logging.Core;

public static partial class ApiLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception at {Path}")]
    public static partial void Unhandled(HttpLogger logger, string path, Exception ex);
}


public sealed class HttpLogger(ILogger logger) : ILogger
{
    private readonly ILogger _inner = logger;
    public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state)!;
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    => _inner.Log(logLevel, eventId, state, exception, formatter);
}