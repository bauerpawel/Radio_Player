using System;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents a favorite radio station
/// </summary>
public class Favorite
{
    /// <summary>
    /// Database ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to RadioStation ID
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// Date when added to favorites
    /// </summary>
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sort order for custom arrangement
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to RadioStation
    /// </summary>
    public RadioStation? Station { get; set; }
}
