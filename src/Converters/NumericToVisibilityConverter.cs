using System;
using System.Globalization;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Converts a numeric value to <see cref="Visibility"/> by comparing it with the threshold passed in the converter parameter.
/// </summary>
/// <remarks>
/// The value and the threshold are converted to <see cref="decimal"/>, so identifiers and money-like values are compared
/// without the precision loss of <see cref="double"/>. Values that cannot be converted (<see langword="null"/>,
/// <see cref="DependencyProperty.UnsetValue"/>, non-numeric strings, <see cref="double.NaN"/>) are treated as a failed
/// comparison and produce <see cref="Visibility.Collapsed"/> (or <see cref="Visibility.Hidden"/> when
/// <see cref="HideInsteadOfCollapse"/> is set).
/// </remarks>
public sealed class NumericToVisibilityConverter : ValueConverterBase<object?, Visibility, decimal?>
{
    /// <summary>
    /// Gets or sets the comparison applied to the value. The default is <see cref="NumberComparison.NonZero"/>.
    /// </summary>
    public NumberComparison Comparison { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Visibility.Hidden"/> is returned instead of
    /// <see cref="Visibility.Collapsed"/> when the comparison fails.
    /// </summary>
    public bool HideInsteadOfCollapse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversion result is inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The comparison requires a threshold and the converter parameter is missing or is not a number.
    /// </exception>
    protected override Visibility ConvertTo(object? value, decimal? parameter, CultureInfo culture)
    {
        if (!Cast<decimal>.TryTo(value, culture, out decimal number))
        {
            return HideInsteadOfCollapse ? Visibility.Hidden : Visibility.Collapsed;
        }

        bool result = Comparison.Evaluate(number, parameter);

        if (Invert)
        {
            result = !result;
        }

        return result ? Visibility.Visible : HideInsteadOfCollapse ? Visibility.Hidden : Visibility.Collapsed;
    }
}
