using Minimal.Mvvm;
using MovieWpfApp.Models;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels;

internal partial class MainWindowViewModel
{
    #region Properties

    [Notify(Setter = AccessModifier.Private)]
    private MainWindowSettings? _settings;

    #endregion

    #region Methods

    private void CreateSettings()
    {
        if (Settings != null)
        {
            return;
        }
        Settings = new MainWindowSettings();
        Lifetime.AddBracket(Settings.Initialize, Settings.Uninitialize);
        Lifetime.AddBracket(LoadSettings, SaveSettings);
    }

    private void LoadSettings()
    {
        Debug.Assert(IsInitialized, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({GetHashCode()}) is not initialized.");
        Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
        using (Settings!.SuspendDirty())
        {
            SettingsService.LoadSettings(Settings);
        }
    }

    private void SaveSettings()
    {
        Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
        if (Settings!.IsDirty && SettingsService.SaveSettings(Settings))
        {
            Settings.ResetDirty();
        }
    }

    #endregion

}
