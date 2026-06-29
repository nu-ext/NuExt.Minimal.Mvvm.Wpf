# NuExt.Minimal.Mvvm.Wpf

`NuExt.Minimal.Mvvm.Wpf` is a **WPF add‑on** for the lightweight MVVM core ([NuExt.Minimal.Mvvm](https://github.com/nu-ext/NuExt.Minimal.Mvvm)). It delivers **deterministic async UX**, **predictable window and document services**, **parent–child view‑model patterns**, and **control/window‑oriented APIs**—with minimal ceremony and zero heavy dependencies.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm.Wpf?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf)

---

## Highlights

- **Parent–child and control/window‑oriented VMs**  
  `ControlViewModel`/`WindowViewModel` model the UI the way WPF actually runs it: *parent–child relations*, deterministic event‑to‑command bindings via attached behaviors, and **dispatcher‑safe** operations via `IDispatcherService` (each UI thread owns its own dispatcher).

- **Explicit view composition**  
  Predictable view resolution with overridable conventions and a safe fallback view. Use templates when you have them, or a customizable `ViewLocator` when you don’t have one. See **View location** below for details.

- **Async‑first UX**  
  Dialogs and documents are orchestrated with proper async lifecycle: view creation, VM initialization, cancellation, and clean completion—*without UI deadlocks*. `IAsyncDialogService` shows modal dialogs with typed commands/results and optional validation. `IAsyncDocumentManagerService` manages the creation/activation/closure of documents that live as windows or tabs.

- **Deterministic windows and documents**  
  Restore/save window position/size/state (`WindowPlacementService`). Manage **documents as windows** (`WindowedDocumentService`) or **as tabs** (`TabbedDocumentService`) using a unified `IAsyncDocument` contract (ID, title, `Show/Hide/CloseAsync`, “dispose VM on close”, optional “hide instead of close”).

- **Multi‑threaded WPF ready**  
  Works in multi‑UI‑thread apps (each window on its own dispatcher). `DispatcherService` is injected per thread; window‑oriented services are dispatcher‑aware by design.

- **Minimal deps, performance‑oriented**  
  No heavy external frameworks. Hot paths avoid needless allocations, lifecycle is explicit, and services/events are carefully cleaned up.

- **Compatibility:**  
  WPF on **.NET 8/9/10** and **.NET Framework 4.6.2+**. Works with any MVVM stack and pairs naturally with [`NuExt.Minimal.Mvvm`](https://github.com/nu-ext/NuExt.Minimal.Mvvm).

---

## Key concepts

- **Control/Window‑oriented VMs + DispatcherService** — VMs expose a *predictable* async lifecycle and run code on the owning UI thread via `IDispatcherService`. This is why the same patterns also work in **multi‑threaded WPF**, where each window has its own dispatcher.

- **IAsyncDocument + IAsyncDocumentManagerService** — a document is a *hosted view + VM* with a stable contract (`Id`, `Title`, `Show/Hide/CloseAsync`, optional “dispose VM on close”). The **manager** creates documents (`CreateDocumentAsync`), tracks `ActiveDocument`, enumerates `Documents`, supports bulk `CloseAllAsync`, and plugs into WPF as **windows** (`WindowedDocumentService`) or **tabs** (`TabbedDocumentService`).

- **IAsyncDialogService** — shows modal dialogs with a view resolved by name/template and a view‑model you provide; returns either a `MessageBoxResult` or a selected `UICommand`. The service handles view creation, optional title binding, validation (via `IDataErrorInfo` / `INotifyDataErrorInfo`), and clean teardown.

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
When no view is found (or view creation fails), a lightweight `FallbackView` is created with a readable error text. Override `ViewLocatorBase` or assign `ViewLocator.Default` if you need full control over lookup rules. If you need to alter caches or registrations during long‑running sessions, the built‑in locator also exposes `ClearCache()` and `ClearRegisteredTypes()`.

## Windowed vs ViewWindowService

**When to use which:**
- Use `WindowedDocumentService` when you manage multiple long‑lived windows as **documents** (ID reuse, ActiveDocument, bulk close).
- Use `ViewWindowService` when you occasionally **show a view as a window** (modal/non‑modal) without keeping a document roster.

---

## Quick Start (practical, minimal)

### 1) Async dialog with validation + automatic window placement

**XAML (attach services to your Window via behaviors):**
```xml
<Window
  ...
  xmlns:nx="http://schemas.nuext.minimal/xaml">
  <nx:Interaction.Behaviors>
    <nx:WindowService/>
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
**ViewModel (show a dialog with a view and a VM):**
```csharp
private IAsyncDialogService Dialogs => GetService<IAsyncDialogService>("Dialogs");

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
                <nx:WindowService Name="CurrentWindowService"/>
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
**ViewModel (find by ID or create; dispose VM on close):**
```csharp
public IAsyncDocumentManagerService WindowManager => GetService<IAsyncDocumentManagerService>("Windows");

public async Task OpenInWindowAsync(MyModel myModel, CancellationToken ct)
{
    var doc = await WindowManager.FindDocumentByIdOrCreateAsync(
        id: myModel.Id,
        createDocumentCallback: async mgr =>
        {
            var vm = new MyModelViewModel();
            try
            {
                var d = await mgr.CreateDocumentAsync(
                    documentType: "MyModelView",
                    viewModel: vm,
                    parentViewModel: this,
                    parameter: myModel,
                    cancellationToken: ct);
                d.DisposeOnClose = true;
                return d;
            }
            catch
            {
                await vm.DisposeAsync();
                throw;
            }
        });

    doc.Show();
}
```

### 3) **Parent–child helper (sticky parent binding)**

Use `ViewModelExtensions.ParentViewModel` to set a parent VM on child view models directly in XAML; enable `StickyParentBinding="True"` to keep it in sync on every `DataContext` change.

```xml
<ContentPresenter
  xmlns:nx="http://schemas.nuext.minimal/xaml"
  nx:ViewModelExtensions.ParentViewModel="{Binding}"
  nx:ViewModelExtensions.StickyParentBinding="True"
  Content="{Binding ChildVm}"/>
```

### 4) Multi‑threaded WPF (per‑thread dispatcher, window‑oriented services)

Each UI thread has its own `DispatcherService` instance injected via behaviors; window‑oriented services (`ViewWindowService` / `WindowedDocumentService`) are dispatcher‑aware and safe to call on the owning thread.
See the runnable samples:

- **Multi‑threaded app**: [WpfMultiThreadedApp](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/tree/main/samples/WpfMultiThreadedApp)
- **Basic app (services/commands/VMs)**: [WpfAppSample](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf/tree/main/samples/WpfAppSample)

---

## Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm.Wpf
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Wpf`.
3. Click "Install".

**Nice to have**: [NuExt.Minimal.Mvvm.SourceGenerator](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) to remove boilerplate in view‑models.
Also see [NuExt.Minimal.Mvvm.MahApps.Metro](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro) - a NuGet package that provides extensions for integrating with [MahApps.Metro](https://github.com/MahApps/MahApps.Metro).

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