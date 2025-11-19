using System;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents listening history entry
/// </summary>
public class ListeningHistory
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
    /// When listening started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Date when recorded
    /// </summary>
    public DateTime DateRecorded { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to RadioStation
    /// </summary>
    public RadioStation? Station { get; set; }

    /// <summary>
    /// Formatted duration for display
    /// </summary>
    public string DurationFormatted
    {
        get
        {
            var duration = TimeSpan.FromSeconds(DurationSeconds);
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else if (duration.TotalMinutes >= 1)
            {
                return $"{duration.Minutes}m {duration.Seconds}s";
            }
            else
            {
                return $"{duration.Seconds}s";
            }
        }
    }
}
