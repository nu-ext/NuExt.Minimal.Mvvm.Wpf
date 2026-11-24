using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class StringToVisibilityConverterTests
{
    private static Visibility Convert(object? value, bool invert = false, bool hideInsteadOfCollapse = false)
    {
        IValueConverter converter = new StringToVisibilityConverter
        {
            Invert = invert,
            HideInsteadOfCollapse = hideInsteadOfCollapse
        };

        return (Visibility)converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture)!;
    }

    // ---------- Text ----------

    [TestMethod]
    public void Text_IsVisible()
    {
        string[] values = ["a", "text", "  padded  "];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Visible, Convert(value), $"Value '{value}' must be visible.");
        }
    }

    [TestMethod]
    public void EmptyText_IsCollapsed()
    {
        object?[] values = [null, "", "   ", "\t\r\n", DependencyProperty.UnsetValue, 5, true];

        foreach (var value in values)
        {
            Assert.AreEqual(Visibility.Collapsed, Convert(value), $"Value '{value ?? "null"}' must collapse.");
        }
    }

    [TestMethod]
    public void EmptyText_ReturnsHiddenInsteadOfCollapsed()
    {
        Assert.AreEqual(Visibility.Hidden, Convert("", hideInsteadOfCollapse: true));
        Assert.AreEqual(Visibility.Hidden, Convert(null, hideInsteadOfCollapse: true));
        Assert.AreEqual(Visibility.Visible, Convert("text", hideInsteadOfCollapse: true));
    }

    // ---------- Invert ----------

    [TestMethod]
    public void Invert_ShowsElementOnlyWithoutText()
    {
        Assert.AreEqual(Visibility.Visible, Convert("", invert: true));
        Assert.AreEqual(Visibility.Visible, Convert(null, invert: true));
        Assert.AreEqual(Visibility.Visible, Convert("   ", invert: true));
        Assert.AreEqual(Visibility.Collapsed, Convert("text", invert: true));
    }

    // ---------- Converter contract ----------

    [TestMethod]
    public void ConvertBack_IsNotSupported()
    {
        IValueConverter converter = new StringToVisibilityConverter();

        Assert.ThrowsExactly<NotSupportedException>(() =>
            converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Convert_WithNonVisibilityTargetType_Throws()
    {
        IValueConverter converter = new StringToVisibilityConverter();

        Assert.ThrowsExactly<InvalidCastException>(() =>
            converter.Convert("text", typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
