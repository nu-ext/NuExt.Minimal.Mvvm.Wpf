using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using Serilog;
using Serilog.Core;
using WpfMultiThreadedApp.Models;
using WpfMultiThreadedApp.Services;
using WpfMultiThreadedApp.Views;

namespace WpfMultiThreadedApp.ViewModels;

internal sealed partial class MainWindowViewModel : WindowViewModel
{
    public static readonly (string Name, string TimeZoneId)[] Cities =
    [
        new("London", "GMT Standard Time"),
        new("Paris", "W. Europe Standard Time"),
        new("Cairo", "Egypt Standard Time"),
        new("Moscow", "Russian Standard Time"),
        new("Dubai", "Arabian Standard Time"),
        new("Delhi", "India Standard Time"),
        new("Bangkok", "SE Asia Standard Time"),
        new("Beijing", "China Standard Time"),
        new("Tokyo", "Tokyo Standard Time"),
        new("Sydney", "AUS Eastern Standard Time"),
        new("Auckland", "New Zealand Standard Time"),
        new("Honolulu", "Hawaiian Standard Time"),
        new("Anchorage", "Alaskan Standard Time"),
        new("Los Angeles", "Pacific Standard Time"),
        new("Denver", "Mountain Standard Time"),
        new("Chicago", "Central Standard Time"),
        new("New York", "Eastern Standard Time"),
        new("Halifax", "Atlantic Standard Time"),
        new("Buenos Aires", "Argentina Standard Time"),
        new("Rio de Janeiro", "E. South America Standard Time")
    ];

    private readonly object _syncLock = new();


    #region Properties

    public ObservableDictionary<CityTimeZone, CityTimeZoneModel> CitiesView { get; } = [];

    public bool UseMultiThreaded { get; init; }

    public bool UseWindowManager { get; init; }

    #endregion

    #region Services

    public EnvironmentService EnvironmentService => GetRequiredService<EnvironmentService>();

    public ILogger? Logger { get; private set; }

    public IViewWindowService? ViewWindowService => GetService<IViewWindowService>("Windows");

    public IAsyncDocumentManagerService? WindowManagerService => GetService<IAsyncDocumentManagerService>("Documents");

    #endregion

    #region Command Methods

    private bool CanShowCity(CityTimeZone key)
    {
        return key != default && IsUsable;
    }

    [Notify(Setter = AccessModifier.Private)]
    private void ShowCity(CityTimeZone key)
    {
        VerifyAccess();

        if (UseWindowManager)
        {
            var document = WindowManagerService!.FindDocumentById(key);
            document?.Show();
        }
        else
        {
            IWindowViewModel? viewModel = null;
            foreach (var vm in GetService<IOpenWindowsService>()?.ViewModels ?? [])
            {
                if (Equals(vm.Parameter, key))
                {
                    viewModel = vm;
                    break;
                }
            }
            viewModel?.Show();
        }
    }

    private bool CanCloseCity(CityTimeZone key)
    {
        return key != default && IsUsable;
    }

    [Notify(Setter = AccessModifier.Private)]
    private async Task CloseCityAsync(CityTimeZone key)
    {
        VerifyAccess();

        var dialogResult = MessageBox.Show(GetWindow(), string.Format(Loc.Are_you_sure_you_want_to_close__Arg0__, key.Name), Loc.Confirmation,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult != MessageBoxResult.Yes) return;

        if (UseWindowManager)
        {
            var document = WindowManagerService!.FindDocumentById(key);
            if (document != null)
            {
                await document.CloseAsync();
            }
        }
        else
        {
            IWindowViewModel? viewModel = null;
            foreach (var vm in GetService<IOpenWindowsService>()?.ViewModels ?? [])
            {
                if (Equals(vm.Parameter, key))
                {
                    viewModel = vm;
                    break;
                }
            }
            if (viewModel != null)
            {
                await viewModel.CloseAsync();
            }
        }
    }

    protected override void CreateCommands()
    {
        base.CreateCommands();

        ShowCityCommand = new RelayCommand<CityTimeZone>(ShowCity, CanShowCity);
        CloseCityCommand = new AsyncCommand<CityTimeZone>(CloseCityAsync, CanCloseCity);
    }

