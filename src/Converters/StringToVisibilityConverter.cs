using System;
using System.Globalization;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Converts a string to <see cref="Visibility"/>: the result is <see cref="Visibility.Visible"/> when the string
/// contains non-whitespace characters.
/// </summary>
/// <remarks>
/// <para>
/// The converter is intended for placeholders, validation hints, and other elements that must disappear when there
/// is no text. <see langword="null"/>, an empty string, and a whitespace-only string are treated as empty, so a
/// placeholder is hidden until the source provides content.
/// </para>
/// <para>
/// Only strings count as text: numbers and other values are treated as empty, and a number is better mapped with
/// <see cref="NumericToVisibilityConverter"/>. An empty value produces <see cref="Visibility.Collapsed"/>
/// (or <see cref="Visibility.Hidden"/> when <see cref="HideInsteadOfCollapse"/> is set). Use <see cref="Invert"/>
/// to show an element only while there is no text.
/// </para>
/// </remarks>
public sealed class StringToVisibilityConverter : ValueConverterBase<object?, Visibility, object?>
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Visibility.Hidden"/> is returned instead of
    /// <see cref="Visibility.Collapsed"/> when there is no text.
    /// </summary>
    public bool HideInsteadOfCollapse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversion result is inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    protected override Visibility ConvertTo(object? value, object? parameter, CultureInfo culture)
    {
        bool hasText = value is string text && !string.IsNullOrWhiteSpace(text);

        if (Invert)
        {
            hasText = !hasText;
        }

        return hasText ? Visibility.Visible : HideInsteadOfCollapse ? Visibility.Hidden : Visibility.Collapsed;
    }
}
