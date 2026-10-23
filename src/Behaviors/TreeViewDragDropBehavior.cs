using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Minimal.Behaviors.Wpf;
using Presentation.Wpf;

namespace Minimal.Mvvm.Wpf;

public class TreeViewDragDropBehavior : Behavior<TreeView>
{
    public static readonly DependencyProperty MoveCommandProperty = DependencyProperty.Register(
        nameof(MoveCommand), typeof(ICommand), typeof(TreeViewDragDropBehavior));

    private Rectangle? _dragBoxFromMouseDown;
    private object? _mouseDownOriginalSource;

    private IDisposable? _subscription;

    #region Properties

    public ICommand? MoveCommand
    {
        get => (ICommand?)GetValue(MoveCommandProperty);
        set => SetValue(MoveCommandProperty, value);
    }

    #endregion

    #region Event Handlers

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(IDragDrop)) is not IDragDrop draggedObject || GetDragDrop(e.OriginalSource)?.CanDrop(draggedObject) != true)
        {
            e.Effects = DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.Move;
        }
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(IDragDrop)) is IDragDrop draggedObject && MoveCommand?.CanExecute(draggedObject) != false && GetDragDrop(e.OriginalSource)?.Drop(draggedObject) == true)
        {
            e.Handled = true;
            MoveCommand?.Execute(draggedObject);
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _dragBoxFromMouseDown = null;
            _mouseDownOriginalSource = null;
            return;
        }

        var position = e.GetPosition(null);

        if (_dragBoxFromMouseDown == null || _dragBoxFromMouseDown.Value.Contains((int)position.X, (int)position.Y))
        {
            return;
        }

        IDragDrop? dragDrop;
        try
        {
            dragDrop = GetDragDrop(_mouseDownOriginalSource);
        }
        finally
        {
            _dragBoxFromMouseDown = null;
            _mouseDownOriginalSource = null;
        }

        if (dragDrop is { CanDrag: true })
        {
            DragDrop.DoDragDrop(AssociatedObject!, new DataObject(typeof(IDragDrop), dragDrop), DragDropEffects.Move);
        }
    }

    private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        Debug.Assert(Equals(sender, AssociatedObject));

        var position = e.GetPosition(null);

        _mouseDownOriginalSource = e.OriginalSource;

        _dragBoxFromMouseDown = new Rectangle(
            (int)(position.X - SystemParameters.MinimumHorizontalDragDistance),
            (int)(position.Y - SystemParameters.MinimumVerticalDragDistance),
            (int)(SystemParameters.MinimumHorizontalDragDistance * 2),
            (int)(SystemParameters.MinimumVerticalDragDistance * 2));
    }

    private void OnMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
    {
        _dragBoxFromMouseDown = null;
        _mouseDownOriginalSource = null;
    }

    private void OnTreeViewLoaded(object? sender, RoutedEventArgs e)
    {
        Debug.Assert(Equals(sender, AssociatedObject));
        if (sender is FrameworkElement fe)
        {
            fe.Loaded -= OnTreeViewLoaded;
        }
        Debug.Assert(_subscription == null);
        Disposable.DisposeAndNull(ref _subscription);
        _subscription = SubscribeTreeView(AssociatedObject);
    }

    #endregion

    #region Methods

    private static IDragDrop? GetDragDrop(object? source)
    {
        if (source is not DependencyObject obj) return null;
        var treeViewItem = obj as TreeViewItem ?? obj.FindParent<TreeViewItem>();
        return treeViewItem?.DataContext as IDragDrop;
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        Debug.Assert(_subscription == null);

        if (AssociatedObject!.IsLoaded)
        {
            Disposable.DisposeAndNull(ref _subscription);
            _subscription = SubscribeTreeView(AssociatedObject);
        }
        else
        {
            AssociatedObject.Loaded += OnTreeViewLoaded;
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        AssociatedObject!.Loaded -= OnTreeViewLoaded;
        Disposable.DisposeAndNull(ref _subscription);

        base.OnDetaching();
    }

    private Lifetime? SubscribeTreeView(TreeView? treeView)
    {
        Debug.Assert(treeView != null);
        if (treeView == null)
        {
            return null;
        }
        var lifetime = new Lifetime()
#if DEBUG
            .SetDebugInfo()
#endif
            ;
        lifetime.AddBracket(() => treeView.DragOver += OnDragOver, () => treeView.DragOver -= OnDragOver);
        lifetime.AddBracket(() => treeView.Drop += OnDrop, () => treeView.Drop -= OnDrop);
        lifetime.AddBracket(() => treeView.PreviewMouseMove += OnMouseMove, () => treeView.PreviewMouseMove -= OnMouseMove);
        lifetime.AddBracket(() => treeView.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown, () => treeView.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown);
        lifetime.AddBracket(() => treeView.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp, () => treeView.PreviewMouseLeftButtonUp -= OnMouseLeftButtonUp);
        return lifetime;
    }

    #endregion
}
