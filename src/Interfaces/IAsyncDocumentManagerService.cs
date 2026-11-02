using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Represents the event arguments for the ActiveDocumentChanged event.
/// </summary>
/// <param name="OldDocument">The previously active document.</param>
/// <param name="NewDocument">The newly active document.</param>
public readonly record struct ActiveDocumentChangedEventArgs(IAsyncDocument? OldDocument, IAsyncDocument? NewDocument);

/// <summary>
/// Defines an interface for managing asynchronous documents.
/// </summary>
public interface IAsyncDocumentManagerService : INamedService
{
    /// <summary>
    /// Creates a new asynchronous document.
    /// </summary>
    /// <param name="documentType">The type of the document.</param>
    /// <param name="viewModel">The view model associated with the document.</param>
    /// <param name="parentViewModel">The parent view model of the document.</param>
    /// <param name="parameter">Additional parameters for creating the document.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the initialization process.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the created asynchronous document.
    /// </returns>
    /// <remarks>
    /// Ownership of the view model is transferred to the document only after the document is created successfully.
    /// The document disposes the view model when it is closed if <see cref="IAsyncDocument.DisposeOnClose"/> is set to <see langword="true"/>.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
    ValueTask<IAsyncDocument> CreateDocumentAsync(string? documentType, object? viewModel, object? parentViewModel,
        object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the currently active document.
    /// </summary>
    IAsyncDocument? ActiveDocument { get; set; }

    /// <summary>
    /// Occurs when the active document has changed.
    /// </summary>
    event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanged;

    /// <summary>
    /// Gets the count of documents.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a collection of all open asynchronous documents.
    /// </summary>
    IEnumerable<IAsyncDocument> Documents { get; }

    /// <summary>
    /// Closes all documents asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask CloseAllAsync();
}
