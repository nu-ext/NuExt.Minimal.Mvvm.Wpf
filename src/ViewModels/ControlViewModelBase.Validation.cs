using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm.Wpf;

partial class ControlViewModelBase
{
    [Conditional("DEBUG")]
    private void ValidateDisposingState()
    {
        var typeName = GetType().FullName;
        var displayName = DisplayName ?? "Unnamed";

        Debug.Assert(CheckAccess());

        var asyncCommands = GetAllAsyncCommands();
        Debug.Assert(asyncCommands.Count == 0 ||
                     asyncCommands.All(pair => pair.Command.IsExecuting == false),
            $"{typeName} ({displayName}) ({RuntimeHelpers.GetHashCode(this):X8}) has unexpected state of async commands.");
    }

    [Conditional("DEBUG")]
    private void ValidateFinalState()
    {
        var typeName = GetType().FullName;
        var displayName = DisplayName ?? "Unnamed";

        var commands = GetAllCommands();
        Debug.Assert(commands.All(c => c.Command is null), $"{typeName} ({displayName}) ({RuntimeHelpers.GetHashCode(this):X8}) has not nullified commands.");
    }
}
