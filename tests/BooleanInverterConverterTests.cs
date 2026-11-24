using System;
using System.Globalization;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class BooleanInverterConverterTests
{
    private static bool Convert(object? value, Type targetType)
    {
        IValueConverter converter = new BooleanInverterConverter();

        return (bool)converter.Convert(value, targetType, null, CultureInfo.InvariantCulture)!;
    }

    [TestMethod]
    public void Convert_InvertsValue()
    {
        Assert.IsFalse(Convert(true, typeof(bool)));
        Assert.IsTrue(Convert(false, typeof(bool)));
    }

    [TestMethod]
    public void Convert_WithNullableBooleanTargetType()
    {
        Assert.IsFalse(Convert(true, typeof(bool?)));
        Assert.IsTrue(Convert(false, typeof(bool?)));
    }

    [TestMethod]
    public void Convert_NullIsFalse()
    {
        Assert.IsTrue(Convert(null, typeof(bool?)));
    }

    [TestMethod]
    public void ConvertBack_InvertsValue()
    {
        IValueConverter converter = new BooleanInverterConverter();

        Assert.AreEqual(true, converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture));
        Assert.AreEqual(false, converter.ConvertBack(true, typeof(bool?), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonBooleanTargetType_Throws()
    {
        Assert.ThrowsExactly<InvalidCastException>(() => Convert(true, typeof(int)));
    }
}
