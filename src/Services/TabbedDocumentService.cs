using Presentation.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Manages tabbed documents within a TabControl. Handles creation, lifecycle, and disposal of documents as tabs.
/// This service extends DocumentServiceBase for asynchronous document management and implements IAsyncDocumentManagerService.
/// </summary>
public class TabbedDocumentService : DocumentServiceBase, IAsyncDocumentManagerService, IAsyncDisposable
{
    #region TabbedDocument

    protected class TabbedDocument : AsyncDisposable, IAsyncDocument
    {
        private readonly AsyncLifetime _lifetime = new AsyncLifetime() { ContinueOnCapturedContext = true }
#if DEBUG
            .SetDebugInfo()
#endif
            ;
        private readonly CancellationTokenSource _cts = new();
        private bool _isClosing;

        public TabbedDocument(TabbedDocumentService owner, TabItem tabItem)
        {
            ArgumentNullException.ThrowIfNull(owner);
            TabItem = tabItem ?? throw new ArgumentNullException(nameof(tabItem));

            var viewModel = ViewModelHelper.GetViewModelFromView(TabItem.Content);
            Debug.Assert(viewModel != null);

            _lifetime.AddDisposable(_cts);
            _lifetime.AddBracket(() => owner.AddDocument(this), () => owner.RemoveDocument(this));
            _lifetime.Add(() => TabControl?.Items.Remove(TabItem));//second, remove tab item
            _lifetime.Add(() => TabItem.ClearStyle());//first, clear tab item style
            _lifetime.Add(() => owner.ClearAllBindings(TabItem));//third, detach vm
            _lifetime.Add(() => ViewModelHelper.DetachContentFromContainer(TabItem));//second, detach content
            _lifetime.AddAsync(DisposeViewModelAsync);//first, dispose vm
            _lifetime.AddBracket(() => SetDocument(TabItem, this), () => SetDocument(TabItem, null));
            _lifetime.AddBracket(
                () => TabItem.IsVisibleChanged += OnTabIsVisibleChanged,
                () => TabItem.IsVisibleChanged -= OnTabIsVisibleChanged);
            if (viewModel != null && ViewModelHelper.ViewModelHasTitleProperty(viewModel))
            {
                _lifetime.AddBracket(
                    () => ViewModelHelper.SetViewTitleBinding(TabItem.Content, HeaderedContentControl.HeaderProperty, TabItem),
                    () => BindingOperations.ClearBinding(TabItem, HeaderedContentControl.HeaderProperty));
            }

            var dpd = DependencyPropertyDescriptor.FromProperty(HeaderedContentControl.HeaderProperty, typeof(HeaderedContentControl));
            if (dpd != null)
            {
                _lifetime.AddBracket(
                    () => dpd.AddValueChanged(TabItem, OnTabHeaderChanged),
                    () => dpd.RemoveValueChanged(TabItem, OnTabHeaderChanged));
            }
            Debug.Assert(dpd != null);
        }

        #region Properties

        public CancellationToken CancellationToken => _cts.Token;

        /// <summary>
        /// Gets or sets a value indicating whether the view model should be disposed when the document closes.
        /// </summary>
        public bool DisposeOnClose { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tab should be hidden instead of closed.
        /// </summary>
        public bool HideInsteadOfClose { get; set; }

        /// <summary>
        /// Gets or sets the identifier for this document.
        /// </summary>
        public object Id { get; set; } = null!;

        public bool IsClosing => _isClosing;

        private TabControl? TabControl => TabItem.Parent as TabControl;

        public TabItem TabItem { get; }

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title
        {
            get => TabItem.Header?.ToString() ?? string.Empty;
            set => TabItem.Header = value;
        }

        #endregion

        #region Event Handlers

        private void OnTabHeaderChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(EventArgsCache.TitlePropertyChanged);
        }

        private void OnTabIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (TabItem.Content is UIElement element)
            {
                element.Visibility = TabItem.Visibility;
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public ValueTask CloseAsync(bool force)
        {
            if (IsDisposingOrDisposed)
            {
                return ValueTask.CompletedTask;
            }

            var dispatcher = TabItem.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                return CloseAsyncCore(force);
            }
            return new ValueTask(dispatcher.InvokeAsync(async () => await CloseAsyncCore(force).ConfigureAwait(false)).Task.Unwrap());
        }

