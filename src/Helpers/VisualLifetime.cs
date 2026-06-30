using System;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides weak subscriptions to visual lifetime events (Loaded/Unloaded).
/// Call <see cref="Observe"/> in <c>OnAttached</c> and <see cref="Unobserve"/> in <c>OnDetaching</c>
/// if your behavior needs to react to visual lifetime changes. The behavior itself does not auto-detach.
/// </summary>
public static class VisualLifetime
{
    /// <summary>
    /// Subscribes to Loaded/Unloaded using weak handlers. No-ops for non-FE/FCE.
    /// </summary>
    public static void Observe(DependencyObject target, EventHandler<RoutedEventArgs> onLoaded, EventHandler<RoutedEventArgs> onUnloaded)
    {
        if (target is FrameworkElement fe)
        {
            WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(fe, nameof(FrameworkElement.Loaded), onLoaded);
            WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(fe, nameof(FrameworkElement.Unloaded), onUnloaded);
        }
        else if (target is FrameworkContentElement fce)
        {
            WeakEventManager<FrameworkContentElement, RoutedEventArgs>.AddHandler(fce, nameof(FrameworkContentElement.Loaded), onLoaded);
            WeakEventManager<FrameworkContentElement, RoutedEventArgs>.AddHandler(fce, nameof(FrameworkContentElement.Unloaded), onUnloaded);
        }
    }

    /// <summary>
    /// Removes weak subscriptions added by <see cref="Observe"/>.
    /// </summary>
    public static void Unobserve(DependencyObject target, EventHandler<RoutedEventArgs> onLoaded, EventHandler<RoutedEventArgs> onUnloaded)
    {
        if (target is FrameworkElement fe)
        {
            WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(fe, nameof(FrameworkElement.Loaded), onLoaded);
            WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(fe, nameof(FrameworkElement.Unloaded), onUnloaded);
        }
        else if (target is FrameworkContentElement fce)
        {
            WeakEventManager<FrameworkContentElement, RoutedEventArgs>.RemoveHandler(fce, nameof(FrameworkContentElement.Loaded), onLoaded);
            WeakEventManager<FrameworkContentElement, RoutedEventArgs>.RemoveHandler(fce, nameof(FrameworkContentElement.Unloaded), onUnloaded);
        }
    }
}
