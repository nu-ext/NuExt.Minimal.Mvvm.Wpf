using Minimal.Behaviors.Wpf;
using Presentation.Wpf;
using Presentation.Wpf.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// A behavior that enables multiple selection in a ComboBox.
/// </summary>
public class ComboBoxMultiSelectionBehavior : Behavior<ComboBox>
{
    private IDisposable? _subscription;

    #region Dependency Properties

    /// <summary>
    /// Identifies the SelectedItems dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems), typeof(IList), typeof(ComboBoxMultiSelectionBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((ComboBoxMultiSelectionBehavior)d).OnSelectedItemsChanged(e.OldValue as IList, e.NewValue as IList)));

    /// <summary>
    /// Identifies the SelectedItemsAsString dependency property.
    /// </summary>
    private static readonly DependencyProperty SelectedItemsAsStringProperty = DependencyProperty.Register(
        "SelectedItemsAsString", typeof(string), typeof(ComboBoxMultiSelectionBehavior));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the selected items of the ComboBox.
    /// </summary>
    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the DropDownClosed event of the ComboBox.
    /// Sets the selected items and updates the selected items as string.
    /// </summary>
    /// <param name="sender">The source of the event, usually a ComboBox.</param>
    /// <param name="e">The event data.</param>
    private void OnComboBoxDropDownClosed(object? sender, EventArgs e)
    {
        Debug.Assert(sender is ComboBox);

        if (sender is ComboBox comboBox)
        {
            SetSelectedItems(comboBox);
            SetSelectedItemsAsString(comboBox, SelectedItems);
        }
    }

    /// <summary>
    /// Handles the DropDownOpened event of the ComboBox.
    /// Updates the selected items from the ComboBox.
    /// </summary>
    /// <param name="sender">The source of the event, usually a ComboBox.</param>
    /// <param name="e">The event data.</param>
    private void OnComboBoxDropDownOpened(object? sender, EventArgs e)
    {
        Debug.Assert(sender is ComboBox);
        if (sender is ComboBox comboBox)
        {
            comboBox.Dispatcher.InvokeAsync(() => SetSelectedItems(comboBox, SelectedItems));
        }
    }

    /// <summary>
    /// Called when the SelectedItems property changes.
    /// Updates the selected items and their string representation.
    /// </summary>
    /// <param name="oldItems">The old selected items.</param>
    /// <param name="newItems">The new selected items.</param>
    private void OnSelectedItemsChanged(IList? oldItems, IList? newItems)
    {
        if (ReferenceEquals(oldItems, newItems))
        {
            return;
        }

        if (AssociatedObject is not { } comboBox) return;

        SetSelectedItems(comboBox, newItems);
        SetSelectedItemsAsString(comboBox, newItems);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Called when the behavior is attached to a ComboBox.
    /// Subscribes to necessary events and sets initial selected items.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        Debug.Assert(_subscription == null);
        Disposable.DisposeAndNull(ref _subscription);
        _subscription = SubscribeComboBox(AssociatedObject!);
        SetSelectedItems(AssociatedObject!, SelectedItems);
    }

    /// <summary>
    /// Called when the behavior is detached from the ComboBox.
    /// Unsubscribes from events.
    /// </summary>
    protected override void OnDetaching()
    {
        Debug.Assert(_subscription != null);
        Disposable.DisposeAndNull(ref _subscription);
        base.OnDetaching();
    }

    /// <summary>
    /// Sets the selected items in the ComboBox based on the CheckBoxes' states.
    /// </summary>
    /// <param name="comboBox">The ComboBox control.</param>
    private void SetSelectedItems(ComboBox comboBox)
    {
        var comboBoxItems = comboBox.GetComboBoxItems();
        if (comboBoxItems == null)
        {
            return;
        }
        var list = comboBoxItems as IList<ComboBoxItem> ?? [.. comboBoxItems];

        var selectedItems = new List<object>();
        foreach (var comboBoxItem in list)
        {
            var checkBox = comboBoxItem.FindChild<CheckBox>();
            if (checkBox?.DataContext == null || checkBox.IsChecked != true) continue;
            selectedItems.Add(checkBox.DataContext);
        }

        var oldItems = SelectedItems;
        if (oldItems != null)
        {
            oldItems.Clear();
            if (!selectedItems.IsNullOrEmpty())
            {
                foreach (var item in selectedItems)
                {
                    oldItems.Add(item);
                }
            }
            comboBox.SelectedItem = oldItems is { Count: > 0 } ? oldItems[0] : null;
            return;
        }
        SelectedItems = selectedItems;
    }

