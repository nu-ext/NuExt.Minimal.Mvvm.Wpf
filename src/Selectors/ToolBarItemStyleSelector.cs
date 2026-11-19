using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// A style selector for toolbar items that selects a container style based on the item type.
/// </summary>
public sealed class ToolBarItemStyleSelector : StyleSelector
{
    #region Properties

    /// <summary>
    /// Gets or sets the style used for plain toolbar items.
    /// </summary>
    public Style? DefaultStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for toggle toolbar items.
    /// </summary>
    public Style? ToggleStyle { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// Selects a container style based on the item and the container.
    /// </summary>
    /// <param name="item">The data object for which to select the style.</param>
    /// <param name="container">The data-bound element.</param>
    /// <returns>
    /// The <see cref="Style"/> to apply, or null if no style is required.
    /// </returns>
    public override Style? SelectStyle(object item, DependencyObject container)
    {
        return item is ToggleToolBarItemViewModel ? ToggleStyle ?? DefaultStyle : DefaultStyle;
    }

    #endregion
}
