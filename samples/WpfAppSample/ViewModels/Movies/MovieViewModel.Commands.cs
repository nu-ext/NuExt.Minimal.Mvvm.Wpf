using Minimal.Mvvm;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static Minimal.Mvvm.AccessModifier;

namespace MovieWpfApp.ViewModels;

partial class MovieViewModel
{
    #region Command Methods

    private bool CanClose()
    {
        return IsWindowed;
    }

    [Notify(Setter = Private)]
    private void Close()
    {
        GetLocalService<IWindowService>()?.Close();
    }

    [Notify(Setter = Private)]
    private void ContentRendered()
    {
        IsWindowed = true;
        CloseCommand?.RaiseCanExecuteChanged();
    }

    #endregion

    #region Methods

    protected override void CreateCommands()
    {
        base.CreateCommands();

        ContentRenderedCommand = new RelayCommand(ContentRendered);
        CloseCommand = new RelayCommand(Close, CanClose);
    }

    protected override ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null)
    {
        return callerName switch
        {
            nameof(ContentRendered) => ContentRenderedCommand,
            nameof(Close) => CloseCommand,
            _ => base.GetCurrentCommand(callerName)
        };
    }

    protected override void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        base.GetAllCommands(ref builder);

        builder.Append((nameof(ContentRenderedCommand), ContentRenderedCommand));
        builder.Append((nameof(CloseCommand), CloseCommand));
    }

    protected override void NullifyCommands()
    {
        ContentRenderedCommand = null;
        CloseCommand = null;

        base.NullifyCommands();
    }

    #endregion
}
