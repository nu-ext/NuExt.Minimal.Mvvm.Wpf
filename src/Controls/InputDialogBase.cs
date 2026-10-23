using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf.Controls;

[TemplatePart(Name = PART_Content, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_Bottom, Type = typeof(ContentPresenter))]
public abstract class InputDialogBase : Window
{
    private const string PART_Content = "PART_Content";
    private const string PART_Bottom = "PART_Bottom";

    #region Dependency Properties

    /// <summary>Identifies the <see cref="DialogBottom"/> dependency property.</summary>
    public static readonly DependencyProperty DialogBottomProperty = DependencyProperty.Register(
        nameof(DialogBottom), typeof(object), typeof(InputDialogBase), new PropertyMetadata(null, UpdateLogicalChild));

    #endregion

    static InputDialogBase()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InputDialogBase), new FrameworkPropertyMetadata(typeof(InputDialogBase)));
    }

    protected InputDialogBase()
    {
        DataContextChanged += InputDialog_DataContextChanged;
    }

    #region Properties

    public object? DialogBottom
    {
        get => GetValue(DialogBottomProperty);
        set => SetValue(DialogBottomProperty, value);
    }

    protected override IEnumerator LogicalChildren
    {
        get
        {
            var children = new ArrayList();
            if (Content != null)
            {
                children.Add(Content);
            }

            if (DialogBottom != null)
            {
                children.Add(DialogBottom);
            }

            return children.GetEnumerator();
        }
    }

    #endregion

    #region Event Handlers

    private void InputDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DialogBottom is FrameworkElement elementBottom)
        {
            elementBottom.SetCurrentValue(DataContextProperty, DataContext);
        }
    }

    private static void UpdateLogicalChild(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not InputDialogBase window)
        {
            return;
        }

        if (e.OldValue is FrameworkElement oldChild)
        {
            window.RemoveLogicalChild(oldChild);
        }

        if (e.NewValue is FrameworkElement newChild)
        {
            window.AddLogicalChild(newChild);
            newChild.SetCurrentValue(DataContextProperty, window.DataContext);
        }
    }

    #endregion
}
