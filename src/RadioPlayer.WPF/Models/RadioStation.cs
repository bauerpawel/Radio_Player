using System;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents a radio station from Radio Browser API
/// </summary>
public class RadioStation
{
    /// <summary>
    /// Database ID (for local storage)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier from Radio Browser API - ALWAYS use UUID instead of 'id' for compatibility
    /// </summary>
    public string StationUuid { get; set; } = string.Empty;

    /// <summary>
    /// Station name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Resolved streaming URL - use this for playback, NOT 'url'
    /// Contains resolved HTTP redirects and playlists
    /// </summary>
    public string UrlResolved { get; set; } = string.Empty;

    /// <summary>
    /// Audio codec (MP3, AAC, AAC+, OGG)
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// Bitrate in kbps
    /// </summary>
    public int Bitrate { get; set; }

    /// <summary>
    /// Country name
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Tags/genres (comma-separated)
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Favicon/logo URL
    /// </summary>
    public string Favicon { get; set; } = string.Empty;

    /// <summary>
    /// Homepage URL
    /// </summary>
    public string Homepage { get; set; } = string.Empty;

    /// <summary>
    /// Station status - 1 = online, 0 = offline
    /// </summary>
    public bool LastCheckOk { get; set; }

    /// <summary>
    /// Number of votes
    /// </summary>
    public int Votes { get; set; }

    /// <summary>
    /// Number of clicks
    /// </summary>
    public int ClickCount { get; set; }

    /// <summary>
    /// Is station active in local database
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when station was added to local database
    /// </summary>
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Is this station in favorites
    /// </summary>
    public bool IsFavorite { get; set; }

    public override string ToString() =>
        $"{Name} ({Country}) - {Codec} {Bitrate}kbps";
}
