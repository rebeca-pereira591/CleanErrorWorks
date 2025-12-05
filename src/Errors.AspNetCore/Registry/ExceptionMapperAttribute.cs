namespace Errors.AspNetCore.Registry;

/// <summary>
/// Supplies metadata that controls mapper ordering and fallback behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ExceptionMapperAttribute : Attribute
{
    public const int DefaultPriority = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMapperAttribute"/> class using the default priority.
    /// </summary>
    public ExceptionMapperAttribute()
    {
        Priority = DefaultPriority;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMapperAttribute"/> class.
    /// </summary>
    /// <param name="priority">Priority used when ordering mappers (higher runs sooner).</param>
    public ExceptionMapperAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// Gets the ordering priority used when selecting mappers.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the mapper should only run as a fallback.
    /// </summary>
    public bool IsFallback { get; init; }
}
