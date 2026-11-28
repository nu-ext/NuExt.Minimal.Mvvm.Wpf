using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf.Controls;

/// <summary>
/// Layout of a dialog: the view content and the command buttons below it.
/// </summary>
/// <remarks>
/// The layout is a content control rather than a window, so the hosting window keeps the style of the
/// active theme (Fluent, Aero, HighContrast) while this control stays theme agnostic: it only places
/// the view above the command buttons.
/// </remarks>
public class InputDialogContent : ContentControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="CommandsSource"/> dependency property.</summary>
    public static readonly DependencyProperty CommandsSourceProperty = DependencyProperty.Register(
        nameof(CommandsSource), typeof(IEnumerable), typeof(InputDialogContent));

    /// <summary>Identifies the <see cref="ButtonRowMargin"/> dependency property.</summary>
    public static readonly DependencyProperty ButtonRowMarginProperty = DependencyProperty.Register(
        nameof(ButtonRowMargin), typeof(Thickness), typeof(InputDialogContent), new PropertyMetadata(new Thickness(12)));

    /// <summary>Identifies the <see cref="ButtonTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty ButtonTemplateProperty = DependencyProperty.Register(
        nameof(ButtonTemplate), typeof(DataTemplate), typeof(InputDialogContent));

    /// <summary>Identifies the <see cref="ButtonTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty ButtonTemplateSelectorProperty = DependencyProperty.Register(
        nameof(ButtonTemplateSelector), typeof(DataTemplateSelector), typeof(InputDialogContent));

    #endregion

    static InputDialogContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InputDialogContent), new FrameworkPropertyMetadata(typeof(InputDialogContent)));
    }

    #region Properties

    /// <summary>
    /// Gets or sets the dialog commands rendered as buttons under the content.
    /// </summary>
    public IEnumerable? CommandsSource
    {
        get => (IEnumerable?)GetValue(CommandsSourceProperty);
        set => SetValue(CommandsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the margin around the button row: the gap to the dialog content above
    /// and to the dialog edges. This is a margin, not a padding: it changes the distance
    /// between the row and the surrounding elements.
    /// </summary>
    public Thickness ButtonRowMargin
    {
        get => (Thickness)GetValue(ButtonRowMarginProperty);
        set => SetValue(ButtonRowMarginProperty, value);
    }

    /// <summary>
    /// Gets or sets the template of a dialog button.
    /// </summary>
    /// <remarks>
    /// WPF gives the template priority over the selector: when both are set, the selector is never called.
    /// Set only one of them, or set the selector to <see langword="null"/> together with the template.
    /// </remarks>
    public DataTemplate? ButtonTemplate
    {
        get => (DataTemplate?)GetValue(ButtonTemplateProperty);
        set => SetValue(ButtonTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template selector of the dialog buttons. The style sets the default one
    /// (theme-like buttons), so a custom selector replaces it without touching the template.
    /// </summary>
    /// <remarks>
    /// The selector is consulted only when <see cref="ButtonTemplate"/> is <see langword="null"/>.
    /// </remarks>
    public DataTemplateSelector? ButtonTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ButtonTemplateSelectorProperty);
        set => SetValue(ButtonTemplateSelectorProperty, value);
    }

    #endregion
}
