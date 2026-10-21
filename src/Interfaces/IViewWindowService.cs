using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// A service that creates and shows windows for view models, fully managing the view resolution,
/// initialization, and parent-child relationship lifecycle.
/// </summary>
public interface IViewWindowService : INamedService
{
    /// <summary>
    /// Creates and shows the view in a non-modal window asynchronously based on the specified parameters.
    /// </summary>
    /// <param name="viewName">The name of the view to get or create.</param>
    /// <param name="viewModel">The ViewModel associated with the View.</param>
    /// <param name="parentViewModel">The parent ViewModel.</param>
    /// <param name="parameter">The ViewModel initialization parameter.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the initialization process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ShowAsync(string? viewName, object? viewModel, object? parentViewModel,
        object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and shows the view in a modal window asynchronously based on the specified parameters.
    /// </summary>
    /// <param name="viewName">The name of the view to get or create.</param>
    /// <param name="viewModel">The ViewModel associated with the View.</param>
    /// <param name="parentViewModel">The parent ViewModel.</param>
    /// <param name="parameter">The ViewModel initialization parameter.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the initialization process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask<bool?> ShowDialogAsync(string? viewName, object? viewModel, object? parentViewModel,
        object? parameter, CancellationToken cancellationToken = default);
}
