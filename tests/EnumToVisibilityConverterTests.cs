using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class EnumToVisibilityConverterTests
{
    private enum Color
    {
        Red,
        Green,
        Blue
    }

    private static Visibility Convert(object? value, object? parameter = null, bool invert = false,
        bool hideInsteadOfCollapse = false)
    {
        IValueConverter converter = new EnumToVisibilityConverter
        {
            Invert = invert,
            HideInsteadOfCollapse = hideInsteadOfCollapse
        };

        return (Visibility)converter.Convert(value, typeof(Visibility), parameter, CultureInfo.InvariantCulture)!;
    }

    [TestMethod]
    public void MatchingMember_IsVisible()
    {
        Assert.AreEqual(Visibility.Visible, Convert(Color.Red, "Red"));
        Assert.AreEqual(Visibility.Visible, Convert(Color.Green, "1"));
        Assert.AreEqual(Visibility.Visible, Convert(Color.Blue, Color.Blue));
    }

    [TestMethod]
    public void OtherMember_IsCollapsed()
    {
        Assert.AreEqual(Visibility.Collapsed, Convert(Color.Green, "Red"));
        Assert.AreEqual(Visibility.Collapsed, Convert(Color.Red, Color.Blue));
    }

    [TestMethod]
    public void FailedComparison_ReturnsHiddenInsteadOfCollapsed()
    {
        Assert.AreEqual(Visibility.Hidden, Convert(Color.Green, "Red", hideInsteadOfCollapse: true));
        Assert.AreEqual(Visibility.Visible, Convert(Color.Red, "Red", hideInsteadOfCollapse: true));
    }

    [TestMethod]
    public void InvalidValues_AreCollapsed()
    {
        object?[] values = [null, DependencyProperty.UnsetValue, "Red", 1, true];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Collapsed, Convert(value, "Red"), $"Value '{value ?? "null"}' must collapse.");
        }
    }

    [TestMethod]
    public void InvalidValue_RespectsHideInsteadOfCollapse()
    {
        Assert.AreEqual(Visibility.Hidden, Convert("Red", "Red", hideInsteadOfCollapse: true));
    }

    [TestMethod]
    public void MissingOrUnknownParameter_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red, "Purple"); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Convert(Color.Red, "red"); });
    }

    [TestMethod]
    public void Invert_FlipsResult()
    {
        Assert.AreEqual(Visibility.Collapsed, Convert(Color.Red, "Red", invert: true));
        Assert.AreEqual(Visibility.Visible, Convert(Color.Red, "Green", invert: true));
    }

    [TestMethod]
    public void Invert_DoesNotMakeInvalidValueVisible()
    {
        Assert.AreEqual(Visibility.Collapsed, Convert(null, "Red", invert: true));
        Assert.AreEqual(Visibility.Collapsed, Convert("Red", "Red", invert: true));
    }

    // ---------- Converter contract ----------

    [TestMethod]
    public void ConvertBack_IsNotSupported()
    {
        IValueConverter converter = new EnumToVisibilityConverter();

        Assert.ThrowsExactly<NotSupportedException>(() =>
            converter.ConvertBack(Visibility.Visible, typeof(Enum), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonVisibilityTargetType_Throws()
    {
        IValueConverter converter = new EnumToVisibilityConverter();

        Assert.ThrowsExactly<InvalidCastException>(() =>
            converter.Convert(Color.Red, typeof(bool), "Red", CultureInfo.InvariantCulture));
    }
}
