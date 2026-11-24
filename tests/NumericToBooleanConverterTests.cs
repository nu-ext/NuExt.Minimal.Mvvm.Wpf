using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class NumericToBooleanConverterTests
{
    private static bool Convert(object? value, object? parameter = null,
        NumberComparison comparison = NumberComparison.NonZero, bool invert = false)
    {
        IValueConverter converter = new NumericToBooleanConverter
        {
            Comparison = comparison,
            Invert = invert
        };

        return (bool)converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture)!;
    }

    // ---------- NonZero (default) ----------

    [TestMethod]
    public void NonZero_ReturnsFalseForZero()
    {
        object?[] values = [0, 0L, (short)0, (byte)0, 0u, 0ul, 0.0, 0f, 0m, "0", "-0", false];

        foreach (var value in values)
        {
            Assert.IsFalse(Convert(value), $"Value '{value ?? "null"}' ({value?.GetType().Name ?? "null"}) must convert to false.");
        }
    }

    [TestMethod]
    public void NonZero_ReturnsTrueForNonZeroNumbers()
    {
        object?[] values =
        [
            1, -1, 1L, -1L, (short)1, (short)-1, (byte)1, 1u, 1ul,
            0.5, -0.5, 0.5f, -0.5f, 1m, -0.5m,
            int.MinValue, int.MaxValue, long.MinValue, long.MaxValue, ulong.MaxValue,
            1e-10, 1e10, "1", "-1", "0.5", true
        ];

        foreach (var value in values)
        {
            Assert.IsTrue(Convert(value), $"Value '{value}' ({value?.GetType().Name}) must convert to true.");
        }
    }

    [TestMethod]
    public void NonZero_IgnoresParameter()
    {
        Assert.IsTrue(Convert(1, "999"));
        Assert.IsFalse(Convert(0, "999"));
    }

    // ---------- Values that cannot be treated as a number ----------

    [TestMethod]
    public void NonNumericValues_ReturnFalse()
    {
        object?[] values =
        [
            null,
            DependencyProperty.UnsetValue,
            "abc",
            "",
            " ",
            "1e3",
            DateTime.UtcNow,
            Guid.Empty,
            'A',
            new object()
        ];

        foreach (var value in values)
        {
            Assert.IsFalse(Convert(value), $"Value '{value ?? "null"}' must convert to false.");
        }
    }

    [TestMethod]
    public void SpecialFloatingPointValues_ReturnFalse()
    {
        Assert.IsFalse(Convert(double.NaN));
        Assert.IsFalse(Convert(double.PositiveInfinity));
        Assert.IsFalse(Convert(double.NegativeInfinity));
        Assert.IsFalse(Convert(float.NaN));
    }

    [TestMethod]
    public void ThousandsSeparator_IsParsedAsNumber()
    {
        // Under the invariant culture a comma is a group separator, so "1,5" means 15, not 1.5.
        Assert.IsTrue(Convert("1,5"));
    }

    [TestMethod]
    public void Value_ParsesWithProvidedCulture()
    {
        IValueConverter converter = new NumericToBooleanConverter { Comparison = NumberComparison.Equal };

        // The value is parsed with the binding culture, the converter parameter always with the invariant culture.
        var result = (bool)converter.Convert("1,5", typeof(bool), "1.5", CultureInfo.GetCultureInfo("ru-RU"))!;

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValuesBelowDecimalPrecision_RoundToZero()
    {
        // decimal keeps at most 28-29 significant digits, so 1e-30 becomes 0 and is treated as zero.
        Assert.IsFalse(Convert(1e-30));
    }

    // ---------- Comparisons ----------

    [TestMethod]
    public void Comparisons_MatchExpectedResult()
    {
        (NumberComparison Comparison, object Value, object Parameter, bool Expected)[] cases =
        [
            (NumberComparison.GreaterThan, 6, 5, true),
            (NumberComparison.GreaterThan, 5, 5, false),
            (NumberComparison.GreaterThan, 4, 5, false),
            (NumberComparison.GreaterThanOrEqual, 5, 5, true),
            (NumberComparison.GreaterThanOrEqual, 4, 5, false),
            (NumberComparison.LessThan, 4, 5, true),
            (NumberComparison.LessThan, 5, 5, false),
            (NumberComparison.LessThanOrEqual, 5, 5, true),
            (NumberComparison.LessThanOrEqual, 6, 5, false),
            (NumberComparison.Equal, 5, 5, true),
            (NumberComparison.Equal, 5, "5.0", true),
            (NumberComparison.Equal, 5, 5.0001, false),
            (NumberComparison.NotEqual, 6, 5, true),
            (NumberComparison.NotEqual, 5, 5, false),
            (NumberComparison.GreaterThan, 1.5, "1.25", true),
            (NumberComparison.LessThan, -2, "-1", true),
            (NumberComparison.GreaterThan, "3.5", 3.4m, true),
            (NumberComparison.LessThan, long.MaxValue, ulong.MaxValue, true)
        ];

        foreach (var (comparison, value, parameter, expected) in cases)
        {
            Assert.AreEqual(expected, Convert(value, parameter, comparison),
                $"{value} {comparison} {parameter} must be {expected}.");
        }
    }

    [TestMethod]
    public void Comparison_WithoutNumericParameter_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(5, null, NumberComparison.GreaterThan); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(5, "abc", NumberComparison.Equal); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(5, "", NumberComparison.LessThanOrEqual); });
    }

    [TestMethod]
    public void Comparison_UsesDecimalPrecisionForLargeIntegers()
    {
        const long value = 9_007_199_254_740_993; // 2^53 + 1, not representable exactly as double

        Assert.IsTrue(Convert(value, "9007199254740992", NumberComparison.GreaterThan));
        Assert.IsFalse(Convert(value, "9007199254740992", NumberComparison.Equal));
        Assert.IsTrue(Convert(value, "9007199254740993", NumberComparison.Equal));
    }

    [TestMethod]
    public void Comparison_InvalidValueReturnsFalse()
    {
        Assert.IsFalse(Convert(null, 5, NumberComparison.LessThan));
        Assert.IsFalse(Convert("abc", 5, NumberComparison.Equal));
    }

    [TestMethod]
    public void UnknownComparison_ThrowsArgumentOutOfRange()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = Convert(5, 5, (NumberComparison)int.MaxValue); });
    }

    // ---------- Invert ----------

    [TestMethod]
    public void Invert_NegatesNonZeroResult()
    {
        Assert.IsTrue(Convert(0, comparison: NumberComparison.NonZero, invert: true));
        Assert.IsFalse(Convert(1, comparison: NumberComparison.NonZero, invert: true));
    }

    [TestMethod]
    public void Invert_NegatesComparisonResult()
    {
        Assert.IsFalse(Convert(6, 5, NumberComparison.GreaterThan, invert: true));
        Assert.IsTrue(Convert(5, 5, NumberComparison.GreaterThan, invert: true));
    }

    [TestMethod]
    public void Invert_DoesNotMakeInvalidValueTrue()
    {
        Assert.IsFalse(Convert("abc", comparison: NumberComparison.NonZero, invert: true));
        Assert.IsFalse(Convert(null, 5, NumberComparison.GreaterThan, invert: true));
    }

    // ---------- Converter contract ----------

    [TestMethod]
    public void ConvertBack_IsNotSupported()
    {
        IValueConverter converter = new NumericToBooleanConverter();

        Assert.ThrowsExactly<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonBooleanTargetType_Throws()
    {
        IValueConverter converter = new NumericToBooleanConverter();

        Assert.ThrowsExactly<InvalidCastException>(() =>
            converter.Convert(1, typeof(int), null, CultureInfo.InvariantCulture));
    }
}
