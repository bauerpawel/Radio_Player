using System;
using System.Collections.Generic;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Root container for backup/export data
/// </summary>
public class BackupData
{
    /// <summary>
    /// Export format version (for future compatibility)
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Date and time when backup was created (UTC)
    /// </summary>
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Application settings
    /// </summary>
    public BackupSettings Settings { get; set; } = new();

    /// <summary>
    /// Favorite stations with metadata
    /// </summary>
    public List<BackupFavorite> Favorites { get; set; } = new();

    /// <summary>
    /// Custom stations added by user (optional)
    /// </summary>
    public List<BackupStation> CustomStations { get; set; } = new();
}

/// <summary>
/// Settings data for backup
/// </summary>
public class BackupSettings
{
    /// <summary>
    /// Debug logging enabled
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Minimize to system tray
    /// </summary>
    public bool MinimizeToTray { get; set; }

    /// <summary>
    /// Buffer duration in seconds
    /// </summary>
    public double BufferDurationSeconds { get; set; }

    /// <summary>
    /// Pre-buffer duration in seconds
    /// </summary>
    public double PreBufferDurationSeconds { get; set; }

    /// <summary>
    /// Language preference (e.g., "en", "pl")
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Volume level (0.0 to 1.0)
    /// </summary>
    public float Volume { get; set; }
}

/// <summary>
/// Favorite station data for backup
/// </summary>
public class BackupFavorite
{
    /// <summary>
    /// Station UUID (stable identifier across databases)
    /// </summary>
    public string StationUuid { get; set; } = string.Empty;

    /// <summary>
    /// Station name (for human readability)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Country (for reference)
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Date when added to favorites
    /// </summary>
    public DateTime DateAdded { get; set; }

    /// <summary>
    /// Sort order for custom arrangement
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Is this a custom station (not from Radio Browser API)
    /// </summary>
    public bool IsCustom { get; set; }
}

/// <summary>
/// Custom station data for backup (user-added stations)
/// </summary>
public class BackupStation
{
    /// <summary>
    /// Station UUID
    /// </summary>
    public string StationUuid { get; set; } = string.Empty;

    /// <summary>
    /// Station name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Streaming URL
    /// </summary>
    public string UrlResolved { get; set; } = string.Empty;

    /// <summary>
    /// Audio codec (MP3, AAC, OGG, etc.)
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
    /// Date when added
    /// </summary>
    public DateTime DateAdded { get; set; }
}
