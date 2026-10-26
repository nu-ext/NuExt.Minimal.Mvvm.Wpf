using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Base class for ViewModels that provides command creation, lookup, enumeration,
/// cancellation, completion tracking, and cleanup infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Commands are created synchronously during ViewModel initialization and nullified
/// during uninitialization.
/// </para>
/// <para>
/// Derived classes must implement the command infrastructure methods manually or use
/// source-generated implementations.
/// </para>
/// </remarks>
public abstract class CommandViewModelBase : ViewModelBase, ICommandViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandViewModelBase"/> class.
    /// </summary>
    protected CommandViewModelBase() : this(fallbackServices: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandViewModelBase"/> class
    /// with an optional fallback service container.
    /// </summary>
    /// <param name="fallbackServices">
    /// The fallback service container used when local and parent service resolution fails;
    /// or <see langword="null"/> if no fallback container is available.
    /// </param>
    protected CommandViewModelBase(IServiceContainer? fallbackServices) : base(fallbackServices)
    {
    }

    /// <inheritdoc/>
    protected override Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        // Intentionally does not call the base implementation to keep command
        // creation synchronous and preserve the caller's synchronization context.
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

        CreateCommands();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task UninitializeAsyncCore(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        NullifyCommands();
        return base.UninitializeAsyncCore(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method calls the <see cref="IAsyncCommand.Cancel"/> on each one.
    /// </remarks>
    public void CancelAsyncCommands()
    {
        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        try
        {
            GetAllCommands(ref commandBuilder);
            foreach (var (_, command) in commandBuilder.AsSpan())
            {
                if (command is IAsyncCommand asyncCommand)
                {
                    asyncCommand.Cancel();
                }
            }
        }
        finally
        {
            commandBuilder.Dispose();
        }
    }

    /// <summary>
    /// Intended for creating commands as needed.
    /// </summary>
    protected abstract void CreateCommands();

    /// <summary>
    /// Retrieves the command associated with the calling method.
    /// </summary>
    /// <param name="callerName">The name of the calling method. This parameter is automatically provided by the compiler.</param>
    /// <returns>
    /// The <see cref="ICommand"/> associated with the calling method, or <see langword="null"/>
    /// if the command has not been created or has already been released.
    /// </returns>
    protected abstract ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null);

    /// <summary>
    /// Retrieves all commands recursively from the current ViewModel and its base classes, adding them to the provided list.
    /// </summary>
    /// <param name="builder">A list of tuples containing property names and their corresponding <see cref="ICommand"/> instances.</param>
    protected abstract void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder);

    /// <summary>
    /// Nullifies all commands in the ViewModel.
    /// </summary>
    protected abstract void NullifyCommands();

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

    /// <inheritdoc/>
    public void RaiseCanExecuteChanged()
    {
        scoped var commandBuilder = new ValueListBuilder<(string PropertyName, ICommand? Command)>([default, default, default, default, default, default, default, default]);
        try
        {
            GetAllCommands(ref commandBuilder);
            foreach (var (_, command) in commandBuilder.AsSpan())
            {
                command?.RaiseCanExecuteChanged();
            }
        }
        finally
        {
            commandBuilder.Dispose();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method waits for all commands stored in the internal collection to complete execution.
    /// If the collection is empty, the method returns immediately.
    /// The operation can be canceled using the provided <paramref name="cancellationToken"/>.
    /// </remarks>
    public async ValueTask WaitAsyncCommands(CancellationToken cancellationToken = default)
    {
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
}