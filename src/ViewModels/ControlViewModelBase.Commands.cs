#define TRACE_EVENTS_
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

partial class ControlViewModelBase
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

    /// <inheritdoc/>
    /// <remarks>
    /// When inheriting from this class, you should override this method to create commands defined in your ViewModel.
    /// Ensure to call the base implementation to include commands from the base class.
    /// </remarks>
    protected override void CreateCommands()
    {
        LoadedCommand = new RelayCommand(OnLoaded);
        UnloadedCommand = new RelayCommand(OnUnloaded);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Thrown when <paramref name="callerName"/> is null or empty, or command not found.</exception>
    /// <remarks>
    /// This method uses the <see cref="CallerMemberNameAttribute"/> to determine the name of the calling method automatically,
    /// making it easier to retrieve the appropriate command without explicitly passing the method name.
    /// When inheriting from this class, you should override this method to handle commands defined in your ViewModel.
    /// Ensure to call the base implementation to handle commands from the base class.
    /// This method is intended to be called from within a command method to obtain the associated command instance.
    /// </remarks>
    protected override ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(callerName), $"{nameof(callerName)} is null or empty");
        ArgumentException.ThrowIfNullOrEmpty(callerName);
        return callerName switch
        {
            nameof(OnLoaded) => LoadedCommand,
            nameof(OnUnloaded) => UnloadedCommand,
            _ => throw new ArgumentException($"Command for method '{callerName}' was not found.", nameof(callerName))
        };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// When inheriting from this class, you should override this method to add commands that are defined in your ViewModel.
    /// Ensure to call the base implementation to include commands from the base class.
    /// Note: Do not reassign the builder parameter within this method. Instead, use the provided builder to add commands.
    /// </remarks>
    protected override void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        builder.Append((nameof(LoadedCommand), LoadedCommand));
        builder.Append((nameof(UnloadedCommand), UnloadedCommand));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// When inheriting from this class, you should override this method to nullify commands defined in your ViewModel.
    /// Ensure to call the base implementation to nullify commands from the base class.
    /// </remarks>
    protected override void NullifyCommands()
    {
        LoadedCommand = null;
        UnloadedCommand = null;
    }

    #endregion
}