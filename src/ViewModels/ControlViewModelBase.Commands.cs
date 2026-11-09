#define TRACE_EVENTS_
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using static Minimal.Mvvm.AccessModifier;

namespace Minimal.Mvvm.Wpf;

partial class ControlViewModelBase
{
    // Command Methods

    /// <summary>
    /// Command executed when the view is loaded.
    /// </summary>
    /// <remarks>
    /// This command should be bound to the control in the view to handle the Loaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    [Command(Setter = Private)]
    private void Loaded()
    {
        OnLoaded();
    }

    /// <summary>
    /// Command executed when the view is unloaded.
    /// </summary>
    /// <remarks>
    /// This command should be bound to the control in the view to handle the Unloaded event. For example:
    /// <code>
    /// &lt;nx:EventToCommand EventName="Unloaded" Command="{Binding UnloadedCommand}" /&gt;
    /// </code>
    /// </remarks>
    [Command(Setter = Private)]
    private void Unloaded()
    {
        OnUnloaded();
    }

    // Methods

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
}