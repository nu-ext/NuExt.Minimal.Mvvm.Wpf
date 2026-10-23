using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Minimal.Behaviors.Wpf;
using Presentation.Wpf.Controls;

namespace Minimal.Mvvm.Wpf;

public class TreeViewSelectedItemBehavior : Behavior<TreeView>
{
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(TreeViewSelectedItemBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    private bool _inSelectedItemChanged;

    #region Properties

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region Event Handlers

    private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue == e.NewValue)
        {
            return;
        }
        if (sender is not TreeViewSelectedItemBehavior behavior) return;

        if (behavior._inSelectedItemChanged)
        {
            return;
        }

        var tree = behavior.AssociatedObject;
        if (tree == null) return;
        if (tree.IsLoaded == false)
        {
            return;
        }

        SelectItem(tree, e.NewValue);
    }

    private void OnTreeViewLoaded(object sender, RoutedEventArgs e)
    {
        Debug.Assert(!_inSelectedItemChanged);
        if (_inSelectedItemChanged)
        {
            return;
        }

        if (sender is not TreeView tree) return;
        if (tree.SelectedItem != SelectedItem)
        {
            SelectItem(tree, SelectedItem);
        }
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _inSelectedItemChanged = true;
        try
        {
            SelectedItem = e.NewValue;
        }
        finally
        {
            _inSelectedItemChanged = false;
        }
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject!.Loaded += OnTreeViewLoaded;
        AssociatedObject!.SelectedItemChanged += OnTreeViewSelectedItemChanged;

        if (AssociatedObject.IsLoaded && AssociatedObject.SelectedItem != SelectedItem)
        {
            SelectItem(AssociatedObject, SelectedItem);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded -= OnTreeViewLoaded;
            AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
        }

        base.OnDetaching();
    }

    private static void SelectItem(TreeView tree, object? newItem)
    {
        if (newItem == null)
        {
            if (tree.SelectedItem is { } selectedItem)
            {
                var selectedContainer = selectedItem as TreeViewItem ?? tree.GetTreeViewItem(selectedItem);

                selectedContainer?.SetCurrentValue(TreeViewItem.IsSelectedProperty, false);
            }
            return;
        }

        if (newItem is TreeViewItem treeViewItem)
        {
            treeViewItem.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);
            //treeViewItem.Focus();
            return;
        }

        var dataBoundTreeViewItem = tree.GetTreeViewItem(newItem);
        Debug.Assert(dataBoundTreeViewItem != null);
        if (dataBoundTreeViewItem == null) return;
        dataBoundTreeViewItem.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);
    }

    #endregion
}