        private async ValueTask CloseAsyncCore(bool force)
        {
            Debug.Assert(TabItem.CheckAccess());
            Debug.Assert(IsDisposed == false);

            VerifyAccess();
            CheckDisposed();

            if (_isClosing)
            {
                return;
            }
            if (force)
            {
#if NET8_0_OR_GREATER
                await _cts.CancelAsync();
#else
                _cts.Cancel();
#endif
            }
            _isClosing = true;
            try
            {
                if (!force)
                {
                    if (await CanCloseAsync(TabItem, CancellationToken) == false)
                    {
                        return;
                    }
                }
                await DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _isClosing = false;
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            Debug.Assert(ContinueOnCapturedContext);
            Debug.Assert(CheckAccess());
            VerifyAccess();
#if NET8_0_OR_GREATER
            await _cts.CancelAsync();
#else
            _cts.Cancel();
#endif
            Hide();
            await _lifetime.DisposeAsync().ConfigureAwait(false);
        }

        private ValueTask DisposeViewModelAsync()
        {
            var viewModel = ViewModelHelper.GetViewModelFromView(TabItem.Content);
            Debug.Assert(viewModel != null);
            if (DisposeOnClose && viewModel is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Hides the document tab.
        /// </summary>
        public void Hide()
        {
            CheckDisposed();

            if (TabItem.Visibility != Visibility.Collapsed)
            {
                TabItem.Visibility = Visibility.Collapsed;
            }
            TabItem.IsSelected = false;
        }

        /// <summary>
        /// Shows the document tab.
        /// </summary>
        public void Show()
        {
            CheckDisposed();

            if (TabItem.Visibility != Visibility.Visible)
            {
                TabItem.Visibility = Visibility.Visible;
            }
            TabItem.IsSelected = true;
        }

        #endregion
    }

    #endregion

    private readonly ObservableCollection<IAsyncDocument> _documents = [];
    private bool _isActiveDocumentChanging;
    private IDisposable? _subscription;

    public TabbedDocumentService()
    {
        if (ViewModelHelper.IsInDesignMode) return;
        (_documents as INotifyPropertyChanged).PropertyChanged += OnDocumentsPropertyChanged;
    }

    #region Properties

    private new TabControl? AssociatedObject => (TabControl?)base.AssociatedObject;

    /// <inheritdoc/>
    public int Count => _documents.Count;

    /// <inheritdoc/>
    public IEnumerable<IAsyncDocument> Documents => _documents;

    #endregion

    #region Event Handlers

    protected override void OnActiveDocumentChanged(IAsyncDocument? oldValue, IAsyncDocument? newValue)
    {
        if (!_isActiveDocumentChanging)
        {
            _isActiveDocumentChanging = true;
            try
            {
                newValue?.Show();
            }
            finally
            {
                _isActiveDocumentChanging = false;
            }
        }
        base.OnActiveDocumentChanged(oldValue, newValue);
    }

    private void OnDocumentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_documents.Count))
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        }
    }

    private static async void OnTabControlItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is { Count: > 0 })
                {
                    foreach (var item in e.OldItems)
                    {
                        if (item is not TabItem tab)
                        {
                            continue;
                        }
                        var document = GetDocument(tab);
                        Debug.Assert(document is null, "The document associated with the removed tab should already be absent from the document collection.");
                        if (document is not null)
                        {
                            try
                            {
                                await document.CloseAsync(force: true).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                var message = $"Error closing document {document}: {ex.Message}";
                                Trace.WriteLine(message);
                                Debug.Fail(message);
                            }
                        }
                    }
                }
                break;
        }
    }

    private void OnTabControlLoaded(object sender, RoutedEventArgs e)
    {
        Debug.Assert(Equals(sender, AssociatedObject));
        if (sender is FrameworkElement fe)
        {
            fe.Loaded -= OnTabControlLoaded;
        }
        Debug.Assert(_subscription == null);
        Disposable.DisposeAndNull(ref _subscription);
        _subscription = SubscribeTabControl(AssociatedObject);
    }

    private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isActiveDocumentChanging)
        {
            return;
        }
        Debug.Assert(Equals(sender, AssociatedObject));
        if (sender is not TabControl tabControl)
        {
            return;
        }
        _isActiveDocumentChanging = true;
        try
        {
            ActiveDocument = (tabControl.SelectedItem is TabItem tabItem) ? GetDocument(tabItem) : null;
        }
        finally
        {
            _isActiveDocumentChanging = false;
        }
    }

    #endregion

    #region Methods

    private void AddDocument(IAsyncDocument document)
    {
        VerifyAccess();
        _documents.Add(document);
    }

    private void RemoveDocument(IAsyncDocument document)
    {
        VerifyAccess();
        _documents.Remove(document);
    }

    protected static async ValueTask<bool> CanCloseAsync(TabItem tabItem, CancellationToken cancellationToken)
    {
        try
        {
            var viewModel = ViewModelHelper.GetViewModelFromView(tabItem.Content);
            switch (viewModel)
            {
                case IAsyncDocumentContent documentContent when await documentContent.CanCloseAsync(cancellationToken) == false:
                    cancellationToken.ThrowIfCancellationRequested();
                    return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //do nothing and return
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public async ValueTask<IAsyncDocument> CreateDocumentAsync(string? documentType, object? viewModel,
        object? parentViewModel, object? parameter, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(AssociatedObject);
        cancellationToken.ThrowIfCancellationRequested();

        var view = await CreateViewAsync(documentType, cancellationToken);

        var tabItem = CreateTabItem();
        tabItem.Content = view;
        ViewModelHelper.SetDataContextBinding(view, FrameworkElement.DataContextProperty, tabItem);

        await ViewModelHelper.InitializeViewAsync(view, viewModel, parentViewModel, parameter, cancellationToken);

        AssociatedObject.Items.Add(tabItem);
        var document = new TabbedDocument(this, tabItem) { ContinueOnCapturedContext = true };
        return document;
    }

    protected virtual TabItem CreateTabItem()
    {
        var tabItem = new TabItem
        {
            Header = "Item",
        };
        return tabItem;
    }

    protected virtual void ClearAllBindings(TabItem tabItem)
    {
        BindingOperations.ClearBinding(tabItem, FrameworkElement.DataContextProperty);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync()
    {
        if (_documents.Count == 0)
        {
            return;
        }
        await Task.WhenAll(_documents.ToList().Select(async x => await x.CloseAsync(force: true).ConfigureAwait(false))).ConfigureAwait(false);
        if (_documents.Count == 0)
        {
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_documents.Count == 0)
            {
                tcs.TrySetResult(true);
            }
        }

        _documents.CollectionChanged += OnDocumentsCollectionChanged;
        try
        {
            if (_documents.Count == 0)
            {
                tcs.TrySetResult(true);
            }
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _documents.CollectionChanged -= OnDocumentsCollectionChanged;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAllAsync().ConfigureAwait(false);
        }
        finally
        {
            (_documents as INotifyPropertyChanged).PropertyChanged -= OnDocumentsPropertyChanged;
        }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        Debug.Assert(_subscription == null);

        if (AssociatedObject!.IsLoaded)
        {
            Disposable.DisposeAndNull(ref _subscription);
            _subscription = SubscribeTabControl(AssociatedObject);
        }
        else
        {
            AssociatedObject.Loaded += OnTabControlLoaded;
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        AssociatedObject!.Loaded -= OnTabControlLoaded;
        Disposable.DisposeAndNull(ref _subscription);

        base.OnDetaching();
    }

    protected virtual Lifetime? SubscribeTabControl(TabControl? tabControl)
    {
        if (tabControl == null)
        {
            return null;
        }
        if (tabControl.ItemsSource != null)
        {
            Throw.InvalidOperationException("Can't use not null ItemsSource in this service.");
        }
        var lifetime = new Lifetime()
#if DEBUG
            .SetDebugInfo()
#endif
            ;
        if (tabControl.Items is INotifyCollectionChanged collection)
        {
            lifetime.AddBracket(() => collection.CollectionChanged += OnTabControlItemsCollectionChanged,
                () => collection.CollectionChanged -= OnTabControlItemsCollectionChanged);
        }
        lifetime.AddBracket(() => tabControl.SelectionChanged += OnTabControlSelectionChanged,
            () => tabControl.SelectionChanged -= OnTabControlSelectionChanged);
        return lifetime;
    }

    #endregion
}