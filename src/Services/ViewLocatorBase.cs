using Minimal.Mvvm.Wpf.Controls;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides a base implementation for locating and creating Views.
/// </summary>
public abstract class ViewLocatorBase
{
    #region Methods

    /// <summary>
    /// Gets or creates a view asynchronously based on the specified view name.
    /// </summary>
    /// <param name="viewName">The name of the view to get or create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created view object.</returns>
    public virtual ValueTask<object> GetOrCreateViewAsync(string? viewName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<object>(cancellationToken);
        }

        var viewType = GetViewType(viewName);
        if (viewType == null)
        {
            return ValueTask.FromResult(CreateFallbackView(viewName));
        }

        try
        {
            return ValueTask.FromResult(CreateViewFromType(viewType, viewName));
        }
        catch (Exception ex) when (ex is TargetInvocationException or MissingMethodException or MemberAccessException)
        {
            return ViewModelHelper.IsInDesignMode 
                ? ValueTask.FromResult(CreateFallbackView(viewName)) 
                : throw new InvalidOperationException($"Failed to create view \"{viewName}\" of type {viewType.FullName}.", ex);
        }
    }

    /// <summary>
    /// Gets the type of the view based on the specified view name.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The type of the view.</returns>
    protected abstract Type? GetViewType(string? viewName);

    /// <summary>
    /// Creates a view from the specified view type.
    /// </summary>
    /// <param name="viewType">The type of the view to create.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The created view object.</returns>
    protected virtual object CreateViewFromType(Type viewType, string? viewName)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        var ctor = viewType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, [], null);
        if (ctor != null)
        {
            return ctor.Invoke(null);
        }
        return Activator.CreateInstance(viewType, nonPublic: true)!;
    }

    /// <summary>
    /// Creates a fallback view when the specified view type cannot be found.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The created fallback view object.</returns>
    protected virtual object CreateFallbackView(string? viewName)
    {
        string errorMessage;
        if (string.IsNullOrEmpty(viewName)) errorMessage = "ViewType is not specified.";
        else if (ViewModelHelper.IsInDesignMode) errorMessage = $"[{viewName}]";
        else errorMessage = $"\"{viewName}\" type not found.";
        return new FallbackView() { Text = errorMessage };
    }

    #endregion
}
