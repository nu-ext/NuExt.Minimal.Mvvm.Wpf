using Presentation.Wpf;
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
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// A service for managing windowed documents within a UI. Extends DocumentServiceBase for asynchronous document lifecycle management.
/// This service handles creation, binding, and disposal of windowed documents associated with a control.
/// </summary>
public class WindowedDocumentService : DocumentServiceBase, IAsyncDocumentManagerService, IAsyncDisposable
{
    #region WindowedDocument

    private class WindowedDocument : AsyncDisposable, IAsyncDocument
    {
        private readonly AsyncLifetime _lifetime = new AsyncLifetime() { ContinueOnCapturedContext = true }
#if DEBUG
            .SetDebugInfo()
#endif
            ;
        private readonly CancellationTokenSource _cts = new();
        private bool _isClosing;
        private bool? _isClosed;
        private bool? _isViewModelDisposing;
        private bool? _isViewModelDisposed;
        private readonly Action _onTitleChanged;

        public WindowedDocument(WindowedDocumentService owner, Window window, AsyncLifetime lifetime)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Window = window ?? throw new ArgumentNullException(nameof(window));

            Debug.Assert(Window.CheckAccess());

            var viewModel = ViewModelHelper.GetViewModelFromView(Window.Content);
            Debug.Assert(viewModel != null);

            _lifetime.AddDisposable(_cts);
            _lifetime.AddBracket(() => owner.AddDocument(this), () => owner.RemoveDocument(this));
            _lifetime.AddBracket(() => SetDocument(Window, this), () => SetDocument(Window, null));

            _lifetime.Add(Window.Close);//close window
            _lifetime.Add(() => Window.ClearStyle());//clear window style
            _lifetime.Add(() => owner.ClearAllBindings(Window));//detach vm
            _lifetime.Add(() => ViewModelHelper.DetachContentFromContainer(Window));//detach content
            if (viewModel is INotifyPropertyChanged notifier)
            {
                _lifetime.AddBracket(
                    () => notifier.PropertyChanged += OnViewModelPropertyChanged,
                    () => notifier.PropertyChanged -= OnViewModelPropertyChanged);
            }
            _lifetime.AddAsync(DisposeViewModelAsync);//dispose vm

            _lifetime.AddAsyncDisposable(lifetime);

            if (viewModel != null && ViewModelHelper.ViewModelHasTitleProperty(viewModel))
            {
                _lifetime.AddBracket(
                    () => ViewModelHelper.SetViewTitleBinding(Window.Content, Window.TitleProperty, Window),
                    () => BindingOperations.ClearBinding(Window, Window.TitleProperty));
            }

            var dpd = DependencyPropertyDescriptor.FromProperty(Window.TitleProperty, typeof(Window));
            if (dpd != null)
            {
                //lock to avoid ThrowInvalidOperationException_ConcurrentOperationsNotSupported
                _lifetime.AddBracket(
                    () => { lock (LockPool.Get(dpd)) dpd.AddValueChanged(Window, OnWindowTitleChanged); },
                    () => { lock (LockPool.Get(dpd)) dpd.RemoveValueChanged(Window, OnWindowTitleChanged); });
            }
            Debug.Assert(dpd != null);

            Title = Window.Title;
            _onTitleChanged = OnTitleChanged;
        }

        #region Properties

        private CancellationToken CancellationToken => _cts.Token;

