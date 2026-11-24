using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Converts an enumeration value to <see cref="bool"/> and back, so a group of checkable controls
/// (for example <c>RadioButton</c> or <c>MenuItem</c>) can share a single enumeration property.
/// </summary>
/// <remarks>
/// <para>
/// The converter parameter identifies the member that maps to <see langword="true"/>: pass its name
/// (<c>ConverterParameter=Kyorugi</c>) or the member itself (<c>ConverterParameter={x:Static ...}</c>).
/// The value and the parameter are compared as enumeration members, so case must match and the parameter
/// must belong to the enumeration type of the value.
/// </para>
/// <para>
/// <see cref="IValueConverter.Convert"/> returns <see langword="false"/> when the value is not an enumeration
/// (<see langword="null"/>, <see cref="DependencyProperty.UnsetValue"/>, a string, and so on), which is what a group
/// of radio buttons needs while the source property is still unset. <see cref="Invert"/> is not applied to such values,
/// so an invalid value is never reported as checked. A missing or unknown parameter is a configuration error and
/// throws <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// <see cref="IValueConverter.ConvertBack"/> returns the member when the control reports <see langword="true"/>
/// and <see cref="Binding.DoNothing"/> when it reports <see langword="false"/> or <see langword="null"/>.
/// WPF unchecks the previously selected button, and that update must not overwrite the source property, otherwise
/// the selection would be cleared. Back conversion parses the parameter against the binding target type
/// (the enumeration property, nullable or not), so it does not depend on the current value.
/// </para>
/// <para>
/// This converter implements <see cref="IValueConverter"/> directly instead of deriving from
/// <see cref="ValueConverterBase{TFrom, TTo, TParameter}"/>: the base class validates the target type against a fixed
/// generic type, which cannot match an arbitrary enumeration property, and it cannot return
/// <see cref="Binding.DoNothing"/>.
/// </para>
/// </remarks>
public sealed class EnumToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets a value indicating whether the comparison result is inverted.
    /// </summary>
    /// <remarks>
    /// Inverted conversions are one-way only: a checked control cannot express "any member except this one",
    /// so <see cref="IValueConverter.ConvertBack"/> throws <see cref="NotSupportedException"/> when this option is enabled.
    /// </remarks>
    public bool Invert { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidCastException">
    /// <paramref name="targetType"/> is neither <see cref="bool"/> nor <see cref="Nullable{T}"/> of <see cref="bool"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The value is an enumeration member and the converter parameter is missing or does not identify a member
    /// of that enumeration.
    /// </exception>
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool) && targetType != typeof(bool?))
        {
            throw new InvalidCastException($"Type 'bool' cannot be assigned to target type '{targetType}'.");
        }

        if (value is not Enum enumValue)
        {
            return false;
        }

        object expected = EnumParameter.Parse(parameter, enumValue.GetType());
        bool result = enumValue.Equals(expected);
        return Invert ? !result : result;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <paramref name="targetType"/> is not an enumeration or a nullable enumeration, or the converter parameter
    /// is missing or does not identify a member of that enumeration.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <see cref="Invert"/> is enabled, because an inverted comparison has no reversible source value.
    /// </exception>
    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Type enumType = EnumParameter.TryGetEnumType(targetType)
            ?? throw new InvalidOperationException(
                $"ConvertBack requires an enumeration source property, but the target type is '{targetType}'.");

        if (Invert)
        {
            throw new NotSupportedException(
                "ConvertBack is not supported when Invert is enabled, because a checked control cannot represent a negative comparison.");
        }

        if (value is not true)
        {
            return Binding.DoNothing;
        }

        return EnumParameter.Parse(parameter, enumType);
    }
}
