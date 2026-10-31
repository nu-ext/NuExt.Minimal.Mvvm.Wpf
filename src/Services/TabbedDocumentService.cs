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
using Presentation.Wpf.Controls;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Manages tabbed documents within a TabControl. Handles creation, lifecycle, and disposal of documents as tabs.
/// This service extends DocumentServiceBase for asynchronous document management and implements IAsyncDocumentManagerService.
/// </summary>
public class TabbedDocumentService : DocumentServiceBase, IAsyncDocumentManagerService, IAsyncDisposable
{
    #region TabbedDocument

    protected sealed class TabbedDocument : AsyncDisposable, IAsyncDocument
    {
        private readonly AsyncLifetime _lifetime = new AsyncLifetime() { ContinueOnCapturedContext = true }
#if DEBUG
            .SetDebugInfo()
#endif
            ;
        private readonly CancellationTokenSource _cts = new();

        internal TabbedDocument(TabbedDocumentService owner, TabItem tabItem)
        {
            ArgumentNullException.ThrowIfNull(owner);
            TabItem = tabItem ?? throw new ArgumentNullException(nameof(tabItem));

            var viewModel = ViewModelHelper.GetViewModelFromView(TabItem.Content);
            Debug.Assert(viewModel != null);

            _lifetime.AddDisposable(_cts);
            _lifetime.AddBracket(() => owner.AddDocument(this), () => owner.RemoveDocument(this));
            _lifetime.Add(() => TabControl?.Items.Remove(TabItem));//5) remove tab item
            _lifetime.Add(() => TabItem.ClearStyle());//4) clear tab item style
            _lifetime.Add(() => owner.ClearAllBindings(TabItem));//3) detach vm
            _lifetime.Add(() => ViewModelHelper.DetachContentFromContainer(TabItem));//2) detach content
            _lifetime.AddAsync(DisposeViewModelAsync);//1) dispose vm
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

        /// <inheritdoc/>
        public bool DisposeOnClose { get; set; }

        /// <inheritdoc/>
        public bool HideInsteadOfClose { get; set; }

        /// <inheritdoc/>
        public object Id { get; set; } = null!;

        /// <inheritdoc/>
        public string Title
        {
            get => TabItem.Header?.ToString() ?? string.Empty;
            set => TabItem.Header = value;
        }

        public bool IsClosing { get; private set; }

        private TabControl? TabControl => TabItem.Parent as TabControl;

        public TabItem TabItem { get; }

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

            if (IsClosing)
            {
                return;
            }

            IsClosing = true;
            try
            {

                if (force)
                {
#if NET8_0_OR_GREATER
                    await _cts.CancelAsync();
#else
                    _cts.Cancel();
#endif
                }
                else
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
                IsClosing = false;
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

        /// <inheritdoc/>
        public void Hide()
        {
            CheckDisposed();
            VerifyAccess();

            if (TabItem.Visibility != Visibility.Collapsed)
            {
                TabItem.Visibility = Visibility.Collapsed;
            }
            TabItem.IsSelected = false;
        }

        /// <inheritdoc/>
        public void Show()
        {
            CheckDisposed();
            VerifyAccess();

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
        Debug.Assert(oldValue != newValue);
        VerifyAccess();

        if (!_isActiveDocumentChanging && newValue != null)
        {
            _isActiveDocumentChanging = true;
            try
            {
                newValue.Show();
            }
            finally
            {
                _isActiveDocumentChanging = false;
            }
        }

        base.OnActiveDocumentChanged(oldValue, newValue);
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
            ActiveDocument = tabControl.SelectedItem is TabItem tabItem ? GetDocument(tabItem) : null;
        }
        finally
        {
            _isActiveDocumentChanging = false;
        }
    }

    private void OnDocumentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_documents.Count))
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
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
            Debug.Assert(viewModel != null);
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
        ArgumentNullException.ThrowIfNull(AssociatedObject);
        cancellationToken.ThrowIfCancellationRequested();

        var view = await CreateViewAsync(documentType, cancellationToken);

        var tabItem = CreateTabItem();
        tabItem.Content = view;
        ViewModelHelper.SetDataContextBinding(view, FrameworkElement.DataContextProperty, tabItem);

        await ViewModelHelper.InitializeViewAsync(view, viewModel, parentViewModel, parameter, cancellationToken);

        var document = new TabbedDocument(this, tabItem) { ContinueOnCapturedContext = true };
        try
        {
            AssociatedObject.Items.Add(tabItem);
        }
        catch (Exception)
        {
            await document.DisposeAsync().ConfigureAwait(false);
            throw;
        }
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

        lifetime.AddBracket(
            () => tabControl.SelectionChanged += OnTabControlSelectionChanged,
            () => tabControl.SelectionChanged -= OnTabControlSelectionChanged);

        return lifetime;
    }

    #endregion
}