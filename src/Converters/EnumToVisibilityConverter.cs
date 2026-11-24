using System;
using System.Globalization;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Converts an enumeration value to <see cref="Visibility"/> by comparing it with the member passed
/// in the converter parameter.
/// </summary>
/// <remarks>
/// <para>
/// The converter parameter identifies the member that maps to <see cref="Visibility.Visible"/>: pass its name
/// (<c>ConverterParameter=Kyorugi</c>) or the member itself (<c>ConverterParameter={x:Static ...}</c>).
/// The value and the parameter are compared as enumeration members, so case must match and the parameter
/// must belong to the enumeration type of the value.
/// </para>
/// <para>
/// A value that is not an enumeration (<see langword="null"/>, <see cref="System.Windows.DependencyProperty.UnsetValue"/>,
/// a string, and so on) produces <see cref="Visibility.Collapsed"/> (or <see cref="Visibility.Hidden"/> when
/// <see cref="HideInsteadOfCollapse"/> is set). <see cref="Invert"/> is not applied to such values, so an invalid value
/// is never shown. A missing or unknown parameter is a configuration error and throws
/// <see cref="InvalidOperationException"/>.
/// </para>
/// </remarks>
public sealed class EnumToVisibilityConverter : ValueConverterBase<Enum?, Visibility, object?>
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Visibility.Hidden"/> is returned instead of
    /// <see cref="Visibility.Collapsed"/> when the comparison fails.
    /// </summary>
    public bool HideInsteadOfCollapse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the comparison result is inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The converter parameter is missing or does not identify a member of the enumeration type of the value.
    /// </exception>
    protected override Visibility ConvertTo(Enum? value, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return HideInsteadOfCollapse ? Visibility.Hidden : Visibility.Collapsed;
        }

        object expected = EnumParameter.Parse(parameter, value.GetType());
        bool result = value.Equals(expected);

        if (Invert)
        {
            result = !result;
        }

        return result ? Visibility.Visible : HideInsteadOfCollapse ? Visibility.Hidden : Visibility.Collapsed;
    }
}
