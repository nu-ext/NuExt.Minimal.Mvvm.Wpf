using System.Windows;
using WpfMinimalApp.ViewModels;

namespace WpfMinimalApp;

public partial class App
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = new MainWindowViewModel { Title = "NuExt Minimal WPF" };
        var window = new MainWindow { DataContext = viewModel };

        await viewModel.InitializeAsync();
        window.Show();
    }
}