using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Defines a ViewModel that manages synchronous and asynchronous commands.
/// </summary>
public interface ICommandViewModel
{
    /// <summary>
    /// Cancels all executing asynchronous commands.
    /// </summary>
    void CancelAsyncCommands();

    /// <summary>
    /// Raises the <see cref="ICommand.CanExecuteChanged"/> event for all commands defined in the ViewModel.
    /// </summary>
    void RaiseCanExecuteChanged();

    /// <summary>
    /// Waits asynchronously until all asynchronous commands in the ViewModel have finished executing.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    ValueTask WaitAsyncCommands(CancellationToken cancellationToken = default);
}