using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Base class for ViewModels associated with UI controls, providing control lifecycle,
/// thread synchronization, asynchronous disposal, and command management support.
/// </summary>
public partial class ControlViewModelBase : CommandViewModelBase, IControlViewModel
{
#if NET9_0_OR_GREATER
    private enum States
    {
        NotDisposed,// default value of _state
        Disposing,
        Disposed
    }

    private volatile States _state;
#else
    private static class States
    {
        public const int NotDisposed = 0;// default value of _state
        public const int Disposing = 1;
        public const int Disposed = 2;
    }

    private volatile int _state;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlViewModelBase"/> class.
    /// </summary>
    protected ControlViewModelBase() : this(fallbackServices: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlViewModelBase"/> class
    /// with an optional fallback service container.
    /// </summary>
    /// <param name="fallbackServices">
    /// The fallback service container used when local and parent service resolution fails;
    /// or <see langword="null"/> if no fallback container is available.
    /// </param>
    protected ControlViewModelBase(IServiceContainer? fallbackServices) : base(fallbackServices)
    {
        if (ViewModelHelper.IsInDesignMode)
        {
            return;
        }
        Lifetime.AddBracket(() => Disposing += OnDisposing, () => Disposing -= OnDisposing);
        Lifetime.AddBracket(() => PropertyChanged += OnPropertyChanged, () => PropertyChanged -= OnPropertyChanged);
        Lifetime.AddBracket(CreateCommands, NullifyCommands);//operation after WaitAsyncCommands
        Lifetime.AddAsync(() => WaitAsyncCommands());
    }

    #region Properties

    /// <summary>
    /// Gets or sets the display name of the ViewModel, primarily used for debugging purposes.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets a value indicating whether the ViewModel has been disposed.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe for reading.
    /// </remarks>
    public bool IsDisposed => _state == States.Disposed;

    /// <summary>
    /// Gets a value indicating whether the ViewModel is currently disposing.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe for reading.
    /// </remarks>
    public bool IsDisposing => _state == States.Disposing;

    /// <summary>
    /// Gets a value indicating whether the ViewModel is currently disposing or has been disposed.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe for reading.
    /// </remarks>
    protected bool IsDisposingOrDisposed => _state != States.NotDisposed;

    /// <summary>
    /// Gets a value indicating whether the ViewModel is usable.
    /// </summary>
    /// <remarks>
    /// The ViewModel is considered usable if it has been initialized and 
    /// is neither in the process of being disposed nor already disposed.
    /// This property ensures that the ViewModel is in a valid state for operations.
    /// </remarks>
    public bool IsUsable => IsInitialized && !IsDisposingOrDisposed;

    /// <summary>
    /// Gets the contract for managing the asynchronous lifecycle of resources and actions.
    /// </summary>
    protected AsyncLifetime Lifetime { get; } = new AsyncLifetime() { ContinueOnCapturedContext = true }
#if DEBUG
        .SetDebugInfo()
#endif
        ;

    #endregion

    #region Services

    /// <summary>
    /// Gets the <see cref="IDispatcherService"/> associated with the UI thread.
    /// </summary>
    private IDispatcherService? DispatcherService => field ??= GetService<IDispatcherService>();

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the ViewModel starts the disposing process asynchronously.
    /// </summary>
    public event AsyncEventHandler? Disposing;

    #endregion

    #region Event Handlers

    private ValueTask OnDisposing(object? sender, EventArgs e, CancellationToken cancellationToken)
    {
        CancelAsyncCommands();
        return ValueTask.CompletedTask;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(ReferenceEquals(sender, this));

        switch (e.PropertyName)
        {
            case nameof(IsInitialized):
            case nameof(IsDisposed):
            case nameof(IsDisposing):
                OnPropertyChanged(EventArgsCache.IsUsablePropertyChanged);
                break;
            case nameof(IsUsable):
                RaiseCanExecuteChanged();
                break;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Checks if the ViewModel has been disposed and throws an <see cref="ObjectDisposedException"/> if it has.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the ViewModel is already disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckDisposed()
    {
        if (IsDisposed)
        {
            var message = $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}) has been disposed.";
            Trace.WriteLine(message);
            Debug.Fail(message);
            Throw.ObjectDisposedException(this, message);
        }
    }

    /// <summary>
    /// Checks if the ViewModel has been disposed or is in the process of disposing.
    /// Throws an <see cref="ObjectDisposedException"/> if either condition is true.
    /// </summary>
    /// <remarks>
    /// This method provides a stronger guarantee than <see cref="CheckDisposed"/> by also
    /// validating that the ViewModel is not currently in the disposal process, which helps
    /// prevent race conditions during asynchronous disposal.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the ViewModel is either already disposed or currently being disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckDisposingOrDisposed()
    {
        if (IsDisposingOrDisposed)
        {
#if DEBUG
            var message = $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}) is being disposed or already disposed.";
            Trace.WriteLine(message);
            Debug.Fail(message);
#endif
            Throw.ObjectDisposedException(this);
        }
    }

    /// <inheritdoc/>
    protected override bool CanSetProperty<T>(T oldValue, T newValue, [CallerMemberName] string? propertyName = null)
    {
        return !IsDisposed && base.CanSetProperty(oldValue, newValue, propertyName);
    }

    /// <summary>
    /// Asynchronously cleans up all services created in the local service container.
    /// </summary>
    /// <returns>
    /// A value task that represents the asynchronous cleanup operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the local service container has not been created, the method completes immediately.
    /// </para>
    /// <para>
    /// Services implementing <see cref="IAsyncDisposable"/> are disposed asynchronously.
    /// Other services implementing <see cref="IDisposable"/> are disposed synchronously.
    /// Asynchronous disposal takes precedence when a service implements both interfaces.
    /// </para>
    /// <para>
    /// Service cleanup continues on the captured synchronization context.
    /// Override this method to customize service cleanup behavior.
    /// </para>
    /// </remarks>
    protected virtual ValueTask CleanupServicesAsync()
    {
        if (!IsServicesCreated)
        {
            return ValueTask.CompletedTask;
        }
        return new ValueTask(Services.CleanupAsync(CleanupServiceAsync, continueOnCapturedContext: true));

        static Task CleanupServiceAsync(object service)
        {
            if (service is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync().AsTask();
            }
            (service as IDisposable)?.Dispose();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Asynchronously disposes of the resources used by the instance.
    /// This method must be called from the UI thread (same thread where the instance was created).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Debug.Assert(CheckAccess());
        VerifyAccess();

        if (_state != States.NotDisposed)
        {
            // Already disposing or disposed
            return;
        }
        _state = States.Disposing;

        OnPropertyChanged(EventArgsCache.IsDisposingPropertyChanged);
        try
        {
            ValidateDisposingState();
            if (Disposing is { } disposing)
            {
                await disposing.InvokeAsync(this, EventArgs.Empty, continueOnCapturedContext: true);
            }
            await DisposeAsyncCore();
            await CleanupServicesAsync();
            ValidateFinalState();
            if (Disposing != null)
            {
                var message = $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}): {nameof(Disposing)} is not null";
                Trace.WriteLine(message);
                Debug.Fail(message);
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}):{Environment.NewLine}{ex.Message}";
            Trace.WriteLine(errorMessage);
            Debug.Fail(errorMessage);
            throw;
        }
        finally
        {
            Disposing = null;
            _state = States.Disposed;
            GC.SuppressFinalize(this);
            RaisePropertiesChangedAndClearHandlers(EventArgsCache.IsDisposingPropertyChanged, EventArgsCache.IsDisposedPropertyChanged);
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting managed resources asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return Lifetime.DisposeAsync();
    }

    /// <summary>
    /// When overridden in a derived class, asynchronously performs the uninitialization logic for the ViewModel.
    /// This method always calls <see cref="DisposeAsync"/> to ensure proper resource cleanup,
    /// regardless of the cancellation state.
    /// </summary>
    protected override Task UninitializeAsyncCore(CancellationToken cancellationToken)
    {
        return InvokeAsync(() => DisposeAsync().AsTask());
    }

    /// <summary>
    /// Handles errors that occur within the ViewModel, providing a mechanism to display error messages.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="callerName">The name of the calling method (automatically provided).</param>
    protected virtual void OnError(Exception ex, [CallerMemberName] string? callerName = null)
    {
        Trace.WriteLine($"An error has occurred in {callerName}:{Environment.NewLine}{ex.Message}");
    }

    /// <summary>
    /// Gets the <see cref="IDispatcherService"/> associated with the UI thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="IDispatcherService"/> is not registered.</exception>
    protected virtual IDispatcherService GetDispatcherService()
    {
        var dispatcherService = DispatcherService;
        if (dispatcherService is null)
        {
            var msg = $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}): IDispatcherService is not registered.";
            Trace.WriteLine(msg);
            Debug.Fail(msg);
            throw new InvalidOperationException(msg);
        }

        return dispatcherService;
    }

    /// <summary>
    /// Gets the Window associated with the <see cref="ControlViewModelBase"/>.
    /// </summary>
    /// <returns><see cref="Window"/> or <see langword="null"/>.</returns>
    protected virtual Window? GetWindow()
    {
        return GetService<WindowService>()?.Window;
    }

    ///<inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAccess()
    {
        return GetDispatcherService().CheckAccess();
    }

    ///<inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            ThrowInvalidThreadAccess();
        }
        return;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowInvalidThreadAccess()
        {
            throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
        }
    }

    #endregion
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs IsDisposedPropertyChanged = new(nameof(ControlViewModelBase.IsDisposed));
    internal static readonly PropertyChangedEventArgs IsDisposingPropertyChanged = new(nameof(ControlViewModelBase.IsDisposing));
    internal static readonly PropertyChangedEventArgs IsUsablePropertyChanged = new(nameof(ControlViewModelBase.IsUsable));
}