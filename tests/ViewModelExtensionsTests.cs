using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf.Tests;

[TestClass]
public sealed class ViewModelExtensionsTests
{
    // ---------- Helpers ----------

    internal sealed class TestChildViewModel : IParentedViewModel
    {
        public int AssignCount { get; private set; }

        public object? ParentViewModel
        {
            get;
            set
            {
                if (!ReferenceEquals(field, value))
                {
                    field = value;
                    AssignCount++;
                }
            }
        }
    }

    internal sealed class TestParentViewModel : IParentedViewModel
    {
        public object? ParentViewModel { get; set; }
    }

    private static void RaiseLoaded(FrameworkElement fe)
        => fe.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void RaiseUnloaded(FrameworkElement fe)
        => fe.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

    // ---------- One-shot (immediate DC) ----------

    [STATestMethod]
    public void OneShot_Assigns_WhenDataContextIsPresent_And_DoesNotReassignOnLaterChanges()
    {
        var parent = new TestParentViewModel();
        var child1 = new TestChildViewModel();
        var child2 = new TestChildViewModel();

        var view = new Grid { DataContext = child1 };

        // one-shot (Sticky=false by default)
        ViewModelExtensions.SetParentViewModel(view, parent);

        Assert.AreSame(parent, child1.ParentViewModel);
        Assert.AreEqual(1, child1.AssignCount);

        // Change DataContext to another VM -> one-shot shouldn't assign anymore
        view.DataContext = child2;

        Assert.IsNull(child2.ParentViewModel);
        Assert.AreEqual(0, child2.AssignCount);

        // Re-assigning the same parent explicitly does not count as another assignment
        ViewModelExtensions.SetParentViewModel(view, parent);
        Assert.IsNull(child2.ParentViewModel);
        Assert.AreEqual(1, child1.AssignCount);
    }

    // ---------- One-shot (deferred until DC appears) ----------

    [STATestMethod]
    public void OneShot_Assigns_WhenDataContextBecomesAvailable_Once()
    {
        var parent = new TestParentViewModel();
        var child1 = new TestChildViewModel();
        var child2 = new TestChildViewModel();

        var view = new Grid(); // no DataContext yet

        ViewModelExtensions.SetParentViewModel(view, parent);
        // No assignment yet
        Assert.IsNull(child1.ParentViewModel);

        // First DC appears -> assignment happens once
        view.DataContext = child1;
        Assert.AreSame(parent, child1.ParentViewModel);
        Assert.AreEqual(1, child1.AssignCount);

        // Next DC change in one-shot mode -> should not assign
        view.DataContext = child2;
        Assert.IsNull(child2.ParentViewModel);
        Assert.AreEqual(0, child2.AssignCount);
    }

    // ---------- Sticky: reassign on every DC change + lifecycle ----------

    [STATestMethod]
    public void Sticky_Reassigns_OnEachChange_And_Respects_Loaded_Unloaded()
    {
        var parent = new TestParentViewModel();
        var c1 = new TestChildViewModel();
        var c2 = new TestChildViewModel();
        var c3 = new TestChildViewModel();

        var view = new Grid();

        // Enable sticky first (order doesn't matter for our impl)
        ViewModelExtensions.SetStickyParentBinding(view, true);
        ViewModelExtensions.SetParentViewModel(view, parent);

        // Simulate element entering visual tree
        RaiseLoaded(view);

        // First DC
        view.DataContext = c1;
        Assert.AreSame(parent, c1.ParentViewModel);
        Assert.AreEqual(1, c1.AssignCount);

        // Change DC -> sticky must re-assign to new VM
        view.DataContext = c2;
        Assert.AreSame(parent, c2.ParentViewModel);
        Assert.AreEqual(1, c2.AssignCount);

        // Simulate leaving visual tree -> detach DC handler
        RaiseUnloaded(view);

        // Change DC while unloaded -> must NOT assign
        view.DataContext = c3;
        Assert.IsNull(c3.ParentViewModel);
        Assert.AreEqual(0, c3.AssignCount);

        // Loaded again -> subscription restored; next change should assign
        RaiseLoaded(view);
        var c4 = new TestChildViewModel();
        view.DataContext = c4;
        Assert.AreSame(parent, c4.ParentViewModel);
        Assert.AreEqual(1, c4.AssignCount);
    }

    // ---------- ContentPresenter: Content preferred over DataContext ----------

    [STATestMethod]
    public void ContentPresenter_Prefers_Content_Over_DataContext_ForInitialApply()
    {
        var parent = new TestParentViewModel();
        var contentVm = new TestChildViewModel();
        var dcVm = new TestChildViewModel();

        var cp = new ContentPresenter
        {
            // Both are set; implementation should prefer Content
            Content = contentVm,
            DataContext = dcVm
        };

        ViewModelExtensions.SetParentViewModel(cp, parent);
        Assert.AreSame(parent, contentVm.ParentViewModel);
        Assert.AreEqual(1, contentVm.AssignCount);
        Assert.IsNull(dcVm.ParentViewModel);
        Assert.AreEqual(0, dcVm.AssignCount);
    }

    // ---------- Cycle guard ----------

    [STATestMethod]
    public void Cycle_IsIgnored_And_DoesNotAssign()
    {
        var parent = new TestParentViewModel();
        var child = new TestChildViewModel();

        // Create cycle: parent -> child
        parent.ParentViewModel = child;

        var view = new Grid { DataContext = child };
        ViewModelExtensions.SetParentViewModel(view, parent);

        // Guard must detect cycle and skip assignment
        Assert.IsNull(child.ParentViewModel);
        Assert.AreEqual(0, child.AssignCount);
    }

    // ---------- Idempotent set: same parent doesn't increment ----------

    [STATestMethod]
    public void Idempotent_SameParent_DoesNotIncrementAssignCount()
    {
        var parent = new TestParentViewModel();
        var child = new TestChildViewModel();
        var view = new Grid { DataContext = child };

        ViewModelExtensions.SetParentViewModel(view, parent);
        Assert.AreEqual(1, child.AssignCount);

        // Same parent again -> no increment
        ViewModelExtensions.SetParentViewModel(view, parent);
        Assert.AreEqual(1, child.AssignCount);
    }
}
