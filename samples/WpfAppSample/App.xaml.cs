using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using MovieWpfApp.Services;
using MovieWpfApp.ViewModels;
using NLog;
using NLog.Extensions.Logging;
using Presentation.Wpf;
using Presentation.Wpf.Diagnostics;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MovieWpfApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : IServiceProvider, IDispatcherObject
{
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _createdNew;
    private readonly EventWaitHandle _ewh;
    private readonly AsyncLifetime _lifetime = new() { ContinueOnCapturedContext = true };

    public App()
    {
        _ewh = new EventWaitHandle(false, EventResetMode.AutoReset, $"{GetType().FullName}", out _createdNew);
        _lifetime.AddDisposable(_ewh);
        _lifetime.Add(_cts.Cancel);
        ServiceContainer = new ServiceProvider(this);
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    #region Properties

    public PerformanceMonitor PerformanceMonitor { get; } = new(Process.GetCurrentProcess(), CultureInfo.InvariantCulture)
    {
        ShowPeakMemoryUsage = true,
        ShowManagedMemory = true,
        ShowPeakManagedMemory = true,
        ShowThreads = true
    };

    public IServiceContainer ServiceContainer { get; }

    #endregion

    #region Services

    public EnvironmentService? EnvironmentService => field ??= GetService<EnvironmentService>();

    private OpenWindowsService OpenWindowsService => field ??= GetService<OpenWindowsService>() ?? throw new ArgumentNullException(nameof(OpenWindowsService));

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
        LogManager.Shutdown();
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
            await OpenWindowsService.DisposeAsync();
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
        if (!_createdNew)
        {
            _ewh.Set();
            Shutdown();
            return;
        }

        Minimal.Mvvm.Wpf.Bootstrap.Initialize();

        ServiceContainer.RegisterService(new DispatcherService() { Name = "AppDispatcherService" });

        PresentationTraceSources.Refresh();
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorTraceListener());

        var environmentService = new EnvironmentService(AppDomain.CurrentDomain.BaseDirectory, e.Args);
        ServiceContainer.RegisterService(environmentService);

        ConfigureLogging(environmentService);

        environmentService.LoadLocalization(typeof(Loc), CultureInfo.CurrentUICulture.IetfLanguageTag);

        var logger = GetService<ILogger>();
        logger?.LogInformation("Application started.");

        _lifetime.AddAsyncDisposable(OpenWindowsService);

        ServiceContainer.RegisterService(new MoviesService(Path.Combine(environmentService.BaseDirectory, "movies.json")));

        var viewModel = new MainWindowViewModel() { ParentViewModel = this };
        _lifetime.AddBracket(
            () => viewModel.Disposing += OnDisposingAsync,
            () => viewModel.Disposing -= OnDisposingAsync);
        try
        {
            var window = new MainWindow { DataContext = viewModel };
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

        _ = Task.Run(() => WaitForNotifyAsync(_cts.Token), _cts.Token);
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
        var configFile = Path.Combine(environmentService.ConfigDirectory, "nlog.config.json");
        Debug.Assert(File.Exists(configFile), $"Configuration file not found: {configFile}");
        var config = new ConfigurationBuilder()
            .SetBasePath(environmentService.BaseDirectory)
            .AddJsonFile(configFile, optional: false, reloadOnChange: true)
            .Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
            builder.AddNLog(config);
        });
        LogManager.Configuration!.Variables["basedir"] = environmentService.LogsDirectory;
        ServiceContainer.RegisterService(loggerFactory);

        var logger = loggerFactory.CreateLogger("App");
        Debug.Assert(logger.IsEnabled(LogLevel.Debug));
        ServiceContainer.RegisterService(logger);
    }

    public T? GetService<T>()
    {
        return (T?)GetService(typeof(T));
    }

    private async Task WaitForNotifyAsync(CancellationToken cancellationToken)
    {
        using var awaiter = new AsyncWaitHandle(_ewh);
        try
        {
            while (await awaiter.WaitOneAsync(cancellationToken))//_ewh is set
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    MainWindow?.BringToFront();
                });
            }
        }
        catch (OperationCanceledException)
        {
            //do nothing
        }
        catch (Exception ex)
        {
            var logger = GetService<ILogger>();
            logger?.LogError(ex, "Unexpected error");
        }
    }

    #endregion

    #region IServiceProvider implementation

    public object? GetService(Type serviceType)
    {
        var service = ServiceContainer.GetService(serviceType);
        if (service != null) return service;
        try
        {
            foreach (var key in Resources.Keys)
            {
                var value = Resources[key];
                if (!serviceType.IsInstanceOfType(value)) continue;
                service = value;
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }
        return service ?? ServiceProvider.Default.GetService(serviceType);
    }

    #endregion
}
