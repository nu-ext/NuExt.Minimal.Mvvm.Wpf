using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

partial class MediaElementService
{
    #region Commands

    ICommand IMediaElementService.PauseCommand => PauseCommand;

    ICommand IMediaElementService.PlayCommand => PlayCommand;

    ICommand IMediaElementService.StopCommand => StopCommand;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the buffering progress of the media.
    /// </summary>
    public double BufferingProgress => Player.BufferingProgress;

    /// <summary>
    /// Returns whether the given media can be paused. This is only valid
    /// after the MediaOpened event has fired.
    /// </summary>
    public bool CanPause => Player.CanPause;

    /// <summary>
    /// Returns the download progress of the media.
    /// </summary>
    public double DownloadProgress => Player.DownloadProgress;

    /// <summary>
    /// Returns whether the given media has audio. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    public bool HasAudio => Player.HasAudio;

    /// <summary>
    /// Returns whether the given media has video. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    public bool HasVideo => Player.HasVideo;

    /// <summary>
    /// Returns whether the given media is currently being buffered. This
    /// applies to network accessed media only.
    /// </summary>
    public bool IsBuffering => Player.IsBuffering;

    /// <summary>
    /// Returns the natural duration of the media. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    public Duration NaturalDuration => Player.NaturalDuration;

    /// <summary>
    /// Returns the natural height of the media in the video. Only valid after
    /// the MediaOpened event has fired.
    /// </summary>
    public int NaturalVideoHeight => Player.NaturalVideoHeight;

    /// <summary>
    /// Returns the natural width of the media in the video. Only valid after
    /// the MediaOpened event has fired.
    /// </summary>
    public int NaturalVideoWidth => Player.NaturalVideoWidth;

    /// <summary>
    /// Returns the current position of the media. This is only valid
    /// after the MediaOpened event has fired.
    /// </summary>
    public TimeSpan Position
    {
        get => Player.Position;

        set => Player.Position = value;
    }

    /// <summary>
    /// Allows the media playback speed ratio to be controlled.
    /// </summary>
    public double SpeedRatio
    {
        get => Player.SpeedRatio;

        set => Player.SpeedRatio = value;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Requests the media is Closed. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    public void Close()
    {
        Player.Close();
        IsMediaOpened = false;
        CurrentState = MediaState.Close;
    }

    /// <summary>
    /// Requests the media is paused. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    public void Pause()
    {
        Player.Pause();
        CurrentState = MediaState.Pause;
    }

    /// <summary>
    /// Requests the media is played. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    public void Play()
    {
        Player.Play();
        if (IsMediaOpened)
        {
            CurrentState = MediaState.Play;
        }
    }

    /// <summary>
    /// Requests the media is stopped. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    public void Stop()
    {
        Player.Stop();
        CurrentState = MediaState.Stop;
    }

    #endregion
}
