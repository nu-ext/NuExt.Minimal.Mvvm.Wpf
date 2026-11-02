using System;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides extension methods for the <see cref="IViewModelProvider"/>.
/// </summary>
public static class ViewModelProviderExtensions
{
    /// <summary>
    /// Gets the currently associated view model.
    /// </summary>
    /// <typeparam name="TViewModel">The expected type of the view model.</typeparam>
    /// <param name="provider">The view model provider.</param>
    /// <returns>The associated view model, or <see langword="null"/> if it is not of the specified type.</returns>
    public static TViewModel? GetViewModel<TViewModel>(this IViewModelProvider provider)
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.ViewModel as TViewModel;
    }
}