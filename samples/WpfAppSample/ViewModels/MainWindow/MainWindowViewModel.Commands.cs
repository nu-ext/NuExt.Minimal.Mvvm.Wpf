using System.Diagnostics;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using MovieWpfApp.Models;
using MovieWpfApp.Views;
using static Minimal.Mvvm.AccessModifier;

namespace MovieWpfApp.ViewModels;

partial class MainWindowViewModel
{
    #region Command Methods

    [Command(nameof(ActiveDocumentChangedCommand), Setter = Private)]
    private void OnActiveDocumentChanged()
    {
        UpdateTitle();
    }

    [Command(nameof(ActiveWindowChangedCommand), Setter = Private)]
    private void OnActiveWindowChanged()
    {
        UpdateTitle();
    }

    private bool CanCloseActiveDocument()
    {
        return IsUsable && _lastActiveDocument != null;
    }

    [Command(Setter = Private)]
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

    [Command(Setter = Private)]
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

    [Command(Setter = Private)]
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

    [Command(Setter = Private)]
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

    [Command(Setter = Private)]
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

    [Command(Setter = Private)]
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

    [Command(Setter = Private, CanExecute = nameof(CanOpenMovie))]
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

    [Command(Setter = Private)]
    private async Task CloseMovieAsync(MovieModel movie)
    {
        var doc = DocumentManagerService.FindDocumentById(new MovieDocument(movie));
        if (doc == null) return;
        await doc.CloseAsync().ConfigureAwait(false);
    }

    #endregion

    #region Methods

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
