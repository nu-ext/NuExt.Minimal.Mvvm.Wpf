using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides extension methods for handling commands.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Retrieves all commands defined in the ViewModel.
    /// </summary>
    /// <param name="viewModel">ViewModel.</param>
    /// <returns>A list of tuples containing command names and their corresponding <see cref="ICommand"/> instances.</returns>
    public static IList<(string PropertyName, ICommand? Command)> GetAllCommands<T>(this T viewModel) where T : BindableBase
    {
        var commandProperties = viewModel.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(prop => typeof(ICommand).IsAssignableFrom(prop.PropertyType) && prop.CanRead);
        return commandProperties.Select(prop => (prop.Name, (ICommand?)prop.GetValue(viewModel))).ToList();
    }

    /// <summary>
    /// Raises the <see cref="ICommand.CanExecuteChanged"/> event.
    /// </summary>
    /// <param name="command">The command to raise the CanExecuteChanged event for.</param>
    /// <remarks>
    /// For commands that do not implement <see cref="IRelayCommand"/>, the method attempts to invoke a discoverable backing event handler.
    /// If no compatible handler is found, the method has no effect.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
    public static void RaiseCanExecuteChanged(this ICommand command)
    {
        Throw.IfNull(command);

        if (command is IRelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
            return;
        }

        // Fallback for non-IRelayCommand: Raise CanExecuteChanged event directly if possible
        var eventFields = command.GetType().GetAllFields(typeof(object), BindingFlags.Instance | BindingFlags.NonPublic, fi => fi.Name.Contains(nameof(ICommand.CanExecuteChanged), StringComparison.OrdinalIgnoreCase));//TODO optimize
        if (eventFields.Count == 0) return;
        if (eventFields[0].GetValue(command) is not EventHandler eventHandler) return;
        eventHandler.Invoke(command, EventArgs.Empty);
    }
}
