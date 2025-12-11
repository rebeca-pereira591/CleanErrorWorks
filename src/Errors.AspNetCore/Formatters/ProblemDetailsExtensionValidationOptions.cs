namespace Errors.AspNetCore.Formatters;

/// <summary>
/// Controls how extensions on <see cref="ProblemDetails"/> are validated and truncated.
/// </summary>
public sealed class ProblemDetailsExtensionValidationOptions
{
    public int MaxExtensions { get; set; } = 20;

    public int MaxStringLength { get; set; } = 2048;

    public int MaxDepth { get; set; } = 2;

    public Func<string, object?, bool>? CustomValidator { get; set; }
        = null;
}
