using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Errors.AspNetCore;

internal static class ValidationMappingExtensions
{
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