using Minimal.Behaviors.Wpf;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf;

public class ListBoxSelectionBehavior : Behavior<ListBox>
{
    private bool _invokedByView;
    private bool _invokedByModel;

    #region Dependency Properties

    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems), typeof(IList), typeof(ListBoxSelectionBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    #endregion

    #region Properties

    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    #endregion

    #region Event Handlers

    private void OnListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Debug.Assert(_invokedByView == false);
        if (_invokedByModel)
        {
            return;
        }
        if (AssociatedObject == null)
        {
            return;
        }
        _invokedByView = true;
        try
        {
            SelectedItems = AssociatedObject.SelectedItems?.Cast<object>().ToArray();
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.Message);
        }
        finally
        {
            _invokedByView = false;
        }
    }

    private void OnListBoxItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SelectItemsInView(SelectedItems);
    }

    private static void OnSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (ListBoxSelectionBehavior)sender;
        behavior.SelectItemsInView((IList)e.NewValue);
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject!.SelectionChanged += OnListBoxSelectionChanged;
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += OnListBoxItemsCollectionChanged;

        SelectItemsInView(SelectedItems);
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;
            ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= OnListBoxItemsCollectionChanged;
        }

        base.OnDetaching();
    }

    private void SelectItemsInView(IList? items)
    {
        Debug.Assert(_invokedByModel == false);
        if (_invokedByView)
        {
            return;
        }
        if (AssociatedObject is not { SelectionMode: SelectionMode.Extended })
        {
            return;
        }
        _invokedByModel = true;
        try
        {
            var list = AssociatedObject.SelectedItems;
            list.Clear();
            if (items?.Count > 0)
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.Message);
        }
        finally
        {
            _invokedByModel = false;
        }
    }

    #endregion
}
