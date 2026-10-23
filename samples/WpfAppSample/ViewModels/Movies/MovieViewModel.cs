using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;
using MovieWpfApp.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using static Minimal.Mvvm.AccessModifier;

namespace MovieWpfApp.ViewModels;

internal sealed partial class MovieViewModel : DocumentContentViewModelBase
{
    #region Properties

    [Notify(Setter = Private)]
    private bool _isWindowed;

    public MovieModel Movie => (MovieModel)Parameter!;

    #endregion

    #region Event Handlers

    private void Movie_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MovieModel.Name) or nameof(MovieModel.ReleaseDate))
        {
            UpdateTitle();
        }
    }

    #endregion

    #region Methods

    public override ValueTask<bool> CanCloseAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();

        var dialogResult = MessageBox.Show(GetWindow(), string.Format(Loc.Are_you_sure_you_want_to_close__Arg0__, Movie.Name), Loc.Confirmation,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult != MessageBoxResult.Yes)
        {
            return new ValueTask<bool>(false);
        }

        return base.CanCloseAsync(cancellationToken);
    }

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Debug.Assert(Parameter is MovieModel);
        await base.InitializeAsyncCore(cancellationToken);
        Lifetime.AddBracket(() => Movie.PropertyChanged += Movie_PropertyChanged,
            () => Movie.PropertyChanged -= Movie_PropertyChanged);
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        Title = $"{Movie.Name} [{Movie.ReleaseDate:yyyy}]";
    }

    #endregion
}
