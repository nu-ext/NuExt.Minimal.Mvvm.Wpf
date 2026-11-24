using System;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Resolves a converter parameter that identifies a member of an enumeration.
/// </summary>
/// <remarks>
/// XAML passes <c>ConverterParameter</c> as a string, while a binding value is a boxed enumeration member.
/// This helper converts a member name (or a boxed member) to the enumeration type of the value and reports
/// configuration errors with a message that names the offending value and type.
/// </remarks>
internal static class EnumParameter
{
    /// <summary>
    /// Gets the enumeration type represented by the specified type.
    /// </summary>
    /// <param name="type">The type to inspect, possibly a nullable enumeration.</param>
    /// <returns>
    /// The enumeration type when <paramref name="type"/> is an enumeration or a nullable enumeration;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public static Type? TryGetEnumType(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsEnum ? underlyingType : null;
    }

    /// <summary>
    /// Converts the converter parameter to a member of the specified enumeration.
    /// </summary>
    /// <param name="parameter">The converter parameter: a member name or an already boxed enumeration member.</param>
    /// <param name="enumType">The enumeration type that owns the member.</param>
    /// <returns>The boxed enumeration member.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="parameter"/> is missing or does not identify a member of <paramref name="enumType"/>.
    /// </exception>
    public static object Parse(object? parameter, Type enumType)
    {
        if (parameter is null)
        {
            throw new InvalidOperationException(
                $"The converter parameter is required; it must identify a member of '{enumType}'.");
        }

        if (enumType.IsInstanceOfType(parameter))
        {
            return parameter;
        }

        if (parameter is string text && text.Length > 0)
        {
#if NET5_0_OR_GREATER
            if (Enum.TryParse(enumType, text, ignoreCase: false, out object? parsed))
            {
                return parsed;
            }
#else
            try
            {
                return Enum.Parse(enumType, text, ignoreCase: false);
            }
            catch (Exception ex) when (ex is ArgumentException or OverflowException)
            {
                // Not a member name and not a numeric value of the enumeration.
            }
#endif
        }

        throw new InvalidOperationException(
            $"The converter parameter '{parameter}' is not a member of '{enumType}'. " +
            "Use a member name or a static member reference.");
    }
}
