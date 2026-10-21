# WpfMinimalApp

A minimal example of `NuExt.Minimal.Mvvm.Wpf` showing the basic View, ViewModel, command, and dialog workflow.

## Demonstrates

- UI services declared in XAML and resolved from the ViewModel
- `WindowService` integration with `WindowViewModel`
- An asynchronous command and `GetCurrentCommand()`
- Dialog View resolution by name
- Parent and parameter propagation
- Child ViewModel initialization and explicit disposal

## Flow

```text
MainWindow declares UI services
    ↓
MainWindowViewModel resolves IAsyncDialogService
    ↓
ShowDialogCommand creates MessageDialogViewModel
    ↓
InputDialogService resolves MessageDialogView
    ↓
The parameter is applied during initialization
    ↓
The dialog is shown and the child ViewModel is disposed
```

For document management and other application services, see `WpfAppSample`. For multiple UI threads and dispatcher-local services, see `WpfMultiThreadedApp`.