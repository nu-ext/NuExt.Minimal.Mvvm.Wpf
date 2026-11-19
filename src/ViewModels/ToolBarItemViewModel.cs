using System.Windows.Controls;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents a view model for a toolbar item that can include a command, header, and icon.
/// </summary>
public partial class ToolBarItemViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the command associated with this toolbar item.
    /// </summary>
    [Notify]
    private ICommand? _command;

    /// <summary>
    /// Gets or sets the parameter to be passed to the command.
    /// </summary>
    [Notify]
    private object? _commandParameter;

    /// <summary>
    /// Gets or sets the header displayed by the toolbar item.
    /// </summary>
    [Notify]
    private object? _header;

    /// <summary>
    /// Gets or sets the icon displayed by the toolbar item.
    /// </summary>
    [Notify]
    private object? _icon;

    /// <summary>
    /// Gets or sets a value indicating whether the toolbar item is enabled.
    /// </summary>
    [Notify]
    private bool _isEnabled = true;

    /// <summary>
    /// Gets or sets the tooltip displayed for the toolbar item.
    /// </summary>
    [Notify]
    private object? _toolTip;

    /// <summary>
    /// Gets or sets how the toolbar item participates in overflow layout.
    /// </summary>
    [Notify]
    private OverflowMode _overflowMode = OverflowMode.AsNeeded;
}