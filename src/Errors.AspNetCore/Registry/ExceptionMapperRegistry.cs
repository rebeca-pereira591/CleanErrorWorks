using Errors.AspNetCore.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Errors.AspNetCore.Registry;

public sealed class ExceptionMapperRegistry : IExceptionMapperRegistry
{
    private readonly IReadOnlyList<MapperRegistration> _primaryMappers;
    private readonly IReadOnlyList<MapperRegistration> _fallbackMappers;

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
