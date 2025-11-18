using System;
using System.Threading;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for playing internet radio streams
/// </summary>
public interface IRadioPlayer : IDisposable
{
    /// <summary>
    /// Current playback state
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Current volume (0.0 to 1.0)
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Is muted
    /// </summary>
    bool IsMuted { get; set; }

    /// <summary>
    /// Currently playing station
    /// </summary>
    RadioStation? CurrentStation { get; }

    /// <summary>
    /// Event raised when playback state changes
    /// </summary>
    event EventHandler<PlaybackState>? PlaybackStateChanged;

    /// <summary>
    /// Event raised when metadata is received from stream
    /// </summary>
    event EventHandler<IcyMetadata>? MetadataReceived;

    /// <summary>
    /// Event raised for streaming progress updates
    /// </summary>
    event EventHandler<StreamProgress>? ProgressUpdated;

    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Start playing a radio station
    /// </summary>
    Task PlayAsync(RadioStation station, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop playback
    /// </summary>
    void Stop();

    /// <summary>
    /// Pause playback (if supported)
    /// </summary>
    void Pause();

    /// <summary>
    /// Resume playback
    /// </summary>
    void Resume();
}

/// <summary>
/// Playback state enumeration
/// </summary>
public enum PlaybackState
{
    Stopped,
    Connecting,
    Buffering,
    Playing,
    Paused,
    Error
}
