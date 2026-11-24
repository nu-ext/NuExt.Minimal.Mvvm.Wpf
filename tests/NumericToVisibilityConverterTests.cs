using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class NumericToVisibilityConverterTests
{
    private static Visibility Convert(object? value, object? parameter = null,
        NumberComparison comparison = NumberComparison.NonZero, bool invert = false, bool hideInsteadOfCollapse = false)
    {
        IValueConverter converter = new NumericToVisibilityConverter
        {
            Comparison = comparison,
            Invert = invert,
            HideInsteadOfCollapse = hideInsteadOfCollapse
        };

        return (Visibility)converter.Convert(value, typeof(Visibility), parameter, CultureInfo.InvariantCulture)!;
    }

    // ---------- NonZero (default) ----------

    [TestMethod]
    public void NonZero_ZeroIsCollapsed()
    {
        object?[] values = [0, 0L, 0.0, 0m, "0", false];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Collapsed, Convert(value), $"Value '{value}' must collapse.");
        }
    }

    [TestMethod]
    public void NonZero_NonZeroIsVisible()
    {
        object?[] values = [1, -1, 1L, 0.5, 1m, (byte)1, long.MaxValue, "1", true];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Visible, Convert(value), $"Value '{value}' must be visible.");
        }
    }

    // ---------- Comparisons ----------

    [TestMethod]
    public void Comparison_MapsToVisibility()
    {
        (NumberComparison Comparison, object Value, object Parameter, Visibility Expected)[] cases =
        [
            (NumberComparison.GreaterThan, 6, 5, Visibility.Visible),
            (NumberComparison.GreaterThan, 5, 5, Visibility.Collapsed),
            (NumberComparison.GreaterThanOrEqual, 5, 5, Visibility.Visible),
            (NumberComparison.LessThan, 4, 5, Visibility.Visible),
            (NumberComparison.LessThan, 5, 5, Visibility.Collapsed),
            (NumberComparison.LessThanOrEqual, 5, 5, Visibility.Visible),
            (NumberComparison.Equal, 5, "5.0", Visibility.Visible),
            (NumberComparison.NotEqual, 6, 5, Visibility.Visible),
            (NumberComparison.NotEqual, 5, 5, Visibility.Collapsed)
        ];

        foreach (var (comparison, value, parameter, expected) in cases)
        {
            Assert.AreEqual(expected, Convert(value, parameter, comparison),
                $"{value} {comparison} {parameter} must be {expected}.");
        }
    }

    [TestMethod]
    public void MissingOrInvalidParameter_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(5, null, NumberComparison.GreaterThan); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(5, "abc", NumberComparison.Equal); });
    }

    [TestMethod]
    public void UnknownComparison_ThrowsArgumentOutOfRange()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = Convert(5, 5, (NumberComparison)int.MaxValue); });
    }

    // ---------- HideInsteadOfCollapse ----------

    [TestMethod]
    public void FailedComparison_ReturnsHiddenInsteadOfCollapsed()
    {
        Assert.AreEqual(Visibility.Hidden, Convert(0, hideInsteadOfCollapse: true));
        Assert.AreEqual(Visibility.Hidden, Convert(5, 5, NumberComparison.GreaterThan, hideInsteadOfCollapse: true));
        Assert.AreEqual(Visibility.Visible, Convert(6, 5, NumberComparison.GreaterThan, hideInsteadOfCollapse: true));
    }

    // ---------- Invalid values ----------

    [TestMethod]
    public void InvalidValues_AreCollapsed()
    {
        object?[] values = [null, DependencyProperty.UnsetValue, "abc", "", DateTime.UtcNow, double.NaN];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Collapsed, Convert(value), $"Value '{value ?? "null"}' must collapse.");
        }
    }

    [TestMethod]
    public void InvalidValue_RespectsHideInsteadOfCollapse()
    {
        Assert.AreEqual(Visibility.Hidden, Convert("abc", hideInsteadOfCollapse: true));
    }

    // ---------- Invert ----------

    [TestMethod]
    public void Invert_FlipsResult()
    {
        Assert.AreEqual(Visibility.Visible, Convert(0, invert: true));
        Assert.AreEqual(Visibility.Collapsed, Convert(1, invert: true));
        Assert.AreEqual(Visibility.Visible, Convert(5, 5, NumberComparison.GreaterThan, invert: true));
        Assert.AreEqual(Visibility.Collapsed, Convert(6, 5, NumberComparison.GreaterThan, invert: true));
    }

    [TestMethod]
    public void Invert_DoesNotMakeInvalidValueVisible()
    {
        Assert.AreEqual(Visibility.Collapsed, Convert("abc", invert: true));
        Assert.AreEqual(Visibility.Collapsed, Convert(null, 5, NumberComparison.GreaterThan, invert: true));
    }

    // ---------- Converter contract ----------

    [TestMethod]
    public void ConvertBack_IsNotSupported()
    {
        IValueConverter converter = new NumericToVisibilityConverter();

        Assert.ThrowsExactly<NotSupportedException>(() =>
            converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonVisibilityTargetType_Throws()
    {
        IValueConverter converter = new NumericToVisibilityConverter();

        Assert.ThrowsExactly<InvalidCastException>(() =>
            converter.Convert(1, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