    protected override ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null)
    {
        return callerName switch
        {
            nameof(ShowCity) => ShowCityCommand,
            nameof(CloseCityAsync) => CloseCityCommand,
            _ => base.GetCurrentCommand(callerName)
        };
    }

    protected override void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        base.GetAllCommands(ref builder);

        builder.Append((nameof(ShowCityCommand), ShowCityCommand));
        builder.Append((nameof(CloseCityCommand), CloseCityCommand));
    }

    protected override void NullifyCommands()
    {
        ShowCityCommand = null;
        CloseCityCommand = null;

        base.NullifyCommands();
    }

    #endregion

    #region Methods

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Debug.Assert(GetDispatcherService().Name == "MainDispatcherService");
        Throw.IfNull(EnvironmentService, $"{nameof(EnvironmentService)} is null");

        await base.InitializeAsyncCore(cancellationToken);

        Logger = GetService<ILogger>()?.ForContext(Constants.SourceContextPropertyName, nameof(MainWindowViewModel));
        Throw.IfNull(Logger, $"{nameof(Logger)} is null");

        Lifetime.AddBracket(
            () => BindingOperations.EnableCollectionSynchronization(CitiesView, _syncLock),
            () => BindingOperations.DisableCollectionSynchronization(CitiesView));

        if (UseWindowManager)
        {
            Debug.Assert(ViewWindowService is null);
            Throw.InvalidOperationExceptionIf(WindowManagerService?.Name != "Documents", $"{nameof(WindowManagerService)} is invalid");
            Lifetime.AddAsync(WindowManagerService!.CloseAllAsync);
        }
        else
        {
            Debug.Assert(WindowManagerService is null);
            Throw.InvalidOperationExceptionIf(ViewWindowService?.Name != "Windows", $"{nameof(ViewWindowService)} is invalid");
        }

        Title = $"WPF Multi-Threaded App - Main Window";
    }

    protected override async ValueTask OnContentRenderedAsync(CancellationToken cancellationToken)
    {
        await base.OnContentRenderedAsync(cancellationToken);

        int i = new Random().Next(Cities.Length);
        do
        {
            var (name, timeZoneId) = Cities[i % Cities.Length];
            try
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                CitiesView.Add(new CityTimeZone(name, timeZone), new CityTimeZoneModel() { Name = name });
            }
            catch (TimeZoneNotFoundException)
            {
                Trace.WriteLine($"Time zone not found: {timeZoneId}");
                Logger?.Error("Time zone not found: {TimeZoneId}", timeZoneId);
            }
            catch (InvalidTimeZoneException)
            {
                Trace.WriteLine($"Invalid time zone: {timeZoneId}");
                Logger?.Error("Invalid time zone: {TimeZoneId}", timeZoneId);
            }
            i++;
            //if (CitiesView.Count == 2) break;
        }
        while ((CitiesView.Count < Environment.ProcessorCount) && (i < 100));

        foreach (var (city, _) in CitiesView)
        {
            if (!UseMultiThreaded)
            {
                ThreadStartingPoint(city);
                continue;
            }
            var newWindowThread = new Thread(ThreadStartingPoint);
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start(city);
        }
    }

    private async void ThreadStartingPoint(object? state)
    {
        var viewModel = new CityTimeZoneViewModel();
        try
        {
            if (UseWindowManager)
            {
                var document = await WindowManagerService!.CreateDocumentAsync(null, viewModel, this, state, CancellationToken);
                document.DisposeOnClose = true;
                document.Id = state!;
                document.Show();
            }
            else
            {
                await ViewWindowService!.ShowAsync(nameof(CityTimeZoneView), viewModel, this, state, CancellationToken);
            }

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        catch (Exception ex)
        {
            Debug.Assert(ex is OperationCanceledException, ex.Message);
            await viewModel.DisposeAsync();
            OnError(ex);
            return;
        }

        if (UseMultiThreaded)
        {
            System.Windows.Threading.Dispatcher.Run();
        }

        void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not CityTimeZoneViewModel viewModel)
            {
                return;
            }

            if (e.PropertyName == nameof(viewModel.IsDisposed))
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                if (IsDisposingOrDisposed)
                {
                    return;
                }
                lock (_syncLock)
                {
                    Debug.Assert(!IsDisposingOrDisposed);
                    if (IsDisposingOrDisposed)
                    {
                        return;
                    }
                    bool remove = CitiesView.Remove(viewModel.Parameter);
                    Debug.Assert(remove);
                }
            }
        }
    }

    protected override void OnError(Exception ex, [CallerMemberName] string? callerName = null)
    {
        base.OnError(ex, callerName);

        Logger?.Error(ex, "Exception in {CallerName}.", callerName);
    }

    public void NotifyUpdateTime(CityTimeZone key, DateTime localTime)
    {
        lock (_syncLock)
        {
            Debug.Assert(CitiesView.ContainsKey(key));
            CitiesView[key].LocalTime = localTime;
        }
    }

    #endregion
}
