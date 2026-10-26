using Minimal.Behaviors.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides helper methods for managing the relationship between views and view models,
/// </summary>
public static class ViewModelHelper
{
    /// <summary>
    /// The name of the title property used in view models.
    /// </summary>
    public const string TitlePropertyName = "Title";

    private static bool? s_isInDesignMode;
    /// <summary>
    /// Gets a value indicating whether the ViewModel is in design mode.
    /// </summary>
    public static bool IsInDesignMode
    {
        get
        {
            s_isInDesignMode ??= (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(System.Windows.DependencyObject)).DefaultValue;
            return s_isInDesignMode.Value;
        }
    }

    /// <summary>
    /// Attaches a view model to the specified view by setting the appropriate property.
    /// </summary>
    /// <param name="view">The view to which the view model will be attached. It can be a <see cref="ContentPresenter"/>, <see cref="FrameworkElement"/>, or <see cref="FrameworkContentElement"/>.</param>
    /// <param name="viewModel">The view model to attach to the view.</param>
    public static void AttachViewModel(object? view, object? viewModel)
    {
        Debug.Assert(view is not DispatcherObject obj || obj.CheckAccess());
        switch (view)
        {
            case ContentPresenter cp: cp.Content = viewModel; break;
            case FrameworkElement fe: fe.DataContext = viewModel; break;
            case FrameworkContentElement fce: fce.DataContext = viewModel; break;
        }
    }

    /// <summary>
    /// Detaches the view model from the specified view by clearing the appropriate property.
    /// </summary>
    /// <param name="view">The view from which the view model will be detached. It can be a <see cref="ContentPresenter"/>, <see cref="FrameworkElement"/>, or <see cref="FrameworkContentElement"/>.</param>
    public static void DetachViewModel(object? view)
    {
        Debug.Assert(view is not DispatcherObject obj || obj.CheckAccess());
        switch (view)
        {
            case ContentPresenter cp: cp.Content = null; break;
            case FrameworkElement fe: fe.DataContext = null; break;
            case FrameworkContentElement fce: fce.DataContext = null; break;
        }
    }

    /// <summary>
    /// Detaches the current content from the specified <see cref="ContentControl"/>.
    /// </summary>
    /// <param name="container">The container from which to detach content. Must have dispatcher access.</param>
    /// <exception cref="ArgumentNullException"><paramref name="container"/> is <see langword="null"/>.</exception>
    public static void DetachContentFromContainer(ContentControl container)
    {
        ArgumentNullException.ThrowIfNull(container);
        Debug.Assert(container.CheckAccess());

        var view = container.Content;
        Debug.Assert(view != null);
        //First, detach DataContext from view
        DetachViewModel(view);
        //Second, detach Content from tab item
        container.Content = null;
        Debug.Assert(container.DataContext == null);
    }

    /// <summary>Gets the data item (view model) for the given WPF view.</summary>
    /// <param name="view">A view object.</param>
    /// <returns>The data item (DataContext or Content), or null.</returns>
    public static object? GetViewModelFromView(object? view)
    {
        Debug.Assert(view is not DispatcherObject dv || dv.CheckAccess());
        return view is DependencyObject obj ? Interaction.GetDataContext(obj) : null;
    }

    /// <summary>
    /// Retrieves the title of the view model if it exists.
    /// </summary>
    /// <param name="viewModel">The view model object from which to retrieve the title.</param>
    /// <returns>Returns the title of the view model if it exists; otherwise, returns string.Empty.</returns>
    public static string GetViewModelTitle(object viewModel)
    {
        Throw.IfNull(viewModel);
        if (viewModel is IAsyncDocumentContent documentContent)
        {
            return documentContent.Title;
        }
        if (viewModel is IWindowViewModel windowViewModel)
        {
            return windowViewModel.Title;
        }
        var titleProperty = viewModel.GetType().GetProperty(TitlePropertyName);
        return titleProperty != null && titleProperty.PropertyType == typeof(string) && titleProperty is { CanRead: true } 
            ? ((string?)titleProperty.GetValue(viewModel) ?? string.Empty) 
            : string.Empty;
    }

    /// <summary>
    /// Initializes the view and view model asynchronously based on the specified parameters.
    /// If <paramref name="cancellationToken"/> is canceled, no actions are performed and a canceled task is returned.
    /// </summary>
    /// <param name="view">The view to initialize.</param>
    /// <param name="viewModel">The view model associated with the view.</param>
    /// <param name="parentViewModel">The parent view model, if any.</param>
    /// <param name="parameter">Additional parameter for initializing the view.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// Sets the parent and parameter (when applicable), attaches the view model to the view,
    /// and performs asynchronous initialization if the view model implements <see cref="IAsyncInitializable"/>.
    /// </remarks>
    public static ValueTask InitializeViewAsync(object? view, object? viewModel, object? parentViewModel, object? parameter, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        // 1) Set parent and parameter BEFORE the view model is attached to the view (bindings not active yet)
        if (viewModel is IParentedViewModel parented)
        {
            parented.ParentViewModel ??= parentViewModel;
        }
        if (viewModel is IParameterizedViewModel parameterized)
        {
            parameterized.Parameter ??= parameter;
        }

        // 2) Attach the view model to the view
        AttachViewModel(view, viewModel);

        // 3) Initialize asynchronously once
        if (viewModel is IAsyncInitializable { IsInitialized: false } initializable)
        {
            return new ValueTask(initializable.InitializeAsync(cancellationToken));
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Sets a DataContext binding to the view model associated with the given view.
    /// </summary>
    /// <param name="sourceView">The view from which the view model will be retrieved.</param>
    /// <param name="property">The dependency property on the target object to which the binding will be set.</param>
    /// <param name="target">The object on which to set the binding.</param>
    public static void SetDataContextBinding(object? sourceView, DependencyProperty property, DependencyObject target)
    {
        Debug.Assert(sourceView != null);

        BindingBase binding = sourceView switch
        {
            ContentPresenter => new Binding
            {
                Path = new PropertyPath(nameof(ContentPresenter.Content)),
                Source = sourceView,
                Mode = BindingMode.OneWay
            },

            HeaderedContentControl => new PriorityBinding
            {
                Bindings =
                {
                    new Binding { Path = new PropertyPath(nameof(HeaderedContentControl.Content)), Source = sourceView, Mode = BindingMode.OneWay },
                    new Binding { Path = new PropertyPath(nameof(HeaderedContentControl.Header)),  Source = sourceView, Mode = BindingMode.OneWay },
                    new Binding { Path = new PropertyPath(nameof(HeaderedContentControl.DataContext)), Source = sourceView, Mode = BindingMode.OneWay },
                }
            },

            _ => new Binding
            {
                Path = new PropertyPath(nameof(FrameworkElement.DataContext)),
                Source = sourceView,
                Mode = BindingMode.OneWay
            }
        };
        Debug.Assert(sourceView is not DispatcherObject d || d.CheckAccess());
        BindingOperations.SetBinding(target, property, binding);
    }

    /// <summary>
    /// Sets a binding to the 'Title' property of the view model associated with the given view.
    /// </summary>
    /// <param name="sourceView">The view from which the view model will be retrieved.</param>
    /// <param name="property">The dependency property on the target object to which the binding will be set.</param>
    /// <param name="target">The object on which to set the binding.</param>
    public static void SetViewTitleBinding(object? sourceView, DependencyProperty property, DependencyObject target)
    {
        Debug.Assert(sourceView != null);

        BindingBase binding = sourceView switch
        {
            ContentPresenter => new Binding
            {
                Path = new PropertyPath("Content.Title"),
                Source = sourceView,
                Mode = BindingMode.TwoWay,
                FallbackValue = "",
                TargetNullValue = ""
            },

            HeaderedContentControl => new PriorityBinding
            {
                Bindings =
                {
                    new Binding { Path = new PropertyPath("Content.Title"), Source = sourceView, Mode = BindingMode.TwoWay, FallbackValue = "", TargetNullValue = "" },
                    new Binding { Path = new PropertyPath("Header.Title"),  Source = sourceView, Mode = BindingMode.TwoWay, FallbackValue = "", TargetNullValue = "" },
                    new Binding { Path = new PropertyPath("DataContext.Title"), Source = sourceView, Mode = BindingMode.TwoWay, FallbackValue = "", TargetNullValue = "" },
                }
            },

            _ => new Binding
            {
                Path = new PropertyPath("DataContext.Title"),
                Source = sourceView,
                Mode = BindingMode.TwoWay,
                FallbackValue = "",
                TargetNullValue = ""
            }
        };
        Debug.Assert(sourceView is not DispatcherObject d || d.CheckAccess());
        BindingOperations.SetBinding(target, property, binding);

    }

    /// <summary>
    /// Checks if the provided ViewModel has a 'Title' property that is readable and writable.
    /// </summary>
    /// <param name="viewModel">The ViewModel object to check.</param>
    /// <returns>Returns <see langword="true"/> if the ViewModel contains a 'Title' property that can be read from and written to; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided viewModel is null.</exception>
    public static bool ViewModelHasTitleProperty(object viewModel)
    {
        Throw.IfNull(viewModel);
        if (viewModel is DocumentContentViewModelBase or WindowViewModelBase)
        {
            return true;
        }
        var titleProperty = viewModel.GetType().GetProperty(TitlePropertyName);
        return titleProperty != null && titleProperty.PropertyType == typeof(string) && titleProperty is { CanRead: true, CanWrite: true };
    }

    private static Task WaitLoadedAsync(object? view, CancellationToken cancellationToken)
    {
        if (view is not FrameworkElement fe || fe.IsLoaded) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationTokenRegistration ctr = default;
        int disposed = 0;

        fe.Loaded += OnLoaded;

        if (cancellationToken.CanBeCanceled)
        {
            ctr = cancellationToken.Register(() =>
            {
                fe.Loaded -= OnLoaded;
                DisposeOnce();
                tcs.TrySetCanceled(cancellationToken);
            }, useSynchronizationContext: true);

            if (Volatile.Read(ref disposed) != 0)
            {
                ctr.Dispose();
            }
        }

        return tcs.Task;


        void DisposeOnce()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                ctr.Dispose();
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(fe.CheckAccess());
            fe.Loaded -= OnLoaded;
            DisposeOnce();
            tcs.TrySetResult(null);
        }
    }
}
