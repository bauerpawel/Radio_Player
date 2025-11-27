using System;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for recording radio streams to audio files
/// </summary>
public interface IRadioRecorder : IDisposable
{
    /// <summary>
    /// Whether recording is currently active
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Current recording duration
    /// </summary>
    TimeSpan RecordingDuration { get; }

    /// <summary>
    /// Current recording file path
    /// </summary>
    string? CurrentRecordingPath { get; }

    /// <summary>
    /// Event raised when recording starts
    /// </summary>
    event EventHandler<string>? RecordingStarted;

    /// <summary>
    /// Event raised when recording stops
    /// </summary>
    event EventHandler<RecordingResult>? RecordingStopped;

    /// <summary>
    /// Event raised when recording error occurs
    /// </summary>
    event EventHandler<Exception>? RecordingError;

    /// <summary>
    /// Start recording the current stream
    /// </summary>
    /// <param name="station">Station being recorded</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="format">Output format (WAV, MP3)</param>
    Task StartRecordingAsync(RadioStation station, string outputPath, RecordingFormat format);

    /// <summary>
    /// Stop recording
    /// </summary>
    Task<RecordingResult> StopRecordingAsync();

    /// <summary>
    /// Update current metadata (for tagging)
    /// </summary>
    void UpdateMetadata(IcyMetadata metadata);
}

/// <summary>
/// Recording output format
/// </summary>
public enum RecordingFormat
{
    /// <summary>
    /// WAV (uncompressed PCM)
    /// </summary>
    Wav,

    /// <summary>
    /// MP3 (MPEG-1 Audio Layer 3)
    /// </summary>
    Mp3
}

/// <summary>
/// Recording result information
/// </summary>
public class RecordingResult
{
    /// <summary>
    /// Output file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Recording duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Whether conversion to MP3 was successful (if applicable)
    /// </summary>
    public bool ConversionSuccessful { get; set; } = true;

    /// <summary>
    /// Station that was recorded
    /// </summary>
    public RadioStation? Station { get; set; }

    /// <summary>
    /// Last known metadata
    /// </summary>
    public IcyMetadata? Metadata { get; set; }
}
