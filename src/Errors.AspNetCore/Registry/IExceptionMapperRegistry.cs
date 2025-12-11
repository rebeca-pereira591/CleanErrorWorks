using Errors.AspNetCore.Mappers;

namespace Errors.AspNetCore.Registry;

/// <summary>
/// Provides prioritized lookup of registered <see cref="IExceptionProblemDetailsMapper"/> implementations.
/// </summary>
public interface IExceptionMapperRegistry
{
    /// <summary>
    /// Resolves the mapper capable of handling the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to classify.</param>
    /// <returns>The winning mapper.</returns>
    IExceptionProblemDetailsMapper Resolve(Exception exception);
}
