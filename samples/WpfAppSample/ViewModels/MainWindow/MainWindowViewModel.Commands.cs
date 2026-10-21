using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using MovieWpfApp.Models;
using MovieWpfApp.Views;
using static AccessModifier;

namespace MovieWpfApp.ViewModels;

partial class MainWindowViewModel
{
    #region Commands

    [Notify(Setter = Private)]
    private ICommand? _activeDocumentChangedCommand;

    [Notify(Setter = Private)]
    private ICommand? _activeWindowChangedCommand;

    #endregion

    #region Command Methods

    private bool CanCloseActiveDocument()
    {
        return IsUsable && _lastActiveDocument != null;
    }

    [Notify(Setter = Private)]
    private async Task CloseActiveDocumentAsync()
    {
        if (_lastActiveDocument != null)
        {
            await _lastActiveDocument.CloseAsync(false);
        }
    }

    private bool CanShowHideActiveDocument(bool show)
    {
        return IsUsable && _lastActiveDocument != null;
    }

    [Notify(Setter = Private)]
    private void ShowHideActiveDocument(bool show)
    {
        if (show)
        {
            _lastActiveDocument?.Show();
            //ActiveDocument = _lastActiveDocument;
        }
        else
        {
            _lastActiveDocument?.Hide();
        }
    }

    private bool CanCloseActiveWindow()
    {
        return IsUsable && _lastActiveWindow != null;
    }

    [Notify(Setter = Private)]
    private async Task CloseActiveWindowAsync()
    {
        if (_lastActiveWindow != null)
        {
            await _lastActiveWindow.CloseAsync(false);
        }
    }

    private bool CanShowHideActiveWindow(bool show)
    {
        return IsUsable && _lastActiveWindow != null;
    }

    [Notify(Setter = Private)]
    private void ShowHideActiveWindow(bool show)
    {
        if (show)
        {
            _lastActiveWindow?.Show();
            //ActiveWindow = _lastActiveWindow;
        }
        else
        {
            _lastActiveWindow?.Hide();
        }
    }

    private bool CanShowMovies()
    {
        return IsUsable;
    }

    [Notify(Setter = Private)]
    private async Task ShowMoviesAsync(CancellationToken cancellationToken)
    {
        var document = await DocumentManagerService.FindDocumentByIdOrCreateAsync(
            id: default(Movies),
            documentType: nameof(MoviesView),
            viewModelFactory: static () => new MoviesViewModel() { Title = Loc.Movies },
            parentViewModel: this,
            parameter: null,
            cancellationToken: cancellationToken);

        document.Show();
    }

    private bool CanOpenMovie(MovieModel movie)
    {
        return IsUsable;
    }

    [Notify(Setter = Private)]
    private async Task OpenMovieAsync(MovieModel movie, CancellationToken cancellationToken)
    {
        var document = await DocumentManagerService.FindDocumentByIdOrCreateAsync(
            id: new MovieDocument(movie),
            documentType: nameof(MovieView),
            viewModelFactory: static () => new MovieViewModel(),
            parentViewModel: this,
            parameter: movie,
            cancellationToken: cancellationToken);

        document.Show();
    }

    [Notify(Setter = Private)]
    private async Task OpenMovieExternalAsync(MovieModel movie, CancellationToken cancellationToken)
    {
        var document = await WindowManagerService.FindDocumentByIdOrCreateAsync(
            id: new MovieDocument(movie),
            documentType: nameof(MovieView),
            viewModelFactory: static () => new MovieViewModel(),
            parentViewModel: this,
            parameter: movie,
            cancellationToken: cancellationToken);

        document.Show();
    }

    private bool CanCloseMovie(MovieModel movie) => CanOpenMovie(movie);

    [Notify(Setter = Private)]
    private async Task CloseMovieAsync(MovieModel movie)
    {
        var doc = DocumentManagerService.FindDocumentById(new MovieDocument(movie));
        if (doc == null) return;
        await doc.CloseAsync().ConfigureAwait(false);
    }

    #endregion

    #region Methods

