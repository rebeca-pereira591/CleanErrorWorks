using Errors.AspNetCore.Mappers;
using System.Reflection;

namespace Errors.AspNetCore.Registry;

/// <summary>
/// Maintains prioritized lists of exception mappers and locates the first mapper able to handle an exception.
/// </summary>
public sealed class ExceptionMapperRegistry : IExceptionMapperRegistry
{
    private readonly IReadOnlyList<MapperRegistration> _primaryMappers;
    private readonly IReadOnlyList<MapperRegistration> _fallbackMappers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMapperRegistry"/> class.
    /// </summary>
    /// <param name="mappers">All mapper implementations registered in the container.</param>
    public ExceptionMapperRegistry(IEnumerable<IExceptionProblemDetailsMapper> mappers)
    {
        ArgumentNullException.ThrowIfNull(mappers);

        var registrations = mappers
            .Select(mapper => new MapperRegistration(mapper, ExtractMetadata(mapper)))
            .ToArray();

        _primaryMappers = registrations
            .Where(r => !r.Metadata.IsFallback)
            .OrderByDescending(r => r.Metadata.Priority)
            .ToArray();

        _fallbackMappers = registrations
            .Where(r => r.Metadata.IsFallback)
            .OrderByDescending(r => r.Metadata.Priority)
            .ToArray();
    }

    public IExceptionProblemDetailsMapper Resolve(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var mapper = TryResolve(exception)
            ?? throw new InvalidOperationException("No registered IExceptionProblemDetailsMapper could handle the exception.");

        return mapper;
    }

    /// <summary>
    /// Attempts to locate a mapper for the provided exception.
    /// </summary>
    /// <param name="exception">Exception to inspect.</param>
    /// <returns>The mapper that can handle the exception or <c>null</c>.</returns>
    private IExceptionProblemDetailsMapper? TryResolve(Exception exception)
    {
        foreach (var registration in _primaryMappers)
        {
            if (registration.Mapper.CanHandle(exception))
            {
                return registration.Mapper;
            }
        }

        foreach (var registration in _fallbackMappers)
        {
            if (registration.Mapper.CanHandle(exception))
            {
                return registration.Mapper;
            }
        }

        return null;
    }

    private static ExceptionMapperMetadata ExtractMetadata(IExceptionProblemDetailsMapper mapper)
    {
        var attribute = mapper.GetType().GetCustomAttribute<ExceptionMapperAttribute>(inherit: false);
        var priority = attribute?.Priority ?? ExceptionMapperAttribute.DefaultPriority;
        var isFallback = attribute?.IsFallback ?? false;
        return new ExceptionMapperMetadata(priority, isFallback);
    }

    private sealed record MapperRegistration(IExceptionProblemDetailsMapper Mapper, ExceptionMapperMetadata Metadata);

    private sealed record ExceptionMapperMetadata(int Priority, bool IsFallback);
}
