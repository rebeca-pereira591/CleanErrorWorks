using Microsoft.Extensions.Logging;

namespace Errors.Logging;

public static class LogEvents
{
    public static readonly EventId UnhandledException = new(1000, nameof(UnhandledException));
    public static readonly EventId NotFound = new(1404, nameof(NotFound));
    public static readonly EventId Unauthorized = new(1401, nameof(Unauthorized));
    public static readonly EventId ValidationFailed = new(1422, nameof(ValidationFailed));
}
