using Minimal.Mvvm;
using static Minimal.Mvvm.AccessModifier;

namespace MovieWpfApp.ViewModels;

partial class MovieViewModel
{
    #region Command Methods

    private bool CanClose()
    {
        return IsWindowed;
    }

    [Command(Setter = Private)]
    private void Close()
    {
        GetLocalService<IWindowService>()?.Close();
    }

    [Command(Setter = Private)]
    private void ContentRendered()
    {
        IsWindowed = true;
        CloseCommand?.RaiseCanExecuteChanged();
    }

    #endregion
}