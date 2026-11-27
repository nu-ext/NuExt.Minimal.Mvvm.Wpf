using System.Windows;
using System.Windows.Controls;
using Minimal.Mvvm;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class ServiceBaseTests
{
    private const string ServiceName = "Test";

    /// <summary>
    /// Exposes the registration state, which is <see langword="protected"/> in the base class.
    /// </summary>
    private sealed class TestService : ServiceBase<FrameworkElement>
    {
        public bool IsRegisteredForTest => IsRegisteredInViewModel;
    }

    private sealed class TestViewModel : IServiceContainerProvider
    {
        public IServiceContainer Services { get; } = new ServiceProvider();
    }

    // ---------- Helpers ----------

    private static void RaiseLoaded(FrameworkElement element)
        => element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void RaiseUnloaded(FrameworkElement element)
        => element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

    private static object? Resolve(TestViewModel viewModel, string name = ServiceName)
        => viewModel.Services.GetService(typeof(TestService), name, localOnly: true);

    private static (Grid View, TestService Service) Attach(TestViewModel viewModel, bool unregisterOnUnloaded = false)
    {
        var view = new Grid { DataContext = viewModel };
        var service = new TestService
        {
            Name = ServiceName,
            ShouldUnregisterOnUnloaded = unregisterOnUnloaded
        };
        service.Attach(view);
        return (view, service);
    }

    // ---------- Registration ----------

    [STATestMethod]
    public void Attach_RegistersServiceInViewModelContainer()
    {
        var viewModel = new TestViewModel();

        var (_, service) = Attach(viewModel);

        Assert.AreSame(service, Resolve(viewModel));
        Assert.IsTrue(service.IsRegisteredForTest);
    }

    [STATestMethod]
    public void RecreatedView_ReplacesPreviousRegistration_WithoutException()
    {
        var viewModel = new TestViewModel();
        var (_, first) = Attach(viewModel);
        var (_, second) = Attach(viewModel);

        Assert.AreSame(second, Resolve(viewModel), "The newest instance must own the slot.");
        Assert.AreNotSame(first, Resolve(viewModel));
    }

    [STATestMethod]
    public void Detaching_UnregistersService()
    {
        var viewModel = new TestViewModel();
        var (_, service) = Attach(viewModel);

        service.Detach();

        Assert.IsNull(Resolve(viewModel));
        Assert.IsFalse(service.IsRegisteredForTest);
    }

    [STATestMethod]
    public void Renaming_ChangesTheRegistrationKey()
    {
        var viewModel = new TestViewModel();
        var (_, service) = Attach(viewModel);

        service.Name = "Renamed";

        Assert.IsNull(Resolve(viewModel), "The old key must be released.");
        Assert.AreSame(service, Resolve(viewModel, "Renamed"));
    }

    // ---------- Loaded/Unloaded ----------

    [STATestMethod]
    public void UnloadedWithoutLoaded_IsIgnored()
    {
        var viewModel = new TestViewModel();
        var (view, service) = Attach(viewModel, unregisterOnUnloaded: true);

        RaiseUnloaded(view);

        Assert.AreSame(service, Resolve(viewModel), "A spurious Unloaded must not unregister the service.");
    }

    [STATestMethod]
    public void UnloadedAfterLoaded_Unregisters_AndLoadedRegistersBack()
    {
        var viewModel = new TestViewModel();
        var (view, service) = Attach(viewModel, unregisterOnUnloaded: true);

        RaiseLoaded(view);
        Assert.AreSame(service, Resolve(viewModel));

        RaiseUnloaded(view);
        Assert.IsNull(Resolve(viewModel));

        RaiseLoaded(view);
        Assert.AreSame(service, Resolve(viewModel));
    }

    [STATestMethod]
    public void DuplicatedLoaded_DoesNotThrow()
    {
        var viewModel = new TestViewModel();
        var (view, service) = Attach(viewModel, unregisterOnUnloaded: true);

        RaiseLoaded(view);
        RaiseLoaded(view);

        Assert.AreSame(service, Resolve(viewModel));
    }

    [STATestMethod]
    public void UnregisterOnUnloadedDisabled_KeepsRegistration()
    {
        var viewModel = new TestViewModel();
        var (view, service) = Attach(viewModel);

        RaiseLoaded(view);
        RaiseUnloaded(view);

        Assert.AreSame(service, Resolve(viewModel), "Without opt-in the service must stay registered.");
    }

    // ---------- Service disabled for registration ----------

    [STATestMethod]
    public void ShouldRegisterInViewModelFalse_DoesNotRegister()
    {
        var viewModel = new TestViewModel();
        var view = new Grid { DataContext = viewModel };
        var service = new TestService { Name = ServiceName, ShouldRegisterInViewModel = false };

        service.Attach(view);

        Assert.IsNull(Resolve(viewModel));
        Assert.IsFalse(service.IsRegisteredForTest);
    }
}
