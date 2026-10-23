using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Mvvm.Wpf;

public sealed partial class MediaElementService : ServiceBase<MediaElement>, IMediaElementService
{
    private MediaState _currentState;
    private bool _isMediaOpened;
    private IDisposable? _subscription;

    public MediaElementService()
    {
        if (ViewModelHelper.IsInDesignMode) return;

        PlayCommand = new RelayCommand(Play, CanPlay);
        PauseCommand = new RelayCommand(Pause, CanPauseInternal);
        StopCommand = new RelayCommand(Stop, CanStop);
    }

    #region Properties

    internal MediaState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnCurrentStateChanged();
                OnPropertyChanged(EventArgsCache.CurrentStatePropertyChanged);
            }
        }
    }

    public bool IsMediaOpened
    {
        get => _isMediaOpened;
        private set
        {
            if (_isMediaOpened != value)
            {
                _isMediaOpened = value;
                OnPropertyChanged(EventArgsCache.IsMediaOpenedPropertyChanged);
            }
        }
    }

    public bool IsPaused => CurrentState == MediaState.Pause;

    public bool IsPlaying => CurrentState == MediaState.Play;

    private MediaElement Player => (MediaElement)AssociatedObject!;

    #endregion

    #region Event Handlers

    private void OnBufferingEnded(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(EventArgsCache.IsBufferingPropertyChanged);
        OnPropertyChanged(EventArgsCache.BufferingProgressPropertyChanged);
        Debug.Assert(IsBuffering == false);
    }

    private void OnBufferingStarted(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(EventArgsCache.IsBufferingPropertyChanged);
        OnPropertyChanged(EventArgsCache.BufferingProgressPropertyChanged);
        Debug.Assert(IsBuffering);
    }

    private void OnCurrentStateChanged()
    {
        OnPropertyChanged(EventArgsCache.CanPausePropertyChanged);
        OnPropertyChanged(EventArgsCache.IsPausedPropertyChanged);
        OnPropertyChanged(EventArgsCache.IsPlayingPropertyChanged);

        PlayCommand.RaiseCanExecuteChanged();
        PauseCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
    }

    private void OnMediaElementLoaded(object sender, RoutedEventArgs e)
    {
        Debug.Assert(Equals(sender, AssociatedObject));
        if (sender is FrameworkElement fe)
        {
            fe.Loaded -= OnMediaElementLoaded;
        }
        Debug.Assert(_subscription == null);
        Disposable.DisposeAndNull(ref _subscription);
        _subscription = SubscribeMediaElement(AssociatedObject);
    }

    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        Player.Stop();
        IsMediaOpened = false;
        CurrentState = MediaState.Stop;
    }

    private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        IsMediaOpened = false;
        CurrentState = MediaState.Stop;
    }

    private void OnMediaOpened(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(EventArgsCache.NaturalDurationPropertyChanged);
        IsMediaOpened = true;
        CurrentState = MediaState.Play;
    }

    private void OnScriptCommand(object? sender, MediaScriptCommandRoutedEventArgs e)
    {

    }

    #endregion

    #region Methods

    protected override void OnAttached()
    {
        base.OnAttached();
        Debug.Assert(_subscription == null);

        if (AssociatedObject!.IsLoaded)
        {
            Disposable.DisposeAndNull(ref _subscription);
            _subscription = SubscribeMediaElement(AssociatedObject);
        }
        else
        {
            AssociatedObject.Loaded += OnMediaElementLoaded;
        }
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.Loaded -= OnMediaElementLoaded;
        Disposable.DisposeAndNull(ref _subscription);

        base.OnDetaching();
    }

    private Lifetime? SubscribeMediaElement(MediaElement? mediaElement)
    {
        if (mediaElement == null)
        {
            return null;
        }
        var lifetime = new Lifetime()
#if DEBUG
                .SetDebugInfo()
#endif
            ;
        lifetime.AddBracket(
            () => mediaElement.BufferingStarted += OnBufferingStarted,
            () => mediaElement.BufferingStarted -= OnBufferingStarted);
        lifetime.AddBracket(
            () => mediaElement.BufferingEnded+= OnBufferingEnded,
            () => mediaElement.BufferingEnded -= OnBufferingEnded);
        lifetime.AddBracket(
            () => mediaElement.MediaOpened += OnMediaOpened,
            () => mediaElement.MediaOpened -= OnMediaOpened);
        lifetime.AddBracket(
            () => mediaElement.MediaEnded += OnMediaEnded,
            () => mediaElement.MediaEnded -= OnMediaEnded);
        lifetime.AddBracket(
            () => mediaElement.MediaFailed += OnMediaFailed,
            () => mediaElement.MediaFailed -= OnMediaFailed);
        lifetime.AddBracket(
            () => mediaElement.ScriptCommand += OnScriptCommand,
            () => mediaElement.ScriptCommand -= OnScriptCommand);
        return lifetime;
    }

    #endregion
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs BufferingProgressPropertyChanged = new(nameof(MediaElementService.BufferingProgress));
    internal static readonly PropertyChangedEventArgs CanPausePropertyChanged = new(nameof(MediaElementService.CanPause));
    internal static readonly PropertyChangedEventArgs CurrentStatePropertyChanged = new(nameof(MediaElementService.CurrentState));
    internal static readonly PropertyChangedEventArgs IsBufferingPropertyChanged = new(nameof(MediaElementService.IsBuffering));
    internal static readonly PropertyChangedEventArgs IsMediaOpenedPropertyChanged = new(nameof(MediaElementService.IsMediaOpened));
    internal static readonly PropertyChangedEventArgs IsPausedPropertyChanged = new(nameof(MediaElementService.IsPaused));
    internal static readonly PropertyChangedEventArgs IsPlayingPropertyChanged = new(nameof(MediaElementService.IsPlaying));
    internal static readonly PropertyChangedEventArgs NaturalDurationPropertyChanged = new(nameof(MediaElementService.NaturalDuration));
}
