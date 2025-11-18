using System;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents streaming progress information for UI updates
/// </summary>
public class StreamProgress
{
    /// <summary>
    /// Current buffer duration
    /// </summary>
    public TimeSpan BufferDuration { get; set; }

    /// <summary>
    /// Is currently buffering
    /// </summary>
    public bool IsBuffering { get; set; }

    /// <summary>
    /// Bytes downloaded
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Current bitrate (kbps)
    /// </summary>
    public int CurrentBitrate { get; set; }

    /// <summary>
    /// Connection status message
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;
}