        /// <summary>
        /// Gets or sets a value indicating whether the view model should be disposed when the document closes.
        /// </summary>
        public bool DisposeOnClose { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the window should be hidden instead of closed.
        /// </summary>
        public bool HideInsteadOfClose { get; set; }

        /// <summary>
        /// Gets or sets the identifier for this document.
        /// </summary>
        public object Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title
        {
            get;
            set => SetProperty(ref field, value, _onTitleChanged, EventArgsCache.TitlePropertyChanged);
        }

        private Window Window { get; }

        private WindowedDocumentService Owner { get; }

        private bool IsViewModeDisposingOrDisposed => _isViewModelDisposing == true || _isViewModelDisposed == true;

        #endregion

        #region Event Handlers

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.Assert(sender is IDisposableState);
            if (sender is not IDisposableState viewModl)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(IDisposableState.IsDisposing):
                    _isViewModelDisposing = viewModl.IsDisposing;
                    break;
                case nameof(IDisposableState.IsDisposed):
                    _isViewModelDisposed = viewModl.IsDisposed;
                    break;
            }
        }

        internal void OnWindowClosing(CancelEventArgs e)
        {
            if (e.Cancel || _isClosing || CancellationToken.IsCancellationRequested)
            {
                return;
            }
            Debug.Assert(!IsDisposingOrDisposed);
            if (IsDisposingOrDisposed)
            {
                return;
            }
            if (IsViewModeDisposingOrDisposed)
            {
                return;
            }

            e.Cancel = true;
            _ = Window.Dispatcher.InvokeAsync(async () => await CloseWindowAsync(this, CancellationToken));
        }

        private static async ValueTask CloseWindowAsync(WindowedDocument document, CancellationToken cancellationToken)
        {
            Debug.Assert(!document._isClosing);

            var viewModel = ViewModelHelper.GetViewModelFromView(document.Window.Content);
            Debug.Assert(viewModel != null);

            if (document.HideInsteadOfClose)
            {
                if (await CanCloseAsync(document.Window, cancellationToken) == false)
                {
                    return;
                }
                document.Hide();
                return;
            }

            await document.CloseAsync(false).ConfigureAwait(false);
        }

        internal void OnWindowClosed()
        {
#if DEBUG
            var activeDocument = Owner.GetPropertySafe(() => Owner.ActiveDocument);
            Debug.Assert(activeDocument != this);
#endif
            _isClosed = true;
        }

        private void OnWindowTitleChanged(object? sender, EventArgs e)
        {
            Debug.Assert(Window == sender);
            Debug.Assert(Window.CheckAccess());
            Title = Window.Title;
        }

        private void OnTitleChanged()
        {
            if (Window.CheckAccess())
            {
                Window.Title = Title ?? "Untitled";
            }
            else
            {
                Window.Dispatcher.Invoke(new Action(() => Window.Title = Title));
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

            var dispatcher = Window.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                return CloseAsyncCore(force);
            }
            return new ValueTask(dispatcher.InvokeAsync(async () => await CloseAsyncCore(force).ConfigureAwait(false)).Task.Unwrap());
        }

        private async ValueTask CloseAsyncCore(bool force)
        {
            Debug.Assert(Window.CheckAccess());
            Debug.Assert(IsDisposingOrDisposed == false);

            VerifyAccess();
            CheckDisposingOrDisposed();

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
                    if (await CanCloseAsync(Window, CancellationToken) == false)
                    {
                        return;
                    }
                }
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while closing document '{Title}': {ex.Message}");
                Debug.Fail($"Exception occurred while closing document '{Title}'.", ex.Message);
                if (force)
                {
                    throw;
                }
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
            if (IsViewModeDisposingOrDisposed)
            {
                Debug.Assert(_isViewModelDisposed == true);
                return ValueTask.CompletedTask;
            }
            var viewModel = ViewModelHelper.GetViewModelFromView(Window.Content);
            Debug.Assert(viewModel != null);
            if (DisposeOnClose && viewModel is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Hides the document window.
        /// </summary>
        public void Hide()
        {
            CheckDisposed();

            if (_isClosed == true)
            {
                return;
            }

            if (Window.CheckAccess())
            {
                Window.Hide();
            }
            else
            {
                Window.Dispatcher.Invoke(Hide);
            }
        }

        /// <summary>
        /// Shows the document window.
        /// </summary>
        public void Show()
        {
            CheckDisposed();

            if (_isClosed == true)
            {
                return;
            }

            if (Window.CheckAccess())
            {
                Window.BringToFront();
            }
            else
            {
                Window.Dispatcher.Invoke(Show);
            }
        }

        /// <summary>
        /// Returns a string representation of the document.
        /// </summary>
        /// <returns>The document title or "Untitled".</returns>
        public override string ToString()
        {
            return Title ?? "Untitled";
        }

        #endregion
    }

    #endregion

    private readonly ObservableCollection<IAsyncDocument> _documents = [];
    private bool _isActiveDocumentChanging;

    public WindowedDocumentService()
    {
        if (ViewModelHelper.IsInDesignMode) return;
        (_documents as INotifyPropertyChanged).PropertyChanged += OnDocumentsPropertyChanged;
    }

    #region Properties

    /// <inheritdoc/>
    public int Count => _documents.Count;

    /// <inheritdoc/>
    public IEnumerable<IAsyncDocument> Documents => _documents;

    /// <summary>
    /// Gets or sets a value indicating whether newly created windows should have an owner window set.
    /// </summary>
    public bool SetWindowOwner
    {
        get => WindowHelper.GetSetWindowOwner(this);
        set => WindowHelper.SetSetWindowOwner(this, value);
    }

    /// <summary>
    /// Gets or sets the startup location for newly created windows.
    /// </summary>
    public WindowStartupLocation WindowStartupLocation
    {
        get => WindowHelper.GetWindowStartupLocation(this);
        set => WindowHelper.SetWindowStartupLocation(this, value);
    }

    /// <summary>
    /// Gets or sets the style to apply to newly created windows.
    /// </summary>
    public Style? WindowStyle
    {
        get => WindowHelper.GetWindowStyle(this);
        set => WindowHelper.SetWindowStyle(this, value);
    }

    /// <summary>
    /// Gets or sets the resource key for the window style to look up in resources.
    /// </summary>
    public string? WindowStyleKey
    {
        get => WindowHelper.GetWindowStyleKey(this);
        set => WindowHelper.SetWindowStyleKey(this, value);
    }

    /// <summary>
    /// Gets or sets the style selector to choose a window style for newly created windows.
    /// </summary>
    public StyleSelector? WindowStyleSelector
    {
        get => WindowHelper.GetWindowStyleSelector(this);
        set => WindowHelper.SetWindowStyleSelector(this, value);
    }

    /// <summary>
    /// Gets or sets the type of window to create. Uses standard Window type if not specified.
    /// </summary>
    public Type? WindowType
    {
        get => WindowHelper.GetWindowType(this);
        set => WindowHelper.SetWindowType(this, value);
    }

    #endregion

    #region Event Handlers

    private void OnDocumentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_documents.Count))
        {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        }
    }

    protected override void OnActiveDocumentChanged(IAsyncDocument? oldValue, IAsyncDocument? newValue)
    {
        Debug.Assert(oldValue != newValue);

        VerifyAccess();
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

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        Debug.Assert(((Window)sender!).CheckAccess());
        var document = GetDocument((Window)sender!);

        if (CheckAccess())
        {
            OnWindowActivated(document);
        }
        else
        {
            Dispatcher.InvokeAsync(() => OnWindowActivated(document), DispatcherPriority.Send);
        }

        void OnWindowActivated(IAsyncDocument? document)
        {
            VerifyAccess();

            if (_isActiveDocumentChanging)
            {
                return;
            }
            _isActiveDocumentChanging = true;
            try
            {
                ActiveDocument = document;
            }
            finally
            {
                _isActiveDocumentChanging = false;
            }
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Debug.Assert(((Window)sender!).CheckAccess());
        var document = GetDocument((Window)sender!);

        if (CheckAccess())
        {
            OnWindowDeactivated(document);
        }
        else
        {
            Dispatcher.InvokeAsync(() => OnWindowDeactivated(document), DispatcherPriority.Send);
        }

        void OnWindowDeactivated(IAsyncDocument? document)
        {
            VerifyAccess();

            if (_isActiveDocumentChanging)
            {
                return;
            }
            if (ActiveDocument == document)
            {
                _isActiveDocumentChanging = true;
                try
                {
                    ActiveDocument = null;
                }
                finally
                {
                    _isActiveDocumentChanging = false;
                }
            }
        }
    }

    private static void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        var document = GetDocument((Window)sender!) as WindowedDocument;
        Debug.Assert(document != null);
        document?.OnWindowClosing(e);
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        var document = GetDocument((Window)sender!) as WindowedDocument;
        Debug.Assert(document != null);
        document?.OnWindowClosed();
    }

