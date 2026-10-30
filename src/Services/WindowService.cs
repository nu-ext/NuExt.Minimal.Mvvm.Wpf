using Presentation.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides a service for interacting with a Window associated with a FrameworkElement.
/// </summary>
public class WindowService : WindowServiceBase, IWindowService
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the ClosingCommand dependency property.
    /// </summary>
    public static readonly DependencyProperty ClosingCommandProperty = DependencyProperty.Register(
        nameof(ClosingCommand), typeof(ICommand), typeof(WindowService),
        new PropertyMetadata(defaultValue: null, (d, e) => ((WindowService)d).OnClosingCommandChanged((ICommand)e.OldValue, (ICommand)e.NewValue)));

    #endregion

    private bool? _isClosed;

    #region Properties

    /// <summary>
    /// Gets or sets the command to execute when the Closing event is raised.
    /// </summary>
    public ICommand? ClosingCommand
    {
        get => (ICommand?)GetValue(ClosingCommandProperty);
        set => SetValue(ClosingCommandProperty, value);
    }

    /// <inheritdoc />
    public bool IsClosed => _isClosed == true;

    /// <inheritdoc />
    public bool IsVisible => Window?.IsVisible == true;

    #endregion

    #region Event Handlers

    protected virtual void OnClosingCommandChanged(ICommand? oldCommand, ICommand? newCommand)
    {

    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        var command = ClosingCommand;
        if (command?.CanExecute(e) == true)
        {
            command.Execute(e);
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    public void Activate()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }

        try
        {
            if (!IsVisible)
            {
                Window?.Show();
            }
            Window?.Activate();
        }
        catch (InvalidOperationException)
        {
            _isClosed = true;
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }
        Window?.Close();
    }

    /// <inheritdoc />
    public void Hide()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }

        try
        {
            Window?.Hide();
        }
        catch (InvalidOperationException)
        {
            _isClosed = true;
        }
    }

    /// <inheritdoc />
    public void Show()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }

        try
        {
            Window?.BringToFront();
        }
        catch (InvalidOperationException)
        {
            _isClosed = true;
        }
    }

    /// <inheritdoc />
    public void Minimize()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }
        Window?.WindowState = WindowState.Minimized;
    }

    /// <inheritdoc />
    public void Maximize()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }
        Window?.WindowState = WindowState.Maximized;
    }

    /// <inheritdoc />
    public void Restore()
    {
        VerifyAccess();
        if (_isClosed == true || !IsEnabled)
        {
            return;
        }
        Window?.WindowState = WindowState.Normal;
    }

    protected override void OnWindowChanged(Window? oldWindow, Window? newWindow)
    {
        if (oldWindow != null)
        {
            oldWindow.Closing -= OnWindowClosing;
            oldWindow.Closed -= OnWindowClosed;
        }
        _isClosed = null;
        if (newWindow != null)
        {
            newWindow.Closing += OnWindowClosing;
            newWindow.Closed += OnWindowClosed;
        }

        base.OnWindowChanged(oldWindow, newWindow);
    }

    #endregion
}
