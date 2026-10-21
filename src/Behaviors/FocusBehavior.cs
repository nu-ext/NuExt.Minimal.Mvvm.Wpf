using Minimal.Behaviors.Wpf;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Sets keyboard focus to the associated UI element when the configured event is raised
/// (defaults to <see cref="FrameworkElement.Loaded"/>).
/// </summary>
public sealed class FocusBehavior : EventBehavior<UIElement>
{
    /// <summary>
    /// Gets or sets a value indicating whether focus should be scheduled asynchronously.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to schedule focus with <see cref="DispatcherPriority.Input"/> (default);
    /// otherwise, <see langword="false"/> for immediate attempt.
    /// </value>
    public bool UseAsyncFocus { get; set; } = true;

    /// <inheritdoc />
    protected override void OnEventCore(object? sender, object? eventArgs)
    {
        if (!UseAsyncFocus)
        {
            TrySetFocus(sender);
            return;
        }

        // Schedule focus on the next input tick; this is the recommended path in WPF
        // to avoid fighting layout/measure/render timing.
        Dispatcher.InvokeAsync(() => { if (IsEnabled) TrySetFocus(sender); }, DispatcherPriority.Input);
    }

    private void TrySetFocus(object? sender)
    {
        if (!IsAttached || !ReferenceEquals(sender, AssociatedObject))
        {
            return;
        }

        switch (sender)
        {
            case UIElement { IsVisible:true, IsEnabled: true, Focusable: true } uiElement:
                SetFocus(uiElement);
                break;
            // Best-effort: if synchronous path couldn't focus, schedule a one-shot retry.
            case UIElement uiElement when !UseAsyncFocus:
                Dispatcher.InvokeAsync(() =>
                {
                    if (IsEnabled && IsAttached && ReferenceEquals(uiElement, AssociatedObject) && uiElement is { IsVisible: true, IsEnabled: true, Focusable: true })
                    {
                        SetFocus(uiElement);
                    }
                }, DispatcherPriority.Input);
                break;
        }
        return;

        static void SetFocus(UIElement uiElement)
        {
            if (ReferenceEquals(Keyboard.FocusedElement, uiElement))
            {
                return;
            }
            if (!uiElement.Focus())
            {
                Keyboard.Focus(uiElement);
            }
        }
    }
}
