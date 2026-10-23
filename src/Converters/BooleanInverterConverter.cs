using System.Globalization;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// A value converter that inverts boolean values using logical NOT operation.
/// </summary>
public sealed class BooleanInverterConverter : ValueConverterBase<bool, bool, object?>
{
    protected override bool ConvertTo(bool value, object? parameter, CultureInfo culture)
    {
        return !value;
    }

    protected override bool ConvertFrom(bool value, object? parameter, CultureInfo culture)
    {
        return !value;
    }
}