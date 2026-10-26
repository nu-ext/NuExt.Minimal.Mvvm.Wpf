using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;

namespace WpfMinimalApp.ViewModels;

public partial class MessageDialogViewModel : ControlViewModelBase
{
    [Notify] public partial string Message { get; private set; }

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await base.InitializeAsyncCore(cancellationToken);
        Message = Parameter as string ?? string.Empty;
    }
}