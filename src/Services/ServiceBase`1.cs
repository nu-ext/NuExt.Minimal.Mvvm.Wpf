using Minimal.Behaviors.Wpf;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents a base class for services that live on a <see cref="FrameworkElement"/>.
/// </summary>
/// <remarks>
/// <para>
/// The service registers itself in the service container of the attached element's DataContext
/// (the ViewModel implements <see cref="IServiceContainerProvider"/>) under its type and <see cref="Name"/>,
/// so the ViewModel resolves it instead of creating it.
/// </para>
/// <para>
/// The registration follows the element: it is created when the DataContext arrives, moved when the name changes,
/// and removed when the behavior is detached. A newer instance of the same type and name replaces the previous one,
/// because a view can be re-created by a template change while its ViewModel stays alive.
/// </para>
/// <para>
/// The service is not an owner of resources: WPF provides no notification for a discarded element, so subscribe
/// to what is needed in <c>Loaded</c> and release it in <c>Unloaded</c> or <see cref="OnDetaching"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the FrameworkElement the service is attached to.</typeparam>
[RuntimeNameProperty(nameof(Name))]
public abstract class ServiceBase<T> : Behavior<T> where T : FrameworkElement
{
#if DEBUG
    public string? DebugName { get; private set; }
#endif

    #region Dependency Properties

    /// <summary>
    /// Identifies the DataContext dependency property.
    /// </summary>
    private static readonly DependencyProperty DataContextProperty = DependencyProperty.Register(
        nameof(DataContext), typeof(object), typeof(ServiceBase<T>),
        new PropertyMetadata(defaultValue: null, (d, e) => ((ServiceBase<T>)d).OnDataContextChanged(e.OldValue, e.NewValue)));

    /// <summary>
    /// Identifies the <see cref="Name"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
        nameof(Name), typeof(string), typeof(ServiceBase<T>),
        new PropertyMetadata(defaultValue: null, (d, e) => ((ServiceBase<T>)d).OnNameChanged((string?)e.OldValue, (string?)e.NewValue)));

    #endregion

    #region Properties

    /// <summary>
    /// Gets the DataContext of the attached FrameworkElement.
    /// </summary>
    public object? DataContext => GetValue(DataContextProperty);

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the service should register itself in the ViewModel.
    /// Default value is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property should be set during initialization (typically in XAML) and not modified afterward.
    /// </para>
    /// <para>
    /// Changing this property while the service is attached has no effect.
    /// </para>
    /// </remarks>
    public bool ShouldRegisterInViewModel
    {
        get;
        set
        {
            if (IsAttached) return;
            field = value;
        }
    } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the service unregisters itself when the attached element is unloaded
    /// and registers again when it is loaded back.
    /// Default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registration itself is created by the DataContext, not by the load state: this option only suspends it
    /// while the element is not loaded. Enable it when the ViewModel must not resolve a service that belongs to
    /// a hidden element.
    /// </para>
    /// <para>
    /// It is not needed for a re-created view: the newer instance replaces the previous registration anyway.
    /// </para>
    /// <para>
    /// This property should be set during initialization (typically in XAML) and not modified afterward.
    /// </para>
    /// <para>
    /// Changing this property while the service is attached has no effect.
    /// </para>
    /// </remarks>
    public bool ShouldUnregisterOnUnloaded
    {
        get;
        set
        {
            if (IsAttached) return;
            field = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the service is registered in the container of the ViewModel.
    /// </summary>
    /// <remarks>
    /// A plain CLR property on purpose: a dependency property would be copied by the <see cref="Freezable"/> cloning
    /// and frozen along with the behavior, while the registration state belongs to the single attached element.
    /// </remarks>
    protected bool IsRegisteredInViewModel { get; private set; }

    // Loaded/Unloaded arrive as a batch and can come in any order, so an Unloaded without
    // a preceding Loaded is a spurious notification and must not unregister anything.
    private bool IsLoaded { get; set; }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the DataContext of the associated object changes.
    /// Registers or unregisters the service in the ViewModel as needed.
    /// </summary>
    /// <param name="oldDataContext">The old DataContext value.</param>
    /// <param name="newDataContext">The new DataContext value.</param>
    /// <remarks>
    /// Overrides must call the base implementation: it moves the registration to the new ViewModel.
    /// </remarks>
    protected virtual void OnDataContextChanged(object? oldDataContext, object? newDataContext)
    {
        if (!ShouldRegisterInViewModel)
        {
            return;
        }
        if (oldDataContext is IServiceContainerProvider oldViewModel)
        {
            UnregisterFromViewModel(oldViewModel.Services);
        }
        if (newDataContext is IServiceContainerProvider newViewModel)
        {
            RegisterInViewModel(newViewModel.Services);
        }
    }

    /// <summary>
    /// Called when the associated element is loaded: registers the service again.
    /// </summary>
    /// <param name="sender">The associated element.</param>
    /// <param name="e">The event data.</param>
    private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            return;
        }
        IsLoaded = true;

        if (DataContext is IServiceContainerProvider viewModel)
        {
            RegisterInViewModel(viewModel.Services);
        }
    }

    /// <summary>
    /// Called when the associated element is unloaded: unregisters the service.
    /// A notification that comes before the first <c>Loaded</c> is ignored.
    /// </summary>
    /// <param name="sender">The associated element.</param>
    /// <param name="e">The event data.</param>
    private void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }
        IsLoaded = false;

        if (DataContext is IServiceContainerProvider viewModel)
        {
            UnregisterFromViewModel(viewModel.Services);
        }
    }

    /// <summary>
    /// Called when the <see cref="Name"/> property changes.
    /// Registers the service in the ViewModel under the new name.
    /// </summary>
    /// <param name="oldName">The old Name value.</param>
    /// <param name="newName">The new Name value.</param>
    /// <remarks>
    /// Overrides must call the base implementation: it moves the registration to the new name.
    /// </remarks>
    protected virtual void OnNameChanged(string? oldName, string? newName)
    {
#if DEBUG
        DebugName = newName;
#endif
        if (!ShouldRegisterInViewModel)
        {
            return;
        }

        if (string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            return;
        }
        if (DataContext is IServiceContainerProvider viewModel)
        {
            UnregisterFromViewModel(viewModel.Services);
            RegisterInViewModel(viewModel.Services);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Registers the service in the specified container of the ViewModel once: the state is tracked locally
    /// because the registration happens from several places (the DataContext change, the Loaded event,
    /// the Name change).
    /// </summary>
    /// <param name="services">The container of the ViewModel the service belongs to.</param>
    /// <remarks>
    /// An existing registration of the same type and name is replaced, not rejected: a view is re-created
    /// (a template change, a theme switch) while its ViewModel stays alive, and WPF attaches the new content
    /// before detaching the old one, so the previous instance is still registered at this moment.
    /// </remarks>
    private void RegisterInViewModel(IServiceContainer services)
    {
        if (IsRegisteredInViewModel)
        {
            return;
        }
        services.RegisterService(GetType(), this, Name, throwIfExists: false);
        IsRegisteredInViewModel = true;
    }

    /// <summary>
    /// Unregisters the service from the specified container of the ViewModel, if it is registered there.
    /// </summary>
    /// <param name="services">The container of the ViewModel the service belongs to.</param>
    private void UnregisterFromViewModel(IServiceContainer services)
    {
        if (!IsRegisteredInViewModel)
        {
            return;
        }
        services.UnregisterService(this);
        IsRegisteredInViewModel = false;
    }

    /// <summary>
    /// Called when the behavior is attached to an AssociatedObject.
    /// Binds the service to the DataContext of the associated FrameworkElement and, when
    /// <see cref="ShouldUnregisterOnUnloaded"/> is enabled, subscribes to its <c>Loaded</c>/<c>Unloaded</c> events.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (!ShouldRegisterInViewModel)
        {
            return;
        }
        BindingOperations.SetBinding(this, DataContextProperty, new Binding()
        {
            Source = AssociatedObject,
            Path = new PropertyPath(FrameworkElement.DataContextProperty),
            Mode = BindingMode.OneWay
        });

        if (!ShouldUnregisterOnUnloaded)
        {
            return;
        }

        AssociatedObject!.Loaded += OnAssociatedObjectLoaded;
        AssociatedObject.Unloaded += OnAssociatedObjectUnloaded;
    }

    /// <summary>
    /// Called before the behavior is detached from an AssociatedObject.
    /// Unregisters the service and cleans up the binding to the DataContext of the associated FrameworkElement.
    /// </summary>
    /// <remarks>
    /// The unregistration is explicit even though clearing the binding reverts the DataContext to null and
    /// triggers <see cref="OnDataContextChanged"/>: that path depends on the revert actually changing the value
    /// and on an override calling the base method, while a detached behavior must not stay registered at all.
    /// </remarks>
    protected override void OnDetaching()
    {
        if (ShouldRegisterInViewModel && DataContext is IServiceContainerProvider viewModel)
        {
            UnregisterFromViewModel(viewModel.Services);
        }

        AssociatedObject!.Loaded -= OnAssociatedObjectLoaded;
        AssociatedObject.Unloaded -= OnAssociatedObjectUnloaded;

        BindingOperations.ClearBinding(this, DataContextProperty);

        base.OnDetaching();
    }

    #endregion
}