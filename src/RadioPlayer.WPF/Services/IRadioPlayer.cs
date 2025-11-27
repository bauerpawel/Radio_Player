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
    /// Current buffer duration (how much audio is buffered)
    /// </summary>
    TimeSpan BufferedDuration { get; }

    /// <summary>
    /// Buffer fill percentage (0.0 to 1.0)
    /// </summary>
    double BufferFillPercentage { get; }

    /// <summary>
    /// Sample rate in Hz (0 if not available)
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Number of audio channels (0 if not available)
    /// </summary>
    int Channels { get; }

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
    /// Event raised when PCM audio data is available (for recording)
    /// </summary>
    event EventHandler<PcmDataEventArgs>? PcmDataAvailable;

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

/// <summary>
/// PCM audio data event arguments (for recording)
/// </summary>
public class PcmDataEventArgs : EventArgs
{
    /// <summary>
    /// PCM audio data
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Offset in the data array
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Number of bytes
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Wave format of the PCM data
    /// </summary>
    public NAudio.Wave.WaveFormat WaveFormat { get; }

    public PcmDataEventArgs(byte[] data, int offset, int count, NAudio.Wave.WaveFormat waveFormat)
    {
        Data = data;
        Offset = offset;
        Count = count;
        WaveFormat = waveFormat;
    }
}
