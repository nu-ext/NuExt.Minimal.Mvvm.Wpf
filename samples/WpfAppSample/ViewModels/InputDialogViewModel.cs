using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;

namespace MovieWpfApp.ViewModels;

internal partial class InputDialogViewModel : ControlViewModelBase
{
    #region Properties

    [Notify]
    private string? _inputMessage;

    [Notify]
    private string? _inputText;

    #endregion

    #region Methods

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await base.InitializeAsyncCore(cancellationToken);
        if (Parameter is string text)
        {
            InputText = text;
        }
    }

    #endregion
}
