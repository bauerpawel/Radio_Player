using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for parsing internet radio playlist files (PLS, M3U, M3U8)
/// </summary>
public interface IPlaylistParserService
{
    /// <summary>
    /// Parses a playlist URL and extracts stream URLs
    /// </summary>
    /// <param name="playlistUrl">URL to the playlist file (.pls, .m3u, .m3u8)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stream URLs found in the playlist</returns>
    Task<PlaylistParseResult> ParsePlaylistAsync(string playlistUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a URL is likely a playlist file
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>True if the URL appears to be a playlist</returns>
    bool IsPlaylistUrl(string url);
}

/// <summary>
/// Result of playlist parsing
/// </summary>
public class PlaylistParseResult
{
    /// <summary>
    /// Whether parsing was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if parsing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of stream URLs extracted from the playlist
    /// </summary>
    public List<string> StreamUrls { get; set; } = new();

    /// <summary>
    /// The type of playlist detected (PLS, M3U, M3U8)
    /// </summary>
    public string? PlaylistType { get; set; }
}
