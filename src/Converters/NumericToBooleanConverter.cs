using System;
using System.Globalization;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Converts a numeric value to <see cref="bool"/> by comparing it with the threshold passed in the converter parameter.
/// </summary>
/// <remarks>
/// The value and the threshold are converted to <see cref="decimal"/>, so identifiers and money-like values are compared
/// without the precision loss of <see cref="double"/>. Values that cannot be converted (<see langword="null"/>,
/// <see cref="System.Windows.DependencyProperty.UnsetValue"/>, non-numeric strings, <see cref="System.Double.NaN"/>)
/// produce <see langword="false"/>.
/// </remarks>
public sealed class NumericToBooleanConverter : ValueConverterBase<object?, bool, decimal?>
{
    /// <summary>
    /// Gets or sets the comparison applied to the value. The default is <see cref="NumberComparison.NonZero"/>.
    /// </summary>
    public NumberComparison Comparison { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversion result is inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The comparison requires a threshold and the converter parameter is missing or is not a number.
    /// </exception>
    protected override bool ConvertTo(object? value, decimal? parameter, CultureInfo culture)
    {
        if (!Cast<decimal>.TryTo(value, culture, out decimal number))
        {
            return false;
        }

        bool result;
        if (Comparison == NumberComparison.NonZero)
        {
            result = number != 0m;
        }
        else
        {
            if (parameter is not { } threshold)
            {
                throw new InvalidOperationException(
                    $"{nameof(NumericToBooleanConverter)} requires a numeric converter parameter for the '{Comparison}' comparison.");
            }

            result = Comparison switch
            {
                NumberComparison.Equal => number == threshold,
                NumberComparison.NotEqual => number != threshold,
                NumberComparison.GreaterThan => number > threshold,
                NumberComparison.GreaterThanOrEqual => number >= threshold,
                NumberComparison.LessThan => number < threshold,
                NumberComparison.LessThanOrEqual => number <= threshold,
                _ => throw new ArgumentOutOfRangeException(nameof(Comparison))
            };
        }

        return Invert ? !result : result;
    }
}
