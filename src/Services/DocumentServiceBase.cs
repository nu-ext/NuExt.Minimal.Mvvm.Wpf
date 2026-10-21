using Presentation.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides a base class for services that manage documents associated with UI elements.
/// </summary>
public abstract class DocumentServiceBase : ViewServiceBase
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="ActiveDocument"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ActiveDocumentProperty = DependencyProperty.Register(
        nameof(ActiveDocument), typeof(IAsyncDocument), typeof(DocumentServiceBase), 
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
            (d, e) => ((DocumentServiceBase)d).OnActiveDocumentChanged(e.OldValue as IAsyncDocument, e.NewValue as IAsyncDocument)));

    /// <summary>
    /// Identifies the Document attached dependency property.
    /// This property is used to associate an asynchronous document (IAsyncDocument) with a DependencyObject.
    /// </summary>
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.RegisterAttached(
        "Document", typeof(IAsyncDocument), typeof(DocumentServiceBase), new PropertyMetadata(null, 
            (d, e) => OnDocumentChanged(d, (IAsyncDocument?)e.OldValue, (IAsyncDocument?)e.NewValue)));

    /// <summary>
    /// Identifies the <see cref="FallbackViewType"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FallbackViewTypeProperty = DependencyProperty.Register(
        nameof(FallbackViewType), typeof(Type), typeof(DocumentServiceBase));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the currently active document.
    /// </summary>
    /// <value>
    /// The active <see cref="IAsyncDocument"/> instance, or <see langword="null"/> if no document is active.
    /// </value>
    public IAsyncDocument? ActiveDocument
    {
        get => (IAsyncDocument)GetValue(ActiveDocumentProperty);
        set => SetValue(ActiveDocumentProperty, value);
    }

    /// <summary>
    /// Gets or sets the type of the fallback View to create when no document type or View template is specified.
    /// </summary>
    public Type? FallbackViewType
    {
        get => (Type)GetValue(FallbackViewTypeProperty);
        set => SetValue(FallbackViewTypeProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the <see cref="ActiveDocument"/> property value changes.
    /// </summary>
    public event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanged;

    #endregion

    #region Event Handlers

    /// <summary>
    /// Raises the <see cref="ActiveDocumentChanged"/> event.
    /// </summary>
    /// <param name="oldValue">The previously active document, or <see langword="null"/>.</param>
    /// <param name="newValue">The newly active document, or <see langword="null"/>.</param>
    protected virtual void OnActiveDocumentChanged(IAsyncDocument? oldValue, IAsyncDocument? newValue)
    {
        Debug.Assert(CheckAccess());
        ActiveDocumentChanged?.Invoke(this, new ActiveDocumentChangedEventArgs(oldValue, newValue));
    }

    /// <summary>
    /// Handles changes to the Document attached property.
    /// </summary>
    /// <param name="element">The element on which the property was changed.</param>
    /// <param name="oldDocument">The old value of the Document property.</param>
    /// <param name="newDocument">The new value of the Document property.</param>
    private static void OnDocumentChanged(DependencyObject element, IAsyncDocument? oldDocument, IAsyncDocument? newDocument)
    {
        var doc = GetDocument(element);
        Debug.Assert(doc == newDocument);
        _ = oldDocument;
    }

    #endregion

    #region Dependency Methods

    /// <summary>
    /// Gets the value of the Document attached property from a specified DependencyObject.
    /// </summary>
    /// <param name="element">The DependencyObject from which to read the value.</param>
    /// <returns>The current value of the Document attached property.</returns>
    public static IAsyncDocument? GetDocument(DependencyObject element)
    {
        return (IAsyncDocument?)element.GetValue(DocumentProperty);
    }

    /// <summary>
    /// Sets the value of the Document attached property on a specified DependencyObject.
    /// </summary>
    /// <param name="element">The DependencyObject on which to set the value.</param>
    /// <param name="value">The new value for the Document attached property.</param>
    public static void SetDocument(DependencyObject element, IAsyncDocument? value)
    {
        element.SetValue(DocumentProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates an instance of the view type specified by <see cref="FallbackViewType"/> using its parameterless constructor.
    /// </summary>
    /// <returns>A new view instance or <see langword="null"/> if no type is specified.</returns>
    /// <remarks>
    /// This method is invoked when no document type, View template, or View template selector is specified.
    /// The specified type must have a public parameterless constructor.
    /// </remarks>
    protected virtual object? CreateFallbackView()
    {
        var fallbackViewType = CheckAccess() ? FallbackViewType : this.GetPropertySafe(() => FallbackViewType);
        return fallbackViewType == null ? null : Activator.CreateInstance(fallbackViewType);
    }

    /// <inheritdoc/>
    protected override ValueTask<object> CreateViewAsync(string? documentType, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<object>(cancellationToken);
        }

        bool hasNoViewTemplate = CheckAccess() 
            ? ViewTemplate == null && ViewTemplateKey == null && ViewTemplateSelector == null 
            : this.GetPropertySafe(() => ViewTemplate == null && ViewTemplateKey == null && ViewTemplateSelector == null);

        if (documentType == null && hasNoViewTemplate)
        {
            try
            {
                var view = CreateFallbackView();
                return view != null ? ValueTask.FromResult(view) : GetViewLocator().GetOrCreateViewAsync(documentType, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTask.FromException<object>(ex);
            }
        }
        return base.CreateViewAsync(documentType, cancellationToken);
    }

    #endregion
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs ActiveDocumentPropertyChanged = new(nameof(IAsyncDocumentManagerService.ActiveDocument));
    internal static readonly PropertyChangedEventArgs CountPropertyChanged = new(nameof(IAsyncDocumentManagerService.Count));
    internal static readonly PropertyChangedEventArgs TitlePropertyChanged = new(nameof(IAsyncDocument.Title));
}
