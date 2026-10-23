using Presentation.Wpf;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// An abstract base class that provides services related to views in an MVVM framework.
/// Provides view creation, template selection, and locator services for view models.
/// </summary>
/// <remarks>
/// <para>
/// This class is Dispatcher-aware and handles thread marshaling automatically.
/// All UI-related operations are guaranteed to execute on the appropriate thread.
/// </para>
/// <para>
/// The view resolution order is: 
/// 1. <see cref="ViewTemplate"/> (if accessed from the calling thread and set), 
/// 2. <see cref="ViewTemplateKey"/> (if set and found in resources),
/// 3. <see cref="ViewTemplateSelector"/> (if set),
/// 4. <see cref="ViewLocator"/> (default or custom).
/// </para>
/// </remarks>
public abstract class ViewServiceBase : ServiceBase<Control>, IDispatcherObject
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="ViewLocator"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewLocatorProperty = DependencyProperty.Register(
        nameof(ViewLocator), typeof(ViewLocatorBase), typeof(ViewServiceBase),
        new PropertyMetadata(null, (d, e) => ((ViewServiceBase)d).OnViewLocatorChanged((ViewLocatorBase?)e.OldValue, (ViewLocatorBase?)e.NewValue)));

    /// <summary>
    /// Identifies the <see cref="ViewTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewTemplateProperty = DependencyProperty.Register(
        nameof(ViewTemplate), typeof(DataTemplate), typeof(ViewServiceBase));

    /// <summary>
    /// Identifies the <see cref="ViewTemplateKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewTemplateKeyProperty = DependencyProperty.Register(
        nameof(ViewTemplateKey), typeof(string), typeof(ViewServiceBase));

    /// <summary>
    /// Identifies the <see cref="ViewTemplateSelector"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewTemplateSelectorProperty = DependencyProperty.Register(
        nameof(ViewTemplateSelector), typeof(DataTemplateSelector), typeof(ViewServiceBase));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the view locator used to locate views for the view models.
    /// </summary>
    /// <value>
    /// The view locator instance, or <see langword="null"/> to use the default locator.
    /// </value>
    public ViewLocatorBase? ViewLocator
    {
        get => (ViewLocatorBase?)GetValue(ViewLocatorProperty);
        set => SetValue(ViewLocatorProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template used for the views.
    /// </summary>
    public DataTemplate? ViewTemplate
    {
        get => (DataTemplate?)GetValue(ViewTemplateProperty);
        set => SetValue(ViewTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the resource key for the data template used for the views.
    /// This key is used to look up the template in the resource dictionary.
    /// </summary>
    public string? ViewTemplateKey
    {
        get => (string?)GetValue(ViewTemplateKeyProperty);
        set => SetValue(ViewTemplateKeyProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template selector used to select the appropriate data template for the views.
    /// </summary>
    public DataTemplateSelector? ViewTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ViewTemplateSelectorProperty);
        set => SetValue(ViewTemplateSelectorProperty, value);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the <see cref="ViewLocator"/> property changes.
    /// </summary>
    /// <param name="oldValue">The old value of the view locator.</param>
    /// <param name="newValue">The new value of the view locator.</param>
    protected virtual void OnViewLocatorChanged(ViewLocatorBase? oldValue, ViewLocatorBase? newValue)
    {

    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a view asynchronously based on the specified parameters.
    /// </summary>
    /// <param name="viewName">The name of the view to create the view for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created view object.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the cancellation token.</exception>
    protected virtual ValueTask<object> CreateViewAsync(string? viewName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<object>(cancellationToken);
        }

        DataTemplate? viewTemplate;
        DataTemplateSelector? viewTemplateSelector;
        if (CheckAccess())
        {
            viewTemplate = ViewTemplate ?? GetViewTemplateByKey(AssociatedObject, ViewTemplateKey);
            viewTemplateSelector = ViewTemplateSelector;
        }
        else
        {
            (var associatedObject, var viewTemplateKey, viewTemplateSelector) = this.GetPropertySafe(() => (AssociatedObject, ViewTemplateKey, ViewTemplateSelector));
            viewTemplate = GetViewTemplateByKey(associatedObject, viewTemplateKey);
        }

        if (viewTemplate != null || viewTemplateSelector != null)
        {
            Debug.Assert(viewTemplate == null || viewTemplate.CheckAccess());
            return ValueTask.FromResult<object>(new ContentPresenter()
            {
                ContentTemplate = viewTemplate,
                ContentTemplateSelector = viewTemplateSelector
            });
        }

        return GetViewLocator().GetOrCreateViewAsync(viewName, cancellationToken);
    }

    /// <summary>
    /// Gets the view locator instance, ensuring thread-safe access.
    /// </summary>
    /// <returns>The view locator instance.</returns>
    protected ViewLocatorBase GetViewLocator()
    {
        if (CheckAccess())
        {
            return GetViewLocatorCore();
        }

        return Dispatcher.Invoke(GetViewLocatorCore, DispatcherPriority.Send);
    }

    /// <summary>
    /// Gets the view locator instance. Returns the default locator if no custom locator is set.
    /// </summary>
    /// <returns>The view locator instance. Guaranteed to be non-null (returns <see cref="Wpf.ViewLocator.Default"/> if no custom locator is set).</returns>
    protected virtual ViewLocatorBase GetViewLocatorCore()
    {
        Debug.Assert(CheckAccess());
        VerifyAccess();
        return ViewLocator ?? Wpf.ViewLocator.Default;
    }

    private static DataTemplate? GetViewTemplateByKey(FrameworkElement? element, string? resourceKey)
    {
        if (element == null || string.IsNullOrEmpty(resourceKey))
        {
            return null;
        }
        DataTemplate? dataTemplate;
        if (element.CheckAccess())
        {
            dataTemplate = element.TryFindResource(resourceKey) as DataTemplate;
        }
        else
        {
            var lockObj = LockPool.Get(element);
            lock (lockObj)
            {
                dataTemplate = element.TryFindResource(resourceKey) as DataTemplate;
            }
        }
        Trace.WriteLineIf(dataTemplate == null, $"DataTemplate with key '{resourceKey}' not found.");
        Debug.Assert(dataTemplate?.CheckAccess() == true, $"DataTemplate with key '{resourceKey}' not found or owned by other thread.");
        return dataTemplate;
    }

    #endregion
}
