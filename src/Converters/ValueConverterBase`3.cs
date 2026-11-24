using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// An abstract base class for implementing value converters in WPF, providing a generic way
/// to convert values between different types with optional parameters and culture information.
/// </summary>
/// <typeparam name="TFrom">The type of the input value.</typeparam>
/// <typeparam name="TTo">The type of the output value.</typeparam>
/// <typeparam name="TParameter">The type of the parameter used in conversion.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IValueConverter.Convert"/> validates the binding target type and converts both the value and the
/// parameter with the silent conversion mode of the cast helper (<c>throwCastException: false</c>). A value that
/// cannot be converted therefore reaches <see cref="ConvertTo"/> as <see langword="default"/>: for <see cref="bool"/>
/// that is <see langword="false"/>, for reference types it is <see langword="null"/>. Derived converters map such
/// input to a neutral result (for example <see langword="false"/> or <see cref="System.Windows.Visibility.Collapsed"/>) instead of
/// throwing, so a missing or temporarily unavailable source value never breaks the binding.
/// </para>
/// <para>
/// A parameter that cannot be converted is a different case: it is a configuration error. Converters that require a
/// parameter (a threshold, a comparison mode, an enumeration member) throw <see cref="InvalidOperationException"/>
/// with a message that names the missing or unsupported setting.
/// </para>
/// <para>
/// Both directions validate the binding type with the target type check of the cast helper, which requires the type
/// to be assignable from the non-nullable <typeparamref name="TTo"/> (in <c>Convert</c>) or
/// <typeparamref name="TFrom"/> (in <c>ConvertBack</c>). Consequently a nullable target such as <c>bool?</c>
/// (<c>ToggleButton.IsChecked</c>), a source property with a more specific type than <typeparamref name="TFrom"/>,
/// and returning <c>Binding.DoNothing</c> cannot be expressed through this base class; such converters implement
/// <see cref="IValueConverter"/> directly, like <see cref="EnumToBooleanConverter"/>.
/// </para>
/// </remarks>
public abstract class ValueConverterBase<TFrom, TTo, TParameter> : IValueConverter
{
    /// <summary>
    /// Converts a value from the source type to the target type.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Cast<TTo>.ValidateTargetType(targetType);
        var v = Cast<TFrom>.To(value, false);
        var p = Cast<TParameter>.To(parameter, false);
        return ConvertTo(v, p, culture);
    }

    /// <summary>
    /// Converts a value from the target type back to the source type.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Cast<TFrom>.ValidateTargetType(targetType);
        var v = Cast<TTo>.To(value, false);
        var p = Cast<TParameter>.To(parameter, false);
        return ConvertFrom(v, p, culture);
    }

    /// <summary>
    /// When overridden in a derived class, converts a value from the source type to the target type.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value.</returns>
    [return: MaybeNull]
    protected abstract TTo ConvertTo([AllowNull] TFrom value, [AllowNull] TParameter parameter, CultureInfo culture);

    /// <summary>
    /// Converts a value from the target type back to the source type.
    /// This implementation throws a <see cref="NotSupportedException"/> by default.
    /// Override this method in a derived class to provide custom backward conversion logic.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value.</returns>
    [return: MaybeNull]
    protected virtual TFrom ConvertFrom([AllowNull] TTo value, [AllowNull] TParameter parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"ConvertBack is not supported by {GetType().Name}. Override ConvertFrom to implement backward conversion.");
    }
}