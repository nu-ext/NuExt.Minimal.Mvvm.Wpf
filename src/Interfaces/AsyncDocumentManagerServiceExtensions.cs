using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides extension methods for the <see cref="IAsyncDocumentManagerService"/>.
/// </summary>
public static class AsyncDocumentManagerServiceExtensions
{
    /// <summary>
    /// Finds an asynchronous document by its ID.
    /// </summary>
    /// <param name="service">The document manager service to search in.</param>
    /// <param name="id">The ID of the document to find.</param>
    /// <returns>The found asynchronous document, or <see langword="null"/> if no document with the specified ID is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is null.</exception>
    public static IAsyncDocument? FindDocumentById(this IAsyncDocumentManagerService service, object id)
    {
        Throw.IfNull(service);
        return service.Documents.FirstOrDefault(x => Equals(x.Id, id));
    }

    /// <summary>
    /// Finds an asynchronous document by its ID, or creates a new document if it does not exist.
    /// </summary>
    /// <param name="service">The document manager service to search in.</param>
    /// <param name="id">The ID of the document to find or create.</param>
    /// <param name="createDocumentCallback">The callback function to create a new document if none is found.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the found or newly created asynchronous document.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="createDocumentCallback"/> is null.</exception>
    public static async ValueTask<IAsyncDocument> FindDocumentByIdOrCreateAsync(
        this IAsyncDocumentManagerService service,
        object id,
        Func<IAsyncDocumentManagerService, ValueTask<IAsyncDocument>> createDocumentCallback)
    {
        Throw.IfNull(service);
        Throw.IfNull(createDocumentCallback);
        IAsyncDocument? document = service.FindDocumentById(id);
        if (document == null)
        {
            document = await createDocumentCallback(service).ConfigureAwait(false);
            document.Id = id;
        }
        return document;
    }

    /// <summary>
    /// Finds a document by its ID, or creates one with a new ViewModel if it does not exist.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the document ViewModel.</typeparam>
    /// <param name="service">The document manager service.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="documentType">The name of the View to create.</param>
    /// <param name="viewModelFactory">The factory that creates the document ViewModel.</param>
    /// <param name="parentViewModel">The parent ViewModel.</param>
    /// <param name="parameter">The initialization parameter.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The existing or newly created document.</returns>
    /// <remarks>
    /// A newly created ViewModel remains owned by this operation until document creation succeeds.
    /// The document takes ownership after successful creation and disposes the ViewModel when closed.
    /// </remarks>
    public static ValueTask<IAsyncDocument> FindDocumentByIdOrCreateAsync<TViewModel>(
        this IAsyncDocumentManagerService service,
        object id,
        string? documentType,
        Func<TViewModel> viewModelFactory,
        object? parentViewModel,
        object? parameter,
        CancellationToken cancellationToken = default)
        where TViewModel : class, IAsyncDisposable
    {
        Throw.IfNull(service);
        Throw.IfNull(viewModelFactory);

        return service.FindDocumentByIdOrCreateAsync(id, CreateDocumentAsync);

        async ValueTask<IAsyncDocument> CreateDocumentAsync(IAsyncDocumentManagerService manager)
        {
            var viewModel = viewModelFactory();
            try
            {
                var document = await manager.CreateDocumentAsync(
                    documentType: documentType,
                    viewModel: viewModel,
                    parentViewModel: parentViewModel,
                    parameter: parameter,
                    cancellationToken: cancellationToken);

                document.DisposeOnClose = true;
                return document;
            }
            catch (Exception ex)
            {
                Debug.Assert(ex is OperationCanceledException, ex.Message);
                await viewModel.DisposeAsync();
                throw;
            }
        }
    }
}