using System.Diagnostics;
using System.Windows;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;

namespace WpfMinimalApp.ViewModels;

[GenerateCommands]
public partial class MainWindowViewModel : WindowViewModelBase
{
    private IAsyncDialogService DialogService => GetRequiredService<IAsyncDialogService>();

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await base.InitializeAsyncCore(cancellationToken);

        _ = DialogService;
        _ = GetDispatcherService();
    }

    [Command(Setter = AccessModifier.Private)]
    private async Task ShowDialogAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(GetCurrentCommand() == ShowDialogCommand);

        await using var viewModel = new MessageDialogViewModel();

        await DialogService.ShowDialogAsync(
            dialogButtons: MessageBoxButton.OK,
            title: "Message",
            documentType: nameof(Views.MessageDialogView),
            viewModel: viewModel,
            parentViewModel: this,
            parameter: "Hello from NuExt.Minimal.Mvvm.Wpf",
            cancellationToken: cancellationToken);
    }
}