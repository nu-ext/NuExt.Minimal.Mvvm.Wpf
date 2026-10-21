#define TRACE_EVENTS_
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static AccessModifier;

namespace Minimal.Mvvm.Wpf;

partial class ControlViewModel
{
    #region Commands

    /// <summary>
    /// Command executed when the view is loaded.
    /// </summary>
    /// <remarks>
    /// This command should be bound to the control in the view to handle the Loaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    [Notify]
    public partial IRelayCommand? LoadedCommand { get; private set; }

    /// <summary>
    /// Command executed when the view is unloaded.
    /// </summary>
    /// <remarks>
    /// This command should be bound to the control in the view to handle the Unloaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Unloaded" Command="{Binding UnloadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    [Notify]
    public partial IRelayCommand? UnloadedCommand { get; private set; }

    #endregion

    #region Command Methods

    /// <summary>
    /// Method to be called when the view is loaded.
    /// </summary>
    /// <remarks>
    /// The LoadedCommand should be bound to the control in the view to handle the Loaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    protected virtual void OnLoaded()
    {
#if TRACE_EVENTS
        Trace.WriteLine($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8})::OnLoaded");
#endif
    }

    /// <summary>
    /// Method to be called when the view is unloaded.
    /// </summary>
    /// <remarks>
    /// The UnloadedCommand should be bound to the control in the view to handle the Unloaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Unloaded" Command="{Binding UnloadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    protected virtual void OnUnloaded()
    {
#if TRACE_EVENTS
        Trace.WriteLine($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8})::OnUnloaded");
