using System.IO;
using Minimal.Mvvm.Wpf;

namespace WpfMultiThreadedApp.Services;

public sealed class EnvironmentService(string baseDirectory, params string[] args)
    : EnvironmentServiceBase(baseDirectory, Path.Combine(baseDirectory, "AppData"), args);