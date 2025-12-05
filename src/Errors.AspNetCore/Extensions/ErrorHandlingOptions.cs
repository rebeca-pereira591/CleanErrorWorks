using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Mappers;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using System.Reflection;

namespace Errors.AspNetCore.Extensions;

/// <summary>
/// Provides hooks for configuring sanitization and ProblemDetails validation.
/// </summary>
public sealed class ErrorHandlingOptions
{
    private readonly List<Type> _mapperTypes = [];

    public Action<ExceptionSanitizerOptions>? ConfigureExceptionSanitizer { get; set; }
        = null;

    public Action<ProblemDetailsExtensionValidationOptions>? ConfigureProblemDetailsExtensions { get; set; }
        = null;

    internal IReadOnlyCollection<Type> MapperTypes => _mapperTypes;

    public void RegisterMapper<TMapper>() where TMapper : class, IExceptionProblemDetailsMapper
    {
        var mapperType = typeof(TMapper);
        var mapperAttribute = mapperType.GetCustomAttribute<ExceptionMapperAttribute>();

        if (mapperAttribute is null)
            throw new InvalidOperationException($"{mapperType.Name} must be decorated with {nameof(ExceptionMapperAttribute)} to convey priority and fallback behavior.");

        _mapperTypes.Add(mapperType);
    }
}
