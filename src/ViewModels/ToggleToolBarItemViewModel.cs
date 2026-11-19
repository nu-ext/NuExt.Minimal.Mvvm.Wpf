using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents a toggle toolbar item that can include a command, header, icon, and checked state.
/// </summary>
public partial class ToggleToolBarItemViewModel : ToolBarItemViewModel
{
    /// <summary>
    /// Gets or sets the command invoked when the toolbar item becomes checked.
    /// </summary>
    [Notify]
    private ICommand? _checkedCommand;

    /// <summary>
    /// Gets or sets the command invoked when the toolbar item becomes unchecked.
    /// </summary>
    [Notify]
    private ICommand? _uncheckedCommand;

    /// <summary>
    /// Gets or sets the command invoked when the toolbar item enters the indeterminate state.
    /// </summary>
    [Notify]
    private ICommand? _indeterminateCommand;

    /// <summary>
    /// Gets or sets a value indicating whether the toolbar item is checked.
    /// <see langword="null"/> represents the indeterminate state.
    /// </summary>
    [Notify]
    private bool? _isChecked;

    /// <summary>
    /// Gets or sets a value indicating whether the toolbar item supports the indeterminate state.
    /// </summary>
    [Notify]
    private bool _isThreeState;
}
