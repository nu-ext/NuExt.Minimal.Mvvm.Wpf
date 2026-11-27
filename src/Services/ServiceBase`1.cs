using Minimal.Behaviors.Wpf;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents a base class for services that can be attached to a FrameworkElement.
/// Provides automatic registration with the ViewModel's service container when the DataContext changes.
/// </summary>
/// <typeparam name="T">The type of the FrameworkElement to which this service is attached.</typeparam>
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
    /// Gets or sets a value indicating whether the service should unregister itself in the ViewModel
    /// while the attached element is unloaded and register again when it is loaded back.
    /// Default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enable it when the element may be discarded while the ViewModel stays alive: a template change
    /// (a theme switch, for example) discards it, and the discarded element never detaches its behaviors.
    /// </para>
    /// <para>
    /// This property should be set during initialization (typically in XAML) and not modified afterward.
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

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the DataContext of the associated object changes.
    /// Registers or unregisters the service in the ViewModel as needed.
    /// </summary>
    /// <param name="oldDataContext">The old DataContext value.</param>
    /// <param name="newDataContext">The new DataContext value.</param>
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
    /// Called when the associated element is loaded: registers the service back.
    /// </summary>
    /// <param name="sender">The associated element.</param>
    /// <param name="e">The event data.</param>
    private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IServiceContainerProvider viewModel)
        {
            RegisterInViewModel(viewModel.Services);
        }
    }

    /// <summary>
    /// Called when the associated element is unloaded: unregisters the service.
    /// </summary>
    /// <param name="sender">The associated element.</param>
    /// <param name="e">The event data.</param>
    private void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IServiceContainerProvider viewModel)
        {
            UnregisterFromViewModel(viewModel.Services);
        }
    }

    /// <summary>
    /// Called when the <see cref="Name"/> property changes.
    /// Registers or unregisters the service in the ViewModel as needed.
    /// </summary>
    /// <param name="oldName">The old Name value.</param>
    /// <param name="newName">The new Name value.</param>
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
    /// because the registration happens from several places (the DataContext change, the Loaded event).
    /// </summary>
    /// <param name="services">The container of the ViewModel the service belongs to.</param>
    private void RegisterInViewModel(IServiceContainer services)
    {
        if (IsRegisteredInViewModel)
        {
            return;
        }
        services.RegisterService(GetType(), this, Name, throwIfExists: true);
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
        // A false result means the local state and the container are out of sync:
        // the registration was removed by someone else.
        bool removed = services.UnregisterService(this);
        Debug.Assert(removed, "The service is registered, but the container has no such registration.");
        IsRegisteredInViewModel = false;
    }

    /// <summary>
    /// Called when the behavior is attached to an AssociatedObject.
    /// Sets up a binding to the DataContext of the associated FrameworkElement.
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
    /// Clean up a binding to the DataContext of the associated FrameworkElement.
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject!.Loaded -= OnAssociatedObjectLoaded;
        AssociatedObject.Unloaded -= OnAssociatedObjectUnloaded;

        BindingOperations.ClearBinding(this, DataContextProperty);

        base.OnDetaching();
    }

    #endregion
}