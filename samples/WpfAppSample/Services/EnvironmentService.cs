using System.IO;
using System.Text.Json;
using Minimal.Mvvm.Wpf;

namespace MovieWpfApp.Services;

public sealed class EnvironmentService(string baseDirectory, params string[] args)
    : EnvironmentServiceBase(baseDirectory, Path.Combine(baseDirectory, "AppData"), args);

public static class EnvironmentServiceExtensions
{
    public static void LoadLocalization(this EnvironmentService environmentService, Type type, string culture)
    {
        var langFilePath = Path.Combine(environmentService.ConfigDirectory, $"local.{culture}.json");
        //Debug.Assert(File.Exists(langFilePath), $"File doesn't exist: {langFilePath}");
        if (!File.Exists(langFilePath)) return;
        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(langFilePath));
        if (translations.IsNullOrEmpty())
        {
            return;
        }
        translations = translations.ToDictionary(pair => Minimal.Mvvm.LocalizeAttribute.StringToValidPropertyName(pair.Key), pair => pair.Value);
        var props = type.GetProperties();
        foreach (var prop in props)
        {
            //Debug.Assert(!prop.CanWrite || translations.ContainsKey(prop.Name), $"Can't find translation for {prop.Name}");
            if (prop.CanWrite && translations.TryGetValue(prop.Name, out var text))
            {
                prop.SetValue(null, text);
            }
        }
    }
}
