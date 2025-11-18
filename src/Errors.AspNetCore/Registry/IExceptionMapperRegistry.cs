using Errors.AspNetCore.Mappers;
using System;

namespace Errors.AspNetCore.Registry;

public interface IExceptionMapperRegistry
{
    IExceptionProblemDetailsMapper Resolve(Exception exception);
}
