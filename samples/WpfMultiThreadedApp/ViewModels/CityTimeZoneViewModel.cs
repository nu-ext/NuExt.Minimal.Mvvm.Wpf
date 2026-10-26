
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using Serilog;
using Serilog.Core;
using WpfMultiThreadedApp.Models;
using WpfMultiThreadedApp.Services;

namespace WpfMultiThreadedApp.ViewModels;

public partial class CityTimeZoneViewModel : WindowViewModelBase
{
    private readonly DispatcherTimer _timer;

    public CityTimeZoneViewModel()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        Lifetime.AddBracket(() => _timer.Tick += OnTimerTick, () => _timer.Tick -= OnTimerTick);
    }

    #region Properties

    [Notify]
    private DateTime _localTime;

    public new CityTimeZone Parameter => (CityTimeZone)base.Parameter!;

    private new MainWindowViewModel ParentViewModel => GetParentViewModel<MainWindowViewModel>();

    #endregion

    #region Services

    public EnvironmentService EnvironmentService => GetService<EnvironmentService>()!;

    public ILogger? Logger { get; private set; }

    #endregion

    #region Event Handlers

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    #endregion

    #region Methods

    public override ValueTask<bool> CanCloseAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();
        VerifyAccess();

        var dialogResult = MessageBox.Show(GetWindow(), string.Format(Loc.Are_you_sure_you_want_to_close__Arg0__, Parameter.Name), Loc.Confirmation,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult != MessageBoxResult.Yes)
        {
            return new ValueTask<bool>(false);
        }

        return base.CanCloseAsync(cancellationToken);
    }

    protected override IDispatcherService GetDispatcherService()
    {
        return GetLocalService<IDispatcherService>() ?? throw new InvalidOperationException($"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({RuntimeHelpers.GetHashCode(this):X8}): IDispatcherService is not registered.");
    }

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Debug.Assert(GetDispatcherService().Name == "ChildDispatcherService");
        Throw.IfNull(EnvironmentService, $"{nameof(EnvironmentService)} is null");
        Throw.IfNull(Parameter.TimeZoneInfo, $"{nameof(Parameter.TimeZoneInfo)} is null");

        await base.InitializeAsyncCore(cancellationToken);

        Logger = GetService<ILogger>()?.ForContext(Constants.SourceContextPropertyName, nameof(CityTimeZoneViewModel));
        Throw.IfNull(Logger, $"{nameof(Logger)} is null");

        Title = $"{Parameter.Name} - {Parameter.TimeZoneInfo!.DisplayName}";
    }

    protected override ValueTask OnContentRenderedAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(GetWindowService()?.Name == "CurrentWindowService", $"{nameof(WindowService)} is invalid");
        Lifetime.AddBracket(_timer.Start, _timer.Stop);
        return base.OnContentRenderedAsync(cancellationToken);
    }

    protected override void OnError(Exception ex, [CallerMemberName] string? callerName = null)
    {
        base.OnError(ex, callerName);

        Logger?.Error(ex, "Exception in {CallerName}", callerName);
    }

    private void UpdateTime()
    {
        Debug.Assert(CheckAccess());
        LocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Parameter.TimeZoneInfo);
        ParentViewModel.NotifyUpdateTime(Parameter, LocalTime);
    }

    #endregion
}