    protected override void CreateCommands()
    {
        base.CreateCommands();

        ActiveDocumentChangedCommand = new RelayCommand(UpdateTitle);
        ActiveWindowChangedCommand = new RelayCommand(UpdateTitle);
        ShowMoviesCommand = new AsyncCommand(ShowMoviesAsync, CanShowMovies);
        ShowHideActiveDocumentCommand = new RelayCommand<bool>(ShowHideActiveDocument, CanShowHideActiveDocument);
        ShowHideActiveWindowCommand = new RelayCommand<bool>(ShowHideActiveWindow, CanShowHideActiveWindow);
        CloseActiveDocumentCommand = new AsyncCommand(CloseActiveDocumentAsync, CanCloseActiveDocument);
        CloseActiveWindowCommand = new AsyncCommand(CloseActiveWindowAsync, CanCloseActiveWindow);
        OpenMovieCommand = new AsyncCommand<MovieModel>(OpenMovieAsync, CanOpenMovie);
        OpenMovieExternalCommand = new AsyncCommand<MovieModel>(OpenMovieExternalAsync, CanOpenMovie);
        CloseMovieCommand = new AsyncCommand<MovieModel>(CloseMovieAsync, CanCloseMovie);
    }

    protected override ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null)
    {
        return callerName switch
        {
            nameof(UpdateTitle) => throw new ArgumentException($"Multiple commands found for method '{callerName}'.", nameof(callerName)),
            nameof(ShowMoviesAsync) => ShowMoviesCommand,
            nameof(ShowHideActiveDocument) => ShowHideActiveDocumentCommand,
            nameof(ShowHideActiveWindow) => ShowHideActiveWindowCommand,
            nameof(CloseActiveDocumentAsync) => CloseActiveDocumentCommand,
            nameof(CloseActiveWindowAsync) => CloseActiveWindowCommand,
            nameof(OpenMovieAsync) => OpenMovieCommand,
            nameof(OpenMovieExternalAsync) => OpenMovieExternalCommand,
            nameof(CloseMovieAsync) => CloseMovieCommand,
            _ => base.GetCurrentCommand(callerName)
        };
    }

    protected override void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        base.GetAllCommands(ref builder);

        builder.Append((nameof(ActiveDocumentChangedCommand), ActiveDocumentChangedCommand));
        builder.Append((nameof(ActiveWindowChangedCommand), ActiveWindowChangedCommand));
        builder.Append((nameof(ShowMoviesCommand), ShowMoviesCommand));
        builder.Append((nameof(ShowHideActiveDocumentCommand), ShowHideActiveDocumentCommand));
        builder.Append((nameof(ShowHideActiveWindowCommand), ShowHideActiveWindowCommand));
        builder.Append((nameof(CloseActiveDocumentCommand), CloseActiveDocumentCommand));
        builder.Append((nameof(CloseActiveWindowCommand), CloseActiveWindowCommand));
        builder.Append((nameof(OpenMovieCommand), OpenMovieCommand));
        builder.Append((nameof(OpenMovieExternalCommand), OpenMovieExternalCommand));
        builder.Append((nameof(CloseMovieCommand), CloseMovieCommand));
    }

    protected override void NullifyCommands()
    {
        ActiveDocumentChangedCommand = null;
        ActiveWindowChangedCommand = null;
        ShowMoviesCommand = null;
        ShowHideActiveDocumentCommand = null;
        ShowHideActiveWindowCommand = null;
        CloseActiveDocumentCommand = null;
        CloseActiveWindowCommand = null;
        OpenMovieCommand = null;
        OpenMovieExternalCommand = null;
        CloseMovieCommand = null;

        base.NullifyCommands();
    }

    protected override async ValueTask OnContentRenderedAsync(CancellationToken cancellationToken)
    {
        await base.OnContentRenderedAsync(cancellationToken);

        Debug.Assert(CheckAccess());
        cancellationToken.ThrowIfCancellationRequested();

        await LoadMenuAsync(cancellationToken);

        await MoviesService.InitializeAsync(cancellationToken);
        RaiseCanExecuteChanged();

        Debug.Assert(Settings!.IsSuspended == false);
        if (Settings.MoviesOpened)
        {
            ShowMoviesCommand?.Execute(null);
        }
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        CreateSettings();
        UpdateTitle();
    }

    #endregion
}