    /// <summary>
    /// Sets the selected items in the ComboBox.
    /// </summary>
    /// <param name="comboBox">The ComboBox control.</param>
    /// <param name="selectedItems">The list of selected items.</param>
    private static void SetSelectedItems(ComboBox comboBox, IList? selectedItems)
    {
        comboBox.SelectedItem = selectedItems is { Count: > 0 } ? selectedItems[0] : null;
        if (!comboBox.IsDropDownOpen)
        {
            return;
        }

        var comboBoxItems = comboBox.GetComboBoxItems();
        if (comboBoxItems == null)
        {
            return;
        }
        var list = comboBoxItems as IList<ComboBoxItem> ?? [.. comboBoxItems];

        var checkBoxItems = new CheckBox?[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var comboBoxItem = list[i];
            var checkBox = comboBoxItem.FindChild<CheckBox>();
            Debug.Assert(checkBox != null);
            checkBoxItems[i] = checkBox;
        }

        if (selectedItems == null || selectedItems.Count == 0)
        {
            foreach (var checkBox in checkBoxItems)
            {
                if (checkBox == null)
                {
                    Debug.Fail($"{nameof(checkBox)} is null");
                    continue;
                }
                checkBox.IsChecked = false;
            }
        }
        else
        {
            var checkedIndexes = new HashSet<int>();
            foreach (var data in selectedItems)
            {
                if (data == null)
                {
                    Debug.Fail($"{nameof(data)} is null");
                    continue;
                }
                for (int i = 0; i < checkBoxItems.Length; i++)
                {
                    var checkBox = checkBoxItems[i];
                    if (checkBox == null || !data.Equals(checkBox.DataContext)) continue;
                    checkBox.IsChecked = true;
                    checkedIndexes.Add(i);
                }
            }//foreach
            for (int i = 0; i < checkBoxItems.Length; i++)
            {
                if (checkedIndexes.Contains(i)) continue;
                var checkBox = checkBoxItems[i];
                if (checkBox == null)
                {
                    Debug.Fail($"{nameof(checkBox)} is null");
                    continue;
                }
                checkBox.IsChecked = false;
            }
        }
    }

    /// <summary>
    /// Subscribes to the ComboBox events for managing multi-selection.
    /// </summary>
    /// <param name="comboBox">The ComboBox control.</param>
    private Lifetime SubscribeComboBox(ComboBox comboBox)
    {
        var lifetime = new Lifetime()
#if DEBUG
                .SetDebugInfo()
#endif
            ;

        lifetime.AddBracket(() => comboBox.DropDownOpened += OnComboBoxDropDownOpened,
            () => comboBox.DropDownOpened -= OnComboBoxDropDownOpened);
        lifetime.AddBracket(() => comboBox.DropDownClosed += OnComboBoxDropDownClosed,
            () => comboBox.DropDownClosed -= OnComboBoxDropDownClosed);

        return lifetime;
    }

    /// <summary>
    /// Sets the selected items in the ComboBox as a comma-separated string.
    /// </summary>
    /// <param name="comboBox">The ComboBox control.</param>
    /// <param name="items">The list of selected items.</param>
    private static void SetSelectedItemsAsString(ComboBox comboBox, IList? items)
    {
        var sb = new ValueStringBuilder(stackalloc char[128]);
        if (items is { Count: > 0 })
        {
            var s = string.Empty;
            foreach (var item in items)
            {
                sb.Append(s);
                sb.Append(item?.ToString());
                s = ", ";
            }
        }
        SetSelectedItemsAsString(comboBox, sb.ToString());
    }

    /// <summary>
    /// Sets the selected items as a string value on the UIElement.
    /// </summary>
    /// <param name="element">The UIElement control.</param>
    /// <param name="value">The string value representing selected items.</param>
    private static void SetSelectedItemsAsString(UIElement element, string value)
    {
        element.SetValue(SelectedItemsAsStringProperty, value);
    }

    /// <summary>
    /// Gets the selected items as a string value from the UIElement.
    /// </summary>
    /// <param name="element">The UIElement control.</param>
    /// <returns>A string value representing the selected items.</returns>
    public static string? GetSelectedItemsAsString(UIElement element)
    {
        return (string?)element.GetValue(SelectedItemsAsStringProperty);
    }

    #endregion
}
