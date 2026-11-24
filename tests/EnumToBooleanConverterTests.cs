using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class EnumToBooleanConverterTests
{
    private enum Color
    {
        Red,
        Green,
        Blue
    }

    private static bool Convert(object? value, object? parameter = null, bool invert = false)
    {
        IValueConverter converter = new EnumToBooleanConverter { Invert = invert };

        return (bool)converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture)!;
    }

    private static object? ConvertBack(object? value, Type targetType, object? parameter = null, bool invert = false)
    {
        IValueConverter converter = new EnumToBooleanConverter { Invert = invert };

        return converter.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);
    }

    // ---------- Convert ----------

    [TestMethod]
    public void Convert_MatchesMemberByName()
    {
        Assert.IsTrue(Convert(Color.Red, "Red"));
        Assert.IsFalse(Convert(Color.Green, "Red"));
        Assert.IsFalse(Convert(Color.Blue, "Red"));
    }

    [TestMethod]
    public void Convert_AcceptsNumericParameter()
    {
        Assert.IsTrue(Convert(Color.Red, "0"));
        Assert.IsTrue(Convert(Color.Green, "1"));
        Assert.IsTrue(Convert(Color.Blue, "2"));
    }

    [TestMethod]
    public void Convert_AcceptsBoxedMemberParameter()
    {
        Assert.IsTrue(Convert(Color.Blue, Color.Blue));
        Assert.IsFalse(Convert(Color.Red, Color.Blue));
    }

    [TestMethod]
    public void Convert_InvalidValueIsFalse()
    {
        object?[] values = [null, DependencyProperty.UnsetValue, "Red", 1, true];

        foreach (var value in values)
        {
            Assert.IsFalse(Convert(value, "Red"), $"Value '{value ?? "null"}' must not be checked.");
        }
    }

    [TestMethod]
    public void Convert_MissingOrUnknownParameter_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red, ""); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red, "Purple"); });
    }

    [TestMethod]
    public void Convert_ParameterCaseMustMatch()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red, "red"); });
    }

    [TestMethod]
    public void Convert_InvertFlipsResult()
    {
        Assert.IsFalse(Convert(Color.Red, "Red", invert: true));
        Assert.IsTrue(Convert(Color.Red, "Green", invert: true));
    }

    [TestMethod]
    public void Convert_InvertDoesNotMakeInvalidValueChecked()
    {
        Assert.IsFalse(Convert(null, "Red", invert: true));
        Assert.IsFalse(Convert(DependencyProperty.UnsetValue, "Red", invert: true));
    }

    [TestMethod]
    public void Convert_NullableBooleanTargetIsSupported()
    {
        IValueConverter converter = new EnumToBooleanConverter();

        Assert.AreEqual(true, converter.Convert(Color.Red, typeof(bool?), "Red", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonBooleanTargetType_Throws()
    {
        IValueConverter converter = new EnumToBooleanConverter();

        Assert.ThrowsExactly<InvalidCastException>(() =>
            converter.Convert(Color.Red, typeof(Visibility), "Red", CultureInfo.InvariantCulture));
    }

    // ---------- ConvertBack ----------

    [TestMethod]
    public void ConvertBack_Checked_ReturnsMember()
    {
        Assert.AreEqual<object?>(Color.Green, ConvertBack(true, typeof(Color), "Green"));
        Assert.AreEqual<object?>(Color.Blue, ConvertBack(true, typeof(Color), "2"));
    }

    [TestMethod]
    public void ConvertBack_NullableEnumTarget_ReturnsMember()
    {
        Assert.AreEqual<object?>(Color.Blue, ConvertBack(true, typeof(Color?), "Blue"));
    }

    [TestMethod]
    public void ConvertBack_Unchecked_ReturnsDoNothing()
    {
        object?[] values = [false, null, DependencyProperty.UnsetValue];

        foreach (var value in values)
        {
            Assert.AreSame(Binding.DoNothing, ConvertBack(value, typeof(Color), "Green"),
                $"Value '{value ?? "null"}' must leave the source untouched.");
        }
    }

    [TestMethod]
    public void ConvertBack_NonEnumTarget_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = ConvertBack(true, typeof(int), "Green"); });
    }

    [TestMethod]
    public void ConvertBack_MissingParameter_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = ConvertBack(true, typeof(Color)); });
    }

    [TestMethod]
    public void ConvertBack_WithInvert_Throws()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertBack(true, typeof(Color), "Green", invert: true); });
    }
}