#if DEBUG
    private static void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        Debug.Assert(((Window)sender!).DataContext == ViewModelHelper.GetViewModelFromView(((Window)sender!).Content));
    }
#endif

    #endregion

    #region Methods

    private void AddDocument(IAsyncDocument document)
    {
        if (!CheckAccess())
        {
            Dispatcher.Invoke(() => _documents.Add(document), DispatcherPriority.Send);
            return;
        }
        _documents.Add(document);
    }

    private void RemoveDocument(IAsyncDocument document)
    {
        if (!CheckAccess())
        {
            Dispatcher.Invoke(() => _documents.Remove(document), DispatcherPriority.Send);
            return;
        }
        _documents.Remove(document);
    }

    private static async ValueTask<bool> CanCloseAsync(Window window, CancellationToken cancellationToken)
    {
        try
        {
            var viewModel = ViewModelHelper.GetViewModelFromView(window.Content);
            Debug.Assert(viewModel != null);
            switch (viewModel)
            {
                case IAsyncDocumentContent { } documentContent when await documentContent.CanCloseAsync(cancellationToken) == false:
                case IWindowViewModel { } windowViewModel when await windowViewModel.CanCloseAsync(cancellationToken) == false:
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
        cancellationToken.ThrowIfCancellationRequested();

        var view = await CreateViewAsync(documentType, cancellationToken);

        var lifetime = new AsyncLifetime() { ContinueOnCapturedContext = true }
#if DEBUG
                .SetDebugInfo()
#endif
            ;

        var window = WindowHelper.CreateWindow(this, view, viewModel, SubscribeWindow);
        ViewModelHelper.SetDataContextBinding(view, FrameworkElement.DataContextProperty, window);
        try
        {
            await ViewModelHelper.InitializeViewAsync(view, viewModel, parentViewModel, parameter, cancellationToken);
        }
        catch
        {
            await lifetime.DisposeAsync();
            throw;
        }

        var document = new WindowedDocument(this, window, lifetime) { ContinueOnCapturedContext = true };
        return document;

        void SubscribeWindow(Window window)
        {
            lifetime.AddBracket(
                () => window.Activated += OnWindowActivated,
                () => window.Activated -= OnWindowActivated);
            lifetime.AddBracket(
                () => window.Deactivated += OnWindowDeactivated,
                () => window.Deactivated -= OnWindowDeactivated);
            lifetime.AddBracket(
                () => window.Closing += OnWindowClosing,
                () => window.Closing -= OnWindowClosing);
            lifetime.AddBracket(
                () => window.Closed += OnWindowClosed,
                () => window.Closed -= OnWindowClosed);
#if DEBUG
            lifetime.AddBracket(
                () => window.SourceInitialized += OnWindowSourceInitialized,
                () => window.SourceInitialized -= OnWindowSourceInitialized);
#endif
        }
    }

    protected virtual void ClearAllBindings(Window window)
    {
        BindingOperations.ClearBinding(window, FrameworkElement.DataContextProperty);
    }

    /// <inheritdoc/>
    public async ValueTask CloseAllAsync()
    {
        if (_documents.Count == 0)
        {
            return;
        }

        await Task.WhenAll(_documents.ToList().Select(async x => await x.CloseAsync().ConfigureAwait(false))).ConfigureAwait(false);
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

    #endregion
}