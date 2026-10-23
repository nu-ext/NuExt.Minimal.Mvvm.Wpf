using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using MovieWpfApp.Models;
using MovieWpfApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using static Minimal.Mvvm.AccessModifier;

namespace MovieWpfApp.ViewModels;

internal sealed partial class MoviesViewModel : DocumentContentViewModelBase
{
    #region Properties

    [Notify(Setter = Private)]
    private ObservableCollection<MovieModelBase>? _movies;

    [Notify(Setter = Private)]
    private ListCollectionView? _moviesView;

    [Notify(CallbackName = nameof(OnSelectedItemChanged))]
    private MovieModelBase? _selectedItem;

    #endregion

    #region Services

    private IAsyncDialogService DialogService => GetRequiredService<IAsyncDialogService>();

    public EnvironmentService EnvironmentService => GetService<EnvironmentService>()!;

    private MoviesService MoviesService => GetService<MoviesService>()!;

    private new MainWindowViewModel ParentViewModel => GetParentViewModel<MainWindowViewModel>();

    private SettingsService? SettingsService => GetService<SettingsService>();

    #endregion

    #region Event Handlers

    private void OnSelectedItemChanged(MovieModelBase? oldSelectedItem)
    {
        RaiseCanExecuteChanged();
    }

    #endregion

    #region Methods

    protected override async ValueTask DisposeAsyncCore()
    {
        Settings!.SelectedPath = SelectedItem?.GetPath();

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    protected override void OnError(Exception ex, [CallerMemberName] string? callerName = null)
    {
        if (!CheckAccess())
        {
            Invoke(() => OnError(ex, callerName));
            return;
        }

        VerifyAccess();

        MessageBox.Show(GetWindow(), string.Format(Loc.An_error_has_occurred_in_Arg0_Arg1, callerName, ex.Message), Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Debug.Assert(DialogService != null, $"{nameof(DialogService)} is null");
        Debug.Assert(EnvironmentService != null, $"{nameof(EnvironmentService)} is null");
        Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
        Debug.Assert(ParentViewModel != null, $"{nameof(ParentViewModel)} is null");
        Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

        await base.InitializeAsyncCore(cancellationToken);

        Movies = [];
        Lifetime.Add(Movies.Clear);

        MoviesView = new ListCollectionView(Movies);
        Lifetime.Add(MoviesView.DetachFromSourceCollection);

        await ReloadMoviesAsync(cancellationToken);
    }

    private async ValueTask ReloadMoviesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Movies!.Clear();
        var movies = await MoviesService.GetMoviesAsync(cancellationToken);
        movies.ForEach(Movies.Add);
        Movies.OfType<MovieGroupModel>().FirstOrDefault()?.Expand();
    }

    #endregion
}
