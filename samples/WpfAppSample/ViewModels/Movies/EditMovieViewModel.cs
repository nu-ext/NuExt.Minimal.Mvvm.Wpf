using Minimal.Mvvm.Wpf;
using MovieWpfApp.Models;
using MovieWpfApp.Services;
using System.ComponentModel;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels;

internal sealed class EditMovieViewModel : ControlViewModelBase, IDataErrorInfo
{
    #region Properties

    public MovieModel Movie => (MovieModel)Parameter!;

    #endregion

    #region Services

    public MoviesService MoviesService => GetService<MoviesService>()!;

    #endregion

    #region Methods

    protected override Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Debug.Assert(Movie != null, $"{nameof(Movie)} is null");
        Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
        return base.InitializeAsyncCore(cancellationToken);
    }

    #endregion

    #region IDataErrorInfo

    public string Error => Movie.Error;

    string IDataErrorInfo.this[string columnName] => null!;

    #endregion
}
