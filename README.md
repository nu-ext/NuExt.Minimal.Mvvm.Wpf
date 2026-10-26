# NuExt.Minimal.Mvvm.Wpf

`NuExt.Minimal.Mvvm.Wpf` is a **WPF add-on** for the lightweight MVVM core ([NuExt.Minimal.Mvvm](https://github.com/nu-ext/NuExt.Minimal.Mvvm)). It adds View resolution, dispatcher-aware UI services, dialogs, documents, window management, and WPF-specific ViewModel infrastructure without introducing a full application framework.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf)
[![Ask DeepWiki](https://img.shields.io/badge/Ask-DeepWiki-blue)](https://deepwiki.com/nu-ext/NuExt.Minimal.Mvvm.Wpf)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm.Wpf?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf)

## Why NuExt.Minimal.Mvvm.Wpf?

`NuExt.Minimal.Mvvm.Wpf` provides the WPF-specific infrastructure that a minimal MVVM core intentionally leaves out: View resolution, dispatcher-safe UI services, dialogs, tabbed and windowed documents, window lifecycle, and deterministic cleanup.

UI services are attached to Views in XAML and registered in the corresponding ViewModel scope. This keeps dialogs, window management, document hosts, media controls, and other visual services close to the UI context that owns them, while allowing ViewModels to resolve them through the existing parent-aware service hierarchy.

Views can be created explicitly through templates or resolved through a customizable `ViewLocator`. ViewModels, Views, document hosts, and windows follow coordinated asynchronous lifecycles, while bindings, cancellation sources, and event subscriptions have explicit ownership and cleanup.

The same document contract works for tabs and independent windows, and dispatcher-aware services keep UI operations on the correct thread without requiring a full application framework, convention engine, reactive stack, or vendor-specific control suite.

### How the pieces fit together

```text
View declares UI services in XAML
    ↓
Services register in the ViewModel's local UI scope
    ↓
The ViewModel resolves local, parent, or application services
    ↓
A View, dialog, document, or window is created explicitly
    ↓
The child ViewModel receives its parent and parameter
    ↓
Initialization completes before the UI operation continues
    ↓
Commands, cancellation, and close validation participate in the lifecycle
    ↓
Cleanup releases the owned ViewModel, bindings, subscriptions, and UI host
```

## Highlights

- **Control/window-oriented ViewModels**  
  `ControlViewModelBase` and `WindowViewModelBase` provide async lifecycle, parent–child composition, dispatcher access, service cleanup, and window close coordination.

- **Attached UI services and behaviors**  
  Dialogs, window services, document managers, media services, and behaviors are configured next to the View in XAML and become available through its ViewModel scope.

- **Explicit view composition**  
  Predictable view resolution with overridable conventions and a safe fallback view. Use templates when you have them, or a customizable `ViewLocator` when you don’t have one. See **View location** below for details.

- **Deterministic ownership**  
  Views, ViewModels, bindings, subscriptions, cancellation sources, and document hosts are released in a defined order.

- **Command lifecycle without reflection**  
  ViewModels can cancel, await, requery, validate, and release their command set without discovering command properties through runtime reflection.

- **Async‑first UX**  
  Dialogs and documents are orchestrated through an async lifecycle that covers view creation, ViewModel initialization, cancellation, closure, and cleanup. `IAsyncDialogService` shows modal dialogs with typed commands/results and optional validation. `IAsyncDocumentManagerService` manages the creation/activation/closure of documents that live as windows or tabs.

- **Deterministic windows and documents**  
  Restore/save window position/size/state (`WindowPlacementService`). Manage **documents as windows** (`WindowedDocumentService`) or **as tabs** (`TabbedDocumentService`) using a unified `IAsyncDocument` contract (ID, title, `Show/Hide/CloseAsync`, “dispose ViewModel on close”, optional “hide instead of close”).

- **Multi‑threaded WPF ready**  
  Works in multi‑UI‑thread apps (each window on its own dispatcher). `DispatcherService` is injected per thread; window‑oriented services are dispatcher‑aware by design.

- **Minimal dependencies, performance-oriented implementation**  
  No full application framework or vendor-specific control suite. Hot paths avoid unnecessary allocations, reflection is kept away from command lifecycle operations, and reusable infrastructure remains explicit.

## Compatibility

- **WPF on .NET 8/9/10 and .NET Framework 4.6.2+**

## Key concepts

- **Control/window ViewModel bases + DispatcherService** — ViewModels expose a *predictable* async lifecycle and run code on the owning UI thread via `IDispatcherService`. This is why the same patterns also work in **multi‑threaded WPF**, where each window has its own dispatcher.

- **IAsyncDocument + IAsyncDocumentManagerService** — a document is a *hosted view + ViewModel* with a stable contract (`Id`, `Title`, `Show/Hide/CloseAsync`, optional “dispose ViewModel on close”). The **manager** creates documents (`CreateDocumentAsync`), tracks `ActiveDocument`, enumerates `Documents`, supports bulk `CloseAllAsync`, and plugs into WPF as **windows** (`WindowedDocumentService`) or **tabs** (`TabbedDocumentService`).

- **IAsyncDialogService** — shows modal dialogs with a view resolved by name/template and a ViewModel you provide; returns either a `MessageBoxResult` or a selected `UICommand`. The service handles view creation, optional title binding, validation (via `IDataErrorInfo` / `INotifyDataErrorInfo`), and clean teardown.

### ViewModel base hierarchy and command generation

The WPF package separates general ViewModel behavior from control- and window-specific behavior:

```text
ViewModelBase
    ↓
CommandViewModelBase implements ICommandViewModel
    ↓
ControlViewModelBase implements IControlViewModel
    ↓
WindowViewModelBase implements IWindowViewModel
```

- `ViewModelBase` provides asynchronous initialization, parent/parameter support, and hierarchical service resolution.
- `CommandViewModelBase` adds command creation, lookup, enumeration, cancellation, completion tracking, requery, and cleanup without runtime reflection.
- `ControlViewModelBase` adds control lifecycle, dispatcher synchronization, asynchronous disposal, and the `LoadedCommand` and `UnloadedCommand` integration points.
- `WindowViewModelBase` adds window state, close validation, cancellation, placement notifications, and window-specific commands.

Use the lowest base class that provides the capabilities required by the ViewModel. A ViewModel that needs generated commands but is not associated with a control can derive directly from `CommandViewModelBase`.

The companion [`NuExt.Minimal.Mvvm.SourceGenerator`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) can generate the complete command infrastructure for a partial ViewModel. Apply `[GenerateCommands]` to the class and `[Command]` to each command method:

```csharp
using Minimal.Mvvm;
using Minimal.Mvvm.Wpf;

[GenerateCommands]
public partial class EditorViewModel : CommandViewModelBase
{
    private bool CanSave() => HasChanges;

    [Command(Setter = AccessModifier.Private)]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        return SaveCoreAsync(cancellationToken);
    }
}
```

For each `[Command]` method, the generator creates the command property and command instance, resolves a compatible `CanExecute` method, and generates the overrides used to create, find, enumerate, and nullify commands. Explicit callback names can be supplied with `CanExecute = nameof(...)`. Generated overrides participate in the base-class chain, so commands declared by base ViewModels remain included.

Command instances are created synchronously from `InitializeAsyncCore`, preserving the caller's synchronization context, and are nullified during uninitialization. `ICommandViewModel` exposes operations to cancel executing asynchronous commands, wait for their completion, and raise `CanExecuteChanged` for the complete command set.

### ViewModel lifecycle

Use each lifecycle stage for a distinct kind of work. `LoadedCommand`, `ContentRenderedCommand`, and `ClosingCommand` participate when they are bound to the corresponding View events:

```text
Construction
    Establish local state
        ↓
InitializeAsyncCore
    Create commands, resolve UI services, apply parameters, and register owned resources
        ↓
LoadedCommand
    React to the View entering the visual tree
        ↓
ContentRenderedCommand
    Run work that requires the Window content to be rendered
        ↓
ClosingCommand
    Intercept the Window Closing event and start asynchronous closing
        ↓
CanCloseAsync
    Validate or cancel the close request
        ↓
DisposeAsyncCore
    Cancel pending work and release the owned resource graph
```

Register subscriptions and other acquired resources with `Lifetime` during initialization:

```csharp
protected override async Task InitializeAsyncCore(CancellationToken cancellationToken)
{
    await base.InitializeAsyncCore(cancellationToken);

    var source = ...
    var resource = ...

    Lifetime.AddBracket(
        () => source.PropertyChanged += OnPropertyChanged,
        () => source.PropertyChanged -= OnPropertyChanged);

    Lifetime.Add(resource);
    Lifetime.AddAsync(ReleaseAsync);
}
```

Cleanup operations are executed in reverse registration order. Register resources in acquisition order so dependent resources are released before the resources they depend on.

## View location (predictable, overridable)

Views are resolved in a strict order by the window/document services:
**`ViewTemplate` → `ViewTemplateKey` → `ViewTemplateSelector` → `ViewLocator`**.  
The default `ViewLocator` searches loaded assemblies, caches types, and produces a **fallback view** if nothing matches.

**Register a custom view name** (e.g., when you don’t follow naming conventions):
```csharp
// if the default locator is the built-in ViewLocator, you can register names:
if (Minimal.Mvvm.Wpf.ViewLocator.Default is Minimal.Mvvm.Wpf.ViewLocator v)
{
    v.RegisterType("MyCustomView", typeof(MyCustomView));
}
```
**Fallback view**
When no View type is found, a lightweight `FallbackView` is created with a readable error message. If the View type is found but cannot be instantiated, the locator throws an `InvalidOperationException` at runtime and preserves the original exception as its inner exception. Override `ViewLocatorBase` or assign `ViewLocator.Default` if you need full control over lookup and creation.

The built-in locator also exposes `ClearCache()` and `ClearRegisteredTypes()` for applications that change registrations or load Views dynamically.

## Windowed vs ViewWindowService

**When to use which:**
- Use `WindowedDocumentService` when you manage multiple long‑lived windows as **documents** (ID reuse, ActiveDocument, bulk close).
- Use `ViewWindowService` when you occasionally **show a view as a window** (modal/non‑modal) without keeping a document roster.

### Show a standalone window

Use `ViewWindowService` when the window does not need document identity, reuse, active-document tracking, or bulk close operations.

```csharp
private IViewWindowService ViewWindowService => GetRequiredService<IViewWindowService>();

private ValueTask ShowDetailsAsync(DetailsModel details, CancellationToken cancellationToken)
{
    return ViewWindowService.ShowAsync(
        viewName: nameof(DetailsView),
        viewModel: new DetailsWindowViewModel(),
        parentViewModel: this,
        parameter: details,
        cancellationToken: cancellationToken);
}
```

A `WindowViewModelBase` coordinates closing and disposal when the created Window includes a `WindowService` bound to `ClosingCommand`. `ViewWindowService` manages the View and Window infrastructure but does not take ownership of an arbitrary disposable ViewModel.

## Quick Start (practical, minimal)

### 1) Async dialog with validation + automatic window placement

**XAML (attach services to your Window via behaviors):**
```xml
<Window
  ...
  xmlns:nx="http://schemas.nuext.minimal/xaml">
  <nx:Interaction.Behaviors>
    <nx:DispatcherService/>
    <nx:WindowService ClosingCommand="{Binding ClosingCommand}"/>
    <nx:InputDialogService
        x:Name="Dialogs"
        MessageBoxButtonLocalizer="{StaticResource MessageBoxButtonLocalizer}"
        ValidatesOnDataErrors="True"
        ValidatesOnNotifyDataErrors="True"/>
    <nx:WindowPlacementService
        FileName="MainWindow"
        DirectoryName="{Binding EnvironmentService.SettingsDirectory, FallbackValue={x:Null}}"/>
  </nx:Interaction.Behaviors>
  <!-- ... your content -->
</Window>
```
**ViewModel (show a dialog with a View and a ViewModel):**
```csharp
private IAsyncDialogService Dialogs => GetRequiredService<IAsyncDialogService>("Dialogs");

public async Task EditAsync(MyModel myModel, CancellationToken ct)
{
    await using var vm = new EditViewModel();
    var result = await Dialogs.ShowDialogAsync(
        MessageBoxButton.OKCancel,
        title: "Edit",
        documentType: "EditView",
        viewModel: vm,
        parentViewModel: this,
        parameter: myModel,
        cancellationToken: ct);

    if (result != MessageBoxResult.OK) return;
    // proceed with the edited model
}
```
> WindowPlacementService is fully automatic once attached: it restores on load and saves on close—no extra code needed.

### 2) Documents as windows (ID‑based reuse + per‑window behaviors)

**XAML (window manager + per‑window template behaviors):**
```xml
<nx:Interaction.Behaviors>
  <nx:WindowedDocumentService x:Name="Windows"
                              ActiveDocument="{Binding ActiveWindow}"
                              FallbackViewType="{x:Type views:ErrorView}">
    <nx:WindowedDocumentService.WindowStyle>
      <Style TargetType="{x:Type Window}">
        <Setter Property="nx:Interaction.BehaviorsTemplate">
          <Setter.Value>
            <DataTemplate>
              <ItemsControl>
                <nx:WindowService Name="CurrentWindowService"
                                  ClosingCommand="{Binding ClosingCommand}"/>
                <nx:EventToCommand EventName="ContentRendered"
                                   Command="{Binding ContentRenderedCommand}"/>
              </ItemsControl>
            </DataTemplate>
          </Setter.Value>
        </Setter>
      </Style>
    </nx:WindowedDocumentService.WindowStyle>
  </nx:WindowedDocumentService>
</nx:Interaction.Behaviors>
```
**ViewModel (find by ID or create; dispose ViewModel on close):**
```csharp
public IAsyncDocumentManagerService WindowManager =>
    GetRequiredService<IAsyncDocumentManagerService>("Windows");

public async Task OpenInWindowAsync(MyModel myModel, CancellationToken ct)
{
    var doc = await WindowManager.FindDocumentByIdOrCreateAsync(
        id: myModel.Id,
        documentType: "MyModelView",
        viewModelFactory: static () => new MyModelViewModel(),
        parentViewModel: this,
        parameter: myModel,
        cancellationToken: ct);

    doc.Show();
}
```

### 3) Parent–child helper (sticky parent binding)

Use `ViewModelExtensions.ParentViewModel` to set a parent ViewModel on child ViewModels directly in XAML; enable `StickyParentBinding="True"` to keep it in sync on every `DataContext` change.

```xml
<ContentPresenter
  xmlns:nx="http://schemas.nuext.minimal/xaml"
  nx:ViewModelExtensions.ParentViewModel="{Binding}"
  nx:ViewModelExtensions.StickyParentBinding="True"
  Content="{Binding ChildVm}"/>
```

### 4) Multi‑threaded WPF (per‑thread dispatcher, window‑oriented services)

Each UI thread has its own `DispatcherService` instance injected via behaviors; window‑oriented services (`ViewWindowService` / `WindowedDocumentService`) are dispatcher‑aware and safe to call on the owning thread.

## Samples

- **[WpfMinimalApp](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/tree/main/samples/WpfMinimalApp)** — minimal View, ViewModel, command, and dialog workflow.
- **[WpfAppSample](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/tree/main/samples/WpfAppSample)** — dialogs, settings, tabbed and windowed documents, View resolution, and application services.
- **[WpfMultiThreadedApp](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/tree/main/samples/WpfMultiThreadedApp)** — multiple WPF UI threads, dispatcher-local services, runtime behavior composition, and coordinated shutdown.

## Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm.Wpf
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Wpf`.
3. Click "Install".

### Recommended companions

- [`NuExt.Minimal.Mvvm.SourceGenerator`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) removes ViewModel boilerplate at compile time.
- [`NuExt.Minimal.Mvvm.MahApps.Metro`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro) adds integration with [MahApps.Metro](https://github.com/MahApps/MahApps.Metro).

## Ecosystem

- [NuExt.Minimal.Mvvm](https://github.com/nu-ext/NuExt.Minimal.Mvvm)
- [NuExt.Minimal.Mvvm.SourceGenerator](https://github.com/nu-ext/NuExt.Minimal.Mvvm.SourceGenerator)
- [NuExt.Minimal.Behaviors.Wpf](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf)
- [NuExt.Presentation.Wpf](https://github.com/nu-ext/NuExt.Presentation.Wpf)
- [NuExt.Minimal.Mvvm.MahApps.Metro](https://github.com/nu-ext/NuExt.Minimal.Mvvm.MahApps.Metro)
- [NuExt.System](https://github.com/nu-ext/NuExt.System)
- [NuExt.System.Data](https://github.com/nu-ext/NuExt.System.Data)
- [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite)

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## Acknowledgements

The DevExpress MVVM Framework has been a long‑time source of inspiration. **NuExt.Minimal.Mvvm.Wpf** distills similar ideas into a lightweight, async‑first, and explicit composition model tailored for contemporary WPF apps.

## License

MIT. See LICENSE.