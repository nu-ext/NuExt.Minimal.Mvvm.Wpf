using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WpfMultiThreadedApp.Services;
using WpfMultiThreadedApp.ViewModels;

namespace WpfMultiThreadedApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : INamedServiceProvider
{
    private readonly CancellationTokenSource _cts = new();
    private readonly AsyncLifetime _lifetime = new() { ContinueOnCapturedContext = true };

    public App()
    {
        _lifetime.Add(_cts.Cancel);
        ServiceContainer = new ServiceProvider(this);
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    #region Properties

    public ILogger? Logger { get; private set; }

    public PerformanceMonitor PerformanceMonitor { get; } = new(Process.GetCurrentProcess(), CultureInfo.InvariantCulture)
    {
        ShowManagedMemory = true,
        ShowThreads = true
    };

    public IServiceContainer ServiceContainer { get; }

    #endregion

    #region Services

    public EnvironmentService? EnvironmentService => field ??= GetService<EnvironmentService>();

    private IOpenWindowsService OpenWindowsService => field ??= GetService<IOpenWindowsService>() ?? throw new ArgumentNullException(nameof(OpenWindowsService));

    #endregion

    #region Event Handlers

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger?.Error(e.Exception, "Application Dispatcher Unhandled Exception.");
        e.Handled = true;
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Logger?.Information("Application exited with code {ExitCode}.", e.ApplicationExitCode);
        Log.CloseAndFlush();
    }

    private async void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        if (e.ReasonSessionEnding != ReasonSessionEnding.Shutdown)
        {
            return;
        }

        try
        {
            await OpenWindowsService.DisposeAsync().ConfigureAwait(false);
            Debug.Assert(OpenWindowsService.Count == 0);
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Application SessionEnding Exception.");
        }
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        Minimal.Mvvm.Wpf.Bootstrap.Initialize();

        var environmentService = new EnvironmentService(AppDomain.CurrentDomain.BaseDirectory, e.Args);
        ServiceContainer.RegisterService(environmentService);

        ConfigureLogging(environmentService);

        environmentService.LoadLocalization(typeof(Loc), CultureInfo.CurrentUICulture.IetfLanguageTag);

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        Logger?.Information("Application started.");

        _lifetime.AddAsyncDisposable(OpenWindowsService);

        var viewModel = new MainWindowViewModel()
        {
            ParentViewModel = this,
            UseMultiThreaded = true,
            UseWindowManager = true
        };
        _lifetime.AddBracket(
            () => viewModel.Disposing += OnDisposingAsync,
            () => viewModel.Disposing -= OnDisposingAsync);
        try
        {
            var window = new MainWindow(viewModel);
            await viewModel.InitializeAsync(viewModel.CancellationToken);
            window.Show();
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.Message);
            Logger?.Error(ex, "Error while initialization");
            await viewModel.DisposeAsync();
            Shutdown();
            return;
        }

        _ = Task.Run(() => PerformanceMonitor.RunAsync(_cts.Token), _cts.Token);
    }

    private async ValueTask OnDisposingAsync(object? sender, EventArgs e, CancellationToken cancellationToken)
    {
        try
        {
            await _lifetime.DisposeAsync().ConfigureAwait(false);
            Debug.Assert(OpenWindowsService.IsDisposed || OpenWindowsService.Count == 0);
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "App Disposing Exception.");
            Debug.Fail(ex.Message);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger?.Error(e.Exception, "TaskScheduler Unobserved Task Exception: {Exception}.", e.Exception.Message);
    }

    #endregion

    #region Methods

    private void ConfigureLogging(EnvironmentService environmentService)
    {
        Debug.Assert(IOUtils.NormalizedPathEquals(environmentService.BaseDirectory, Directory.GetCurrentDirectory()));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(environmentService.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var logPath = configuration["Serilog:WriteTo:0:Args:path"];
        if (!string.IsNullOrEmpty(logPath))
        {
            configuration["Serilog:WriteTo:0:Args:path"] = Path.Combine(environmentService.LogsDirectory, logPath);
        }

        var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration);
#if DEBUG
        loggerConfiguration.MinimumLevel.Debug();
#endif
        Log.Logger = loggerConfiguration.CreateLogger();
        ServiceContainer.RegisterService(Log.Logger);

        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, "App");
        Debug.Assert(Logger.IsEnabled(LogEventLevel.Debug));
    }

    #endregion

    #region Service implementation

    public T? GetService<T>() where T : class
    {
        return (T?)GetService(typeof(T), null);
    }

    public T? GetService<T>(string? name) where T : class
    {
        return (T?)GetService(typeof(T), name: name);
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        return GetService(serviceType, null);
    }

    public object? GetService(Type serviceType, string? name)
    {
        var service = ServiceContainer.GetService(serviceType, name);
        if (service != null) return service;
        try
        {
            bool useName = !string.IsNullOrEmpty(name);
            foreach (var key in Resources.Keys)
            {
                var value = Resources[key];
                if (key is string str && useName)
                {
                    if (str != name) continue;
                }
                if (!serviceType.IsInstanceOfType(value)) continue;
                service = value;
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }
        return service ?? ServiceProvider.Default.GetService(serviceType, name);
    }

    #endregion
}