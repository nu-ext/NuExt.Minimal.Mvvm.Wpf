#define TRACE_LIFETIME_
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Base class for window ViewModels, providing window lifecycle management,
/// state, services, and commands for interacting with the window.
/// </summary>
[GenerateCommands]
public partial class WindowViewModelBase : ControlViewModelBase, IWindowViewModel
{
    private readonly CancellationTokenSource _cts = new();
    private bool _isClosing;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowViewModelBase"/> class.
    /// </summary>
    protected WindowViewModelBase() : this(fallbackServices: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowViewModelBase"/> class
    /// with an optional fallback service container.
    /// </summary>
    /// <param name="fallbackServices">
    /// The fallback service container used when local service resolution fails;
    /// or <see langword="null"/> if no fallback container is available.
    /// </param>
    protected WindowViewModelBase(IServiceContainer? fallbackServices)
        : base(fallbackServices)
    {
        if (ViewModelHelper.IsInDesignMode)
        {
            return;
        }

        PropertyChanged += OnPropertyChanged;
    }

    // Properties

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    [Notify(CallbackName = nameof(OnTitleChanged))]
    public partial string Title { get; set; }

    // Services

    /// <summary>
    /// Gets the <see cref="IOpenWindowsService"/> responsible for managing open windows.
    /// </summary>
    private IOpenWindowsService? OpenWindowsService => field ??= GetService<IOpenWindowsService>();

    /// <summary>
    /// Gets the <see cref="IWindowService"/> responsible for managing the current window.
    /// </summary>
    private IWindowService? WindowService => field ??= GetService<IWindowService>();

    // Methods

    /// <summary>
    /// Occurs when the <see cref="Title"/> property changes, allowing derived classes to respond to title updates.
    /// </summary>
    protected virtual void OnTitleChanged()
    {
        DisplayName ??= Title;
    }

    /// <inheritdoc/>
    public virtual ValueTask<bool> CanCloseAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellationToken) : ValueTask.FromResult(true);
    }

    /// <inheritdoc/>
    public ValueTask CloseAsync(bool force)
    {
        if (IsDisposingOrDisposed)
        {
            return ValueTask.CompletedTask;
        }

        if (CheckAccess())
        {
            return CloseAsyncCore(force);
        }
        return new ValueTask(InvokeAsync(async () => await CloseAsyncCore(force).ConfigureAwait(false)));
    }

    private async ValueTask CloseAsyncCore(bool force)
    {
        Debug.Assert(CheckAccess());
        Debug.Assert(IsDisposingOrDisposed == false);

        VerifyAccess();
        CheckDisposingOrDisposed();

        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        try
        {

            if (force)
            {
#if NET8_0_OR_GREATER
                await _cts.CancelAsync();
#else
            _cts.Cancel();
#endif
            }
            else
            {
                try
                {
                    if (await CanCloseAsync(CancellationToken) == false)
                    {
                        return;
                    }
                }
                catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                {
                    //do nothing and return
                    return;
                }
            }

            await DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnError(ex);
            if (force)
            {
                throw;
            }
        }
        finally
        {
            _isClosing = false;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsDisposed) && IsDisposed)
        {
#if TRACE_LIFETIME
            Trace.WriteLine($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}) is closing");
#endif
            Close();
#if TRACE_LIFETIME
            Trace.WriteLine($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}) is closed");
#endif
            _cts.Dispose();
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask DisposeAsyncCore()
    {
        Debug.Assert(CheckAccess());
        VerifyAccess();
#if NET8_0_OR_GREATER
        await _cts.CancelAsync();
#else
        _cts.Cancel();
#endif
        Hide();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc/>
    public void Hide()
    {
        CheckDisposed();

        Invoke(GetWindowService().Hide);
    }

    /// <inheritdoc/>
    public void Show()
    {
        CheckDisposed();

        Invoke(GetWindowService().Show);
    }

    /// <summary>
    /// Gets the <see cref="IWindowService"/> responsible for managing the current window.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="IWindowService"/> is not registered.</exception>
    protected virtual IWindowService GetWindowService()
    {
        var windowService = WindowService;
        Debug.Assert(windowService != null, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}): IWindowService is not registered.");
        _ = windowService ?? throw new InvalidOperationException($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}): IWindowService is not registered.");
        return windowService;
    }
}