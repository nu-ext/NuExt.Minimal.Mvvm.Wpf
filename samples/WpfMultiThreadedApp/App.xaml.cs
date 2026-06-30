using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using WpfMultiThreadedApp.Services;
using WpfMultiThreadedApp.ViewModels;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
        var logger = GetService<ILogger>();
        if (logger?.IsEnabled(LogLevel.Error) == true)
        {
            logger.LogError(e.Exception, "Application Dispatcher Unhandled Exception: {Exception}.", e.Exception.Message);
        }
        e.Handled = true;
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        var logger = GetService<ILogger>();

        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation("Application exited with code {ExitCode}.", e.ApplicationExitCode);
        }
        Log.CloseAndFlush();
    }

    private async void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        if (e.ReasonSessionEnding != ReasonSessionEnding.Shutdown)
        {
            return;
        }
        var logger = GetService<ILogger>();
        try
        {
            await OpenWindowsService.DisposeAsync().ConfigureAwait(false);
            Debug.Assert(OpenWindowsService.Count == 0);
        }
        catch (Exception ex)
        {
            if (logger?.IsEnabled(LogLevel.Error) == true)
            {
                logger.LogError(ex, "Application SessionEnding Exception: {Exception}.", ex.Message);
            }
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


        var logger = GetService<ILogger>();
        logger?.LogInformation("Application started.");

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
            logger?.LogError(ex, "Error while initialization");
            await viewModel.DisposeAsync();
            Shutdown();
            return;
        }

        _ = Task.Run(() => PerformanceMonitor.RunAsync(_cts.Token), _cts.Token);
    }

    private async ValueTask OnDisposingAsync(object? sender, EventArgs e, CancellationToken cancellationToken)
    {
        var logger = GetService<ILogger>();
        try
        {
            await _lifetime.DisposeAsync().ConfigureAwait(false);
            Debug.Assert(OpenWindowsService.Count == 0);
        }
        catch (Exception ex)
        {
            if (logger?.IsEnabled(LogLevel.Error) == true)
            {
                logger.LogError(ex, "App Disposing Exception: {Exception}.", ex.Message);
            }
            Debug.Fail(ex.Message);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logger = GetService<ILogger>();
        if (logger?.IsEnabled(LogLevel.Error) == true)
        {
            logger.LogError(e.Exception, "TaskScheduler Unobserved Task Exception: {Exception}.", e.Exception.Message);
        }
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

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
            builder.AddSerilog();
        });

        ServiceContainer.RegisterService(loggerFactory);

        var logger = loggerFactory.CreateLogger("App");
        Debug.Assert(logger.IsEnabled(LogLevel.Debug));
        ServiceContainer.RegisterService(logger);
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
        var service = ServiceContainer.GetService(serviceType);
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