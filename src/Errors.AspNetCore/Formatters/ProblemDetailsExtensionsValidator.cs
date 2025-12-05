using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace Errors.AspNetCore.Formatters;

internal static class ProblemDetailsExtensionsValidator
{
    private static readonly Type[] AllowedPrimitiveTypes =
    {
        typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
        typeof(Guid), typeof(DateTime), typeof(DateTimeOffset)
    };

    /// <summary>
    /// Validates the extension entries to ensure they meet configured limits.
    /// </summary>
    /// <param name="problemDetails">Problem payload to sanitize.</param>
    /// <param name="options">Validation configuration.</param>
    public static void Validate(ProblemDetails problemDetails, ProblemDetailsExtensionValidationOptions options)
    {
        if (problemDetails is null) throw new ArgumentNullException(nameof(problemDetails));
        options ??= new ProblemDetailsExtensionValidationOptions();

        TrimExcessExtensions(problemDetails, options);
        SanitizeEntries(problemDetails, options);
    }

    private static void TrimExcessExtensions(ProblemDetails problemDetails, ProblemDetailsExtensionValidationOptions options)
    {
        if (problemDetails.Extensions.Count <= options.MaxExtensions)
        {
            return;
        }

        var keysToRemove = problemDetails.Extensions.Keys
            .Skip(options.MaxExtensions)
            .ToArray();

        foreach (var key in keysToRemove)
        {
            problemDetails.Extensions.Remove(key);
        }
    }

    private static void SanitizeEntries(ProblemDetails problemDetails, ProblemDetailsExtensionValidationOptions options)
    {
        var keys = problemDetails.Extensions.Keys.ToArray();
        foreach (var key in keys)
        {
            var value = problemDetails.Extensions[key];

            if (options.CustomValidator is not null && !options.CustomValidator(key, value))
            {
                problemDetails.Extensions.Remove(key);
                continue;
            }

            if (!IsSupportedValue(value, options.MaxDepth, options.MaxStringLength))
            {
                problemDetails.Extensions.Remove(key);
                continue;
            }

            if (value is string s && s.Length > options.MaxStringLength)
            {
                problemDetails.Extensions[key] = s[..options.MaxStringLength];
            }
        }
    }

    private static bool IsSupportedValue(object? value, int maxDepth, int maxStringLength, int currentDepth = 0)
    {
        if (value is null)
        {
            return true;
        }

        if (value is string s)
        {
            return s.Length <= maxStringLength;
        }

        var valueType = value.GetType();
        if (AllowedPrimitiveTypes.Contains(valueType))
        {
            return true;
        }

        if (currentDepth >= maxDepth)
        {
            return false;
        }

        if (value is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable.Cast<object?>())
            {
                if (!IsSupportedValue(item, maxDepth, maxStringLength, currentDepth + 1))
                {
                    return false;
                }
            }
            return true;
        }

        if (value is IDictionary<string, object?> dict)
        {
            foreach (var entry in dict)
            {
                if (!IsSupportedValue(entry.Value, maxDepth, maxStringLength, currentDepth + 1))
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }
}
