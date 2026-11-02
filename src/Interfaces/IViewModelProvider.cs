namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides access to the currently associated view model.
/// </summary>
public interface IViewModelProvider
{
    /// <summary>
    /// Gets the currently associated view model.
    /// </summary>
    object? ViewModel { get; }
}