using System;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents ICY/Shoutcast metadata extracted from radio stream
/// </summary>
public class IcyMetadata
{
    /// <summary>
    /// Full stream title (usually "Artist - Title")
    /// </summary>
    public string StreamTitle { get; set; } = string.Empty;

    /// <summary>
    /// Stream URL (if provided)
    /// </summary>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>
    /// Artist name (parsed from StreamTitle)
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Track title (parsed from StreamTitle)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when metadata was received
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Parse StreamTitle into Artist and Title
    /// Typical format: "Artist - Title"
    /// </summary>
    public void ParseStreamTitle()
    {
        if (string.IsNullOrWhiteSpace(StreamTitle))
            return;

        var parts = StreamTitle.Split(new[] { " - " }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            Artist = parts[0].Trim();
            Title = parts[1].Trim();
        }
        else
        {
            // No separator found, put everything in Title
            Title = StreamTitle.Trim();
            Artist = string.Empty;
        }
    }

    public override string ToString() =>
        !string.IsNullOrWhiteSpace(Artist) && !string.IsNullOrWhiteSpace(Title)
            ? $"{Artist} - {Title}"
            : StreamTitle;
}
