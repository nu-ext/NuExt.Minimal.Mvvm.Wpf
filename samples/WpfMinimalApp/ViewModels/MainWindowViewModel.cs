using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;

namespace WpfMinimalApp.ViewModels;

public partial class MainWindowViewModel : WindowViewModel
{
    private IAsyncDialogService DialogService => GetRequiredService<IAsyncDialogService>();

    protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await base.InitializeAsyncCore(cancellationToken);

        _ = DialogService;
        _ = GetDispatcherService();
    }

    protected override void CreateCommands()
    {
        base.CreateCommands();

        ShowDialogCommand = new AsyncCommand(ShowDialogAsync);
    }

    protected override ICommand? GetCurrentCommand([CallerMemberName] string? callerName = null) =>
        callerName switch
        {
            nameof(ShowDialogAsync) => ShowDialogCommand,
            _ => base.GetCurrentCommand(callerName)
        };

    protected override void GetAllCommands(ref ValueListBuilder<(string PropertyName, ICommand? Command)> builder)
    {
        base.GetAllCommands(ref builder);

        builder.Append((nameof(ShowDialogCommand), ShowDialogCommand));
    }

    protected override void NullifyCommands()
    {
        ShowDialogCommand = null;

        base.NullifyCommands();
    }

    [Notify(Setter = AccessModifier.Private)]
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