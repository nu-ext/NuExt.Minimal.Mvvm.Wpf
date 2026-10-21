using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides services for managing open window view models within the application.
/// This service maintains a list of currently open window view models and offers functionality to register,
/// unregister, and force-close all registered windows asynchronously. It ensures thread safety.
/// </summary>
public sealed class OpenWindowsService : AsyncDisposable, IOpenWindowsService
{
    private readonly List<IWindowViewModel> _viewModels = [];
    private readonly AsyncLock _lock = new AsyncLock()
#if DEBUG
        .SetDebugInfo()
#endif
        ;

    public OpenWindowsService()
    {
        ContinueOnCapturedContext = true;
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            CheckDisposingOrDisposed();

            _lock.Enter();
            try
            {
                return _viewModels.Count;
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IWindowViewModel> ViewModels
    {
        get
        {
            CheckDisposingOrDisposed();

            _lock.Enter();
            try
            {
                return [.. _viewModels];
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <summary>
    /// Registers a window view model with the service.
    /// </summary>
    /// <param name="viewModel">The window view model to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the service is disposed.</exception>
    public void Register(IWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        CheckDisposingOrDisposed();

        bool added = false;
        _lock.Enter();
        try
        {
            CheckDisposingOrDisposed();

            if (!_viewModels.Contains(viewModel))
            {
                _viewModels.Add(viewModel);
                added = true;
            }
        }
        finally
        {
            _lock.Exit();
        }

        if (added)
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        }
    }

    /// <summary>
    /// Unregisters a window view model from the service.
    /// </summary>
    /// <param name="viewModel">The window view model to unregister.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the service is disposed.</exception>
    public void Unregister(IWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (IsDisposingOrDisposed)//to prevent Unregister call in DisposeAsyncCore->CloseAsync
        {
            return;
        }

        bool removed = false;
        _lock.Enter();
        try
        {
            CheckDisposingOrDisposed();

            removed = _viewModels.Remove(viewModel);
            if (!removed)
            {
                string message = $"{nameof(WindowViewModel)} was not found in the registry: {viewModel}";
                Trace.WriteLine(message);
                Debug.Fail(message);
            }
        }
        finally
        {
            _lock.Exit();
        }
        if (removed)
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        }
    }

    /// <summary>
    /// Asynchronously disposes the service, force-closing all registered windows.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    /// <exception cref="AggregateException">Thrown when one or more windows fail to close.</exception>
    protected override async ValueTask DisposeAsyncCore()
    {
        Debug.Assert(ContinueOnCapturedContext);
        Debug.Assert(CheckAccess());
        VerifyAccess();

        bool removed = false;
        await _lock.EnterAsync();
        try
        {
            List<Exception>? exceptions = null;
            for (int i = _viewModels.Count - 1; i >= 0; i--)
            {
                try
                {
                    await _viewModels[i].CloseAsync(force: true);
                    removed = true;
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
            //Debug.Assert(_viewModels.Count == 0);
            _viewModels.Clear();
        }
        finally
        {
            _lock.Exit();
        }
        _lock.Dispose();

        if (removed)
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        }
    }
}