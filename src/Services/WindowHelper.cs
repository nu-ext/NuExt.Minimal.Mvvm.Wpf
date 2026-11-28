using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Attached properties and methods for window creation services.
/// </summary>
public class WindowHelper
{
    #region Dependency Properties

    public static readonly DependencyProperty SetWindowOwnerProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.SetWindowOwner), typeof(bool), typeof(WindowHelper), new PropertyMetadata(defaultValue: false));

    public static readonly DependencyProperty WindowStartupLocationProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.WindowStartupLocation), typeof(WindowStartupLocation), typeof(WindowHelper), new PropertyMetadata(defaultValue: WindowStartupLocation.CenterScreen));

    public static readonly DependencyProperty WindowStyleProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.WindowStyle), typeof(Style), typeof(WindowHelper));

    public static readonly DependencyProperty WindowStyleKeyProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.WindowStyleKey), typeof(string), typeof(WindowHelper));

    public static readonly DependencyProperty WindowStyleSelectorProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.WindowStyleSelector), typeof(StyleSelector), typeof(WindowHelper));

    public static readonly DependencyProperty WindowTypeProperty = DependencyProperty.RegisterAttached(
        nameof(ViewWindowService.WindowType), typeof(Type), typeof(WindowHelper),
        new FrameworkPropertyMetadata(null), ValidateWindowType);

    public static bool GetSetWindowOwner(DependencyObject obj)
        => (bool)obj.GetValue(SetWindowOwnerProperty);

    public static void SetSetWindowOwner(DependencyObject obj, bool value)
        => obj.SetValue(SetWindowOwnerProperty, value);

    public static WindowStartupLocation GetWindowStartupLocation(DependencyObject obj)
        => (WindowStartupLocation)obj.GetValue(WindowStartupLocationProperty);

    public static void SetWindowStartupLocation(DependencyObject obj, WindowStartupLocation value)
        => obj.SetValue(WindowStartupLocationProperty, value);

    public static Style? GetWindowStyle(DependencyObject obj)
        => (Style?)obj.GetValue(WindowStyleProperty);

    public static void SetWindowStyle(DependencyObject obj, Style? value)
        => obj.SetValue(WindowStyleProperty, value);

    public static string? GetWindowStyleKey(DependencyObject obj)
        => (string?)obj.GetValue(WindowStyleKeyProperty);

    public static void SetWindowStyleKey(DependencyObject obj, string? value)
        => obj.SetValue(WindowStyleKeyProperty, value);

    public static StyleSelector? GetWindowStyleSelector(DependencyObject obj)
        => (StyleSelector?)obj.GetValue(WindowStyleSelectorProperty);

    public static void SetWindowStyleSelector(DependencyObject obj, StyleSelector? value)
        => obj.SetValue(WindowStyleSelectorProperty, value);

    /// <summary>
    /// Validates that the window type is a <see cref="Window"/>: the value is used as
    /// <c>(Window)Activator.CreateInstance(windowType)</c>, and a wrong type would fail much later.
    /// </summary>
    private static bool ValidateWindowType(object? value)
        => value is not Type windowType || typeof(Window).IsAssignableFrom(windowType);

    /// <summary>
    /// Gets the type of window to create; <see langword="null"/> means a plain <see cref="Window"/>.
    /// </summary>
    public static Type? GetWindowType(DependencyObject obj)
        => (Type?)obj.GetValue(WindowTypeProperty);

    /// <summary>
    /// Sets the type of window to create. The type must derive from <see cref="Window"/> and have
    /// a parameterless constructor; an invalid type is rejected when the value is set.
    /// </summary>
    public static void SetWindowType(DependencyObject obj, Type? value)
        => obj.SetValue(WindowTypeProperty, value);

    #endregion

    #region Methods

    public static Window CreateWindow(ViewServiceBase service, object? view, object? viewModel, Action<Window>? onWindowCreated = null)
    {
        Type? windowType;
        bool setWindowOwner;
        WindowStartupLocation windowStartupLocation;
        if (service.CheckAccess())
        {
            (windowType, setWindowOwner, windowStartupLocation) = (GetWindowType(service), GetSetWindowOwner(service), GetWindowStartupLocation(service));
        }
        else
        {
            (windowType, setWindowOwner, windowStartupLocation) = service.Dispatcher.Invoke(() => (GetWindowType(service), GetSetWindowOwner(service), GetWindowStartupLocation(service)), DispatcherPriority.Send);
        }

        var window = windowType != null ? (Window)Activator.CreateInstance(windowType)! : new Window();
        window.Title = "Untitled";
        window.Content = view;

        onWindowCreated?.Invoke(window);

        var windowStyle = GetWindowStyle(service, window, viewModel);
        if (windowStyle != null)
        {
            Debug.Assert(windowStyle.CheckAccess());
            window.Style = windowStyle;
        }

        if (setWindowOwner)
        {
            var owner = service.CheckAccess() ? GetWindow(service) : service.Dispatcher.Invoke(() => GetWindow(service), DispatcherPriority.Send);
            Debug.Assert(owner != null);
            Throw.IfNull(owner, $"{nameof(ViewWindowService.SetWindowOwner)} is true, but owner window is null.");
            if (owner.CheckAccess())
            {
                window.Owner = owner;
            }
            else
            {
                var mainWindowHandle = owner.Dispatcher.Invoke(() => new WindowInteropHelper(owner).Handle, DispatcherPriority.Send);
                _ = new WindowInteropHelper(window) { Owner = mainWindowHandle };
            }
        }
        window.WindowStartupLocation = windowStartupLocation;

        return window;
    }

    /// <summary>
    /// Attempts to retrieve the Window associated with the current object.
    /// </summary>
    /// <returns>
    /// The Window instance associated with the current object if available; otherwise, null.
    /// </returns>
    private static Window? GetWindow(ViewServiceBase service)
    {
        return service.AssociatedObject != null ? service.AssociatedObject as Window ?? Window.GetWindow(service.AssociatedObject) : null;
    }

    private static Style? GetWindowStyle(ViewServiceBase service, Window window, object? viewModel)
    {
        Debug.Assert(window.CheckAccess());

        Style? style;
        StyleSelector? styleSelector;

        if (service.CheckAccess())
        {
            style = GetWindowStyle(service) ?? GetWindowStyleByKey(service.AssociatedObject, GetWindowStyleKey(service));
            styleSelector = GetWindowStyleSelector(service);
        }
        else
        {
            (var associatedObject, var windowStyleKey, styleSelector) = service.Dispatcher.Invoke(() => (service.AssociatedObject, GetWindowStyleKey(service), GetWindowStyleSelector(service)), DispatcherPriority.Send);
            style = GetWindowStyleByKey(associatedObject, windowStyleKey);
        }

        // WindowStyle has first stab
        if (style != null)
        {
            return style;
        }

        // no WindowStyle set, try WindowStyleSelector
        if (styleSelector != null)
        {
            style = styleSelector.SelectStyle(viewModel, window);
        }

        return style;
    }

    private static Style? GetWindowStyleByKey(FrameworkElement? element, string? resourceKey)
    {
        if (element == null || string.IsNullOrEmpty(resourceKey))
        {
            return null;
        }
        Style? style;
        if (element.CheckAccess())
        {
            style = element.TryFindResource(resourceKey) as Style;
        }
        else
        {
            var lockObj = LockPool.Get(element);
            lock (lockObj)
            {
                style = element.TryFindResource(resourceKey) as Style;
            }
        }
        Trace.WriteLineIf(style == null, $"Style with key '{resourceKey}' not found.");
        Debug.Assert(style?.CheckAccess() == true, $"Style with key '{resourceKey}' not found or owned by other thread.");
        return style;
    }

    #endregion
}
