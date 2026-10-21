using System;
using System.Windows;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf;

public interface IMediaElementService : INamedService
{
    ICommand PauseCommand { get; }
    ICommand PlayCommand { get; }
    ICommand StopCommand { get; }

    /// <summary>
    /// Returns the buffering progress of the media.
    /// </summary>
    double BufferingProgress { get; }

    /// <summary>
    /// Returns whether the given media can be paused. This is only valid
    /// after the MediaOpened event has fired.
    /// </summary>
    bool CanPause { get; }

    /// <summary>
    /// Returns the download progress of the media.
    /// </summary>
    double DownloadProgress { get; }

    /// <summary>
    /// Returns whether the given media has audio. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    bool HasAudio { get; }

    /// <summary>
    /// Returns whether the given media has video. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    bool HasVideo { get; }

    /// <summary>
    /// Returns whether the given media is currently being buffered. This
    /// applies to network accessed media only.
    /// </summary>
    bool IsBuffering { get; }

    /// <summary>
    /// Returns the natural duration of the media. Only valid after the
    /// MediaOpened event has fired.
    /// </summary>
    Duration NaturalDuration { get; }

    /// <summary>
    /// Returns the natural height of the media in the video. Only valid after
    /// the MediaOpened event has fired.
    /// </summary>
    int NaturalVideoHeight { get; }

    /// <summary>
    /// Returns the natural width of the media in the video. Only valid after
    /// the MediaOpened event has fired.
    /// </summary>
    int NaturalVideoWidth { get; }

    /// <summary>
    /// Returns the current position of the media. This is only valid
    /// after the MediaOpened event has fired.
    /// </summary>
    TimeSpan Position { get; set; }

    /// <summary>
    /// Allows the media playback speed ratio to be controlled.
    /// </summary>
    double SpeedRatio { get; set; }


    /// <summary>
    /// Requests the media is Closed. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    void Close();

    /// <summary>
    /// Requests the media is paused. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    void Pause();

    /// <summary>
    /// Requests the media is played. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    void Play();

    /// <summary>
    /// Requests the media is stopped. This method only has an effect if the current
    /// media element state is manual.
    /// </summary>
    void Stop();
}
