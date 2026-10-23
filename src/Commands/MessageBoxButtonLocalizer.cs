using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides a base class for localizing message box buttons.
/// </summary>
public abstract class MessageBoxButtonLocalizerBase
{
    /// <summary>
    /// Localizes the specified message box button.
    /// </summary>
    /// <param name="button">The message box button to localize.</param>
    /// <returns>The localized string representation of the message box button.</returns>
    public abstract string Localize(MessageBoxResult button);
}

/// <summary>
/// A default implementation of <see cref="MessageBoxButtonLocalizerBase"/> that provides 
/// basic localization for message box buttons.
/// </summary>
public class MessageBoxButtonLocalizer : MessageBoxButtonLocalizerBase
{
    private static MessageBoxButtonLocalizerBase? s_custom;

    /// <summary>
    /// Gets or sets the message box button localizer.
    /// Assigning <see langword="null"/> restores the default localizer.
    /// </summary>
    [AllowNull]
    public static MessageBoxButtonLocalizerBase Default
    {
        get => s_custom ?? field;
        set => s_custom = value;
    } = new MessageBoxButtonLocalizer();

    /// <summary>
    /// Localizes the specified message box button by returning its string representation.
    /// </summary>
    /// <param name="button">The message box button to localize.</param>
    /// <returns>The localized string representation of the message box button.</returns>
    public override string Localize(MessageBoxResult button)
    {
        return button switch
        {
            MessageBoxResult.OK => button.ToString(),
            MessageBoxResult.Cancel => button.ToString(),
            MessageBoxResult.Yes => button.ToString(),
            MessageBoxResult.No => button.ToString(),
            _ => string.Empty
        };
    }
}
