using Minimal.Mvvm;
using MovieWpfApp.Models;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels;

partial class MoviesViewModel
{
    #region Properties

    [Notify(Setter = AccessModifier.Private)]
    private MoviesSettings? _settings;

    #endregion

    #region Methods

    private void CreateSettings()
    {
        if (Settings != null)
        {
            return;
        }
        Settings = new MoviesSettings();
        Lifetime.AddBracket(Settings.Initialize, Settings.Uninitialize);
        Lifetime.AddBracket(LoadSettings, SaveSettings);
    }

    private void LoadSettings()
    {
        Debug.Assert(IsInitialized, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({GetHashCode()}) is not initialized.");
        Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
        Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
        using (Settings!.SuspendChangeTracking())
        {
            SettingsService!.LoadSettings(Settings);
        }
    }

    private void SaveSettings()
    {
        Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
        Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
        if (Settings!.IsChanged && SettingsService!.SaveSettings(Settings))
        {
            Settings.AcceptChanges();
        }
    }

    #endregion
}