#endif
    }

    #endregion

    #region Methods

    /// <summary>
    /// Cancels all executing asynchronous commands.
    /// </summary>
    /// <remarks>
    /// This method calls the <see cref="IAsyncCommand.Cancel"/> on each one.
    /// </remarks>
    public void CancelAsyncCommands()
    {
        CheckDisposed();

        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        GetAllCommands(ref commandBuilder);
        foreach (var (_, command) in commandBuilder.AsSpan())
        {
            if (command is IAsyncCommand asyncCommand)
            {
                asyncCommand.Cancel();
            }
        }
        commandBuilder.Dispose();
    }

    /// <summary>
    /// Intended for creating commands as needed.
    /// </summary>
    /// <remarks>
    /// When inheriting from this class, you should override this method to create commands defined in your ViewModel.
    /// Ensure to call the base implementation to include commands from the base class.
    /// </remarks>
    protected virtual void CreateCommands()
    {
        LoadedCommand = new RelayCommand(OnLoaded);
        UnloadedCommand = new RelayCommand(OnUnloaded);
    }

    /// <summary>
    /// Retrieves the command associated with the calling method.
    /// </summary>
    /// <param name="callerName">The name of the calling method. This parameter is automatically provided by the compiler.</param>
    /// <returns>
    /// The <see cref="ICommand"/> associated with the calling method, or <see langword="null"/>
    /// if the command has not been created or has already been released.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="callerName"/> is null or empty, or command not found.</exception>
    /// <remarks>
    /// This method uses the <see cref="CallerMemberNameAttribute"/> to determine the name of the calling method automatically,
    /// making it easier to retrieve the appropriate command without explicitly passing the method name.
    /// When inheriting from this class, you should override this method to handle commands defined in your ViewModel.
    /// Ensure to call the base implementation to handle commands from the base class.
    /// This method is intended to be called from within a command method to obtain the associated command instance.
    /// </remarks>
    protected virtual ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(callerName), $"{nameof(callerName)} is null or empty");
        Throw.IfNullOrEmpty(callerName);
        return callerName switch
        {
            nameof(OnLoaded) => LoadedCommand,
            nameof(OnUnloaded) => UnloadedCommand,
            _ => throw new ArgumentException($"Command for method '{callerName}' was not found.", nameof(callerName))
        };
    }

    /// <summary>
    /// Retrieves all commands recursively from the current ViewModel and its base classes, adding them to the provided list.
    /// </summary>
    /// <param name="builder">A list of tuples containing property names and their corresponding <see cref="ICommand"/> instances.</param>
    /// <remarks>
    /// When inheriting from this class, you should override this method to add commands that are defined in your ViewModel.
    /// Ensure to call the base implementation to include commands from the base class.
    /// Note: Do not reassign the builder parameter within this method. Instead, use the provided builder to add commands.
    /// </remarks>
    protected virtual void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        builder.Append((nameof(LoadedCommand), LoadedCommand));
        builder.Append((nameof(UnloadedCommand), UnloadedCommand));
    }

    /// <summary>
    /// Nullifies all commands in the ViewModel.
    /// This is useful for cleanup purposes before the ViewModel is disposed.
    /// </summary>
    /// <remarks>
    /// When inheriting from this class, you should override this method to nullify commands defined in your ViewModel.
    /// Ensure to call the base implementation to nullify commands from the base class.
    /// </remarks>
    protected virtual void NullifyCommands()
    {
        LoadedCommand = null;
        UnloadedCommand = null;
    }

    /// <summary>
    /// Retrieves all commands defined in the ViewModel.
    /// </summary>
    /// <returns>A list of tuples containing property names and their corresponding <see cref="ICommand"/> instances.</returns>
    protected IReadOnlyList<(string PropertyName, ICommand? Command)> GetAllCommands()
    {
        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        GetAllCommands(ref commandBuilder);
        return commandBuilder.ToArray();
    }

    /// <summary>
    /// Retrieves all non-null asynchronous commands defined in the ViewModel.
    /// </summary>
    /// <returns>A list of tuples containing property names and their corresponding <see cref="IAsyncCommand"/> instances.</returns>
    protected IReadOnlyList<(string PropertyName, IAsyncCommand Command)> GetAllAsyncCommands()
    {
        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        GetAllCommands(ref commandBuilder);

        scoped var asyncCommandBuilder = new ValueListBuilder<(string PropertyName, IAsyncCommand Command)>([default, default, default, default, default, default, default, default]);
        foreach (var (name, command) in commandBuilder.AsSpan())
        {
            if (command is IAsyncCommand asyncCommand)
            {
                asyncCommandBuilder.Append((name, asyncCommand));
            }
        }
        commandBuilder.Dispose();

        return asyncCommandBuilder.ToArray();
    }

    /// <summary>
    /// Raises the <see cref="ICommand.CanExecuteChanged"/> event for all commands defined in the ViewModel.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        GetAllCommands(ref commandBuilder);
        foreach (var (_, command) in commandBuilder.AsSpan())
        {
            command?.RaiseCanExecuteChanged();
        }
        commandBuilder.Dispose();
    }

    /// <summary>
    /// Waits asynchronously until all asynchronous commands in the ViewModel have finished executing.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the wait is canceled.</exception>
    /// <remarks>
    /// This method waits for all commands stored in the internal collection to complete execution.
    /// If the collection is empty, the method returns immediately.
    /// The operation can be canceled using the provided <paramref name="cancellationToken"/>.
    /// </remarks>
    public async ValueTask WaitAsyncCommands(CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        GetAllCommands(ref commandBuilder);

        scoped var taskBuilder = new ValueListBuilder<Task>([null, null, null, null, null, null, null, null]);
        foreach (var (_, command) in commandBuilder.AsSpan())
        {
            if (command is IAsyncCommand { IsExecuting: true } asyncCommand)
            {
                taskBuilder.Append(asyncCommand.WaitAsync(cancellationToken));
            }
        }
        commandBuilder.Dispose();

        if (taskBuilder.Length == 0)
        {
            taskBuilder.Dispose();
            return;
        }

#if NET9_0_OR_GREATER
        var task = Task.WhenAll(taskBuilder.AsSpan());
        taskBuilder.Dispose();
        await task.ConfigureAwait(false);
#else
        await Task.WhenAll(taskBuilder.ToArray()).ConfigureAwait(false);
#endif
    }

    #endregion
}
