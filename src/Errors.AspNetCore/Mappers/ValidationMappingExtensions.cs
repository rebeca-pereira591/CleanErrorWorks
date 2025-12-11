using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Errors.AspNetCore.Mappers;

internal static class ValidationMappingExtensions
{
    /// <summary>
    /// Converts validation errors into <see cref="ModelStateDictionary"/> entries.
    /// </summary>
    /// <param name="errors">Dictionary keyed by field name.</param>
    /// <returns>A populated <see cref="ModelStateDictionary"/>.</returns>
    public static ModelStateDictionary ToModelState(
        this IReadOnlyDictionary<string, string[]> errors)
    {
        var ms = new ModelStateDictionary();
        foreach (var (key, msgs) in errors)
            if (msgs != null)
                foreach (var m in msgs)
                    ms.AddModelError(key ?? string.Empty, m ?? string.Empty);
        return ms;
    }
}
