using System;

namespace Errors.AspNetCore.Registry;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ExceptionMapperAttribute : Attribute
{
    public const int DefaultPriority = 0;

    public ExceptionMapperAttribute()
    {
        Priority = DefaultPriority;
    }

    public ExceptionMapperAttribute(int priority)
    {
        Priority = priority;
    }

    public int Priority { get; }

    public bool IsFallback { get; init; }
}
