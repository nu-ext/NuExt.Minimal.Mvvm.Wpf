using System;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

partial class ControlViewModelBase
{
    #region Methods

    /// <inheritdoc/>
    public Task<object?> BeginInvoke(Delegate method, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(method);

        return GetDispatcherService().BeginInvoke(method, args);
    }

    /// <inheritdoc/>
    public object? Invoke(Delegate method, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!CheckAccess())
        {
            return GetDispatcherService().Invoke(method, args);
        }

        return method.Call(args);
    }

    /// <inheritdoc/>
    public void Invoke(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!CheckAccess())
        {
            GetDispatcherService().Invoke(callback);
            return;
        }

        callback();
    }

    /// <inheritdoc/>
    public TResult Invoke<TResult>(Func<TResult> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!CheckAccess())
        {
            return GetDispatcherService().Invoke(callback);
        }

        return callback();
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return GetDispatcherService().InvokeAsync(callback);
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return GetDispatcherService().InvokeAsync(callback);
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return GetDispatcherService().InvokeAsync(callback);
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return GetDispatcherService().InvokeAsync(callback);
    }

    #endregion
}
