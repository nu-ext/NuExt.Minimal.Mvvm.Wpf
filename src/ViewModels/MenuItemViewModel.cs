using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents a menu item with a command, visual state, and optional submenu items.
/// </summary>
public partial class MenuItemViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the command associated with this menu item.
    /// </summary>
    [Notify]
    private ICommand? _command;

    /// <summary>
    /// Gets or sets the parameter passed to the command.
    /// </summary>
    [Notify]
    private object? _commandParameter;

    /// <summary>
    /// Gets or sets the header displayed by the menu item.
    /// </summary>
    [Notify]
    private object? _header;

    /// <summary>
    /// Gets or sets the icon displayed by the menu item.
    /// </summary>
    [Notify]
    private object? _icon;

    /// <summary>
    /// Gets or sets the text describing the associated input gesture.
    /// </summary>
    [Notify]
    private string? _inputGestureText;

    /// <summary>
    /// Gets or sets a value indicating whether the menu item can be checked.
    /// </summary>
    [Notify]
    private bool _isCheckable;

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is checked.
    /// </summary>
    [Notify]
    private bool _isChecked;

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is enabled.
    /// </summary>
    [Notify]
    private bool _isEnabled = true;

    /// <summary>
    /// Gets or sets a value indicating whether the parent menu remains open when this item is clicked.
    /// </summary>
    [Notify]
    private bool _staysOpenOnClick;

    /// <summary>
    /// Gets or sets the submenu items.
    /// </summary>
    [Notify]
    private ObservableCollection<MenuItemViewModel?>? _subMenuItems;
}