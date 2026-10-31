using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Base class for ViewModels that represent document content and support
/// asynchronous close validation.
/// </summary>
public abstract partial class DocumentContentViewModelBase : ControlViewModelBase, IAsyncDocumentContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentContentViewModelBase"/> class.
    /// </summary>
    protected DocumentContentViewModelBase() : this(fallbackServices: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentContentViewModelBase"/> class
    /// with an optional fallback service container.
    /// </summary>
    /// <param name="fallbackServices">
    /// The fallback service container used when local and parent service resolution fails;
    /// or <see langword="null"/> if no fallback container is available.
    /// </param>
    protected DocumentContentViewModelBase(IServiceContainer? fallbackServices) : base(fallbackServices)
    {
    }

    #region Properties

    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    [Notify(CallbackName = nameof(OnTitleChanged))]
    public partial string Title { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// Occurs when the <see cref="Title"/> property changes, allowing derived classes to respond to title updates.
    /// </summary>
    protected virtual void OnTitleChanged()
    {
        DisplayName ??= Title;
    }

    /// <summary>
    /// Determines asynchronously whether the document can be closed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A value task whose result is <see langword="true"/> if the document can be closed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Override this method to provide document-specific close validation.
    /// The default implementation allows the document to close unless the operation
    /// has already been canceled.
    /// </remarks>
    public virtual ValueTask<bool> CanCloseAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellationToken) : ValueTask.FromResult(true);
    }

    #endregion
}