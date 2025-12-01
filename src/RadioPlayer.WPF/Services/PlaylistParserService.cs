using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RadioPlayer.WPF.Helpers;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for parsing internet radio playlist files (PLS, M3U, M3U8)
/// </summary>
public class PlaylistParserService : IPlaylistParserService
{
    private readonly HttpClient _httpClient;
    private const int DownloadTimeoutSeconds = 10;

    public PlaylistParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsPlaylistUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var lowerUrl = url.ToLowerInvariant();
        return lowerUrl.EndsWith(".pls") ||
               lowerUrl.EndsWith(".m3u") ||
               lowerUrl.EndsWith(".m3u8") ||
               lowerUrl.Contains(".pls?") ||
               lowerUrl.Contains(".m3u?") ||
               lowerUrl.Contains(".m3u8?");
    }

    public async Task<PlaylistParseResult> ParsePlaylistAsync(string playlistUrl, CancellationToken cancellationToken = default)
    {
        var result = new PlaylistParseResult();

        try
        {
            DebugLogger.Log("PLAYLIST", $"Parsing playlist: {playlistUrl}");

            // Validate URL
            if (!Uri.TryCreate(playlistUrl, UriKind.Absolute, out var uri))
            {
                result.ErrorMessage = "Invalid playlist URL";
                return result;
            }

            // Create request with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(DownloadTimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, playlistUrl);
            request.Headers.Add("User-Agent", AppConstants.HttpHeaders.UserAgent);

            // Download playlist content
            using var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (string.IsNullOrWhiteSpace(content))
            {
                result.ErrorMessage = "Playlist file is empty";
                return result;
            }

            DebugLogger.Log("PLAYLIST", $"Downloaded {content.Length} bytes");

            // Detect playlist type and parse
            var lowerUrl = playlistUrl.ToLowerInvariant();
            if (lowerUrl.Contains(".pls"))
            {
                result.PlaylistType = "PLS";
                result.StreamUrls = ParsePlsPlaylist(content);
            }
            else if (lowerUrl.Contains(".m3u"))
            {
                result.PlaylistType = lowerUrl.Contains(".m3u8") ? "M3U8" : "M3U";
                result.StreamUrls = ParseM3uPlaylist(content);
            }
            else
            {
                // Try to auto-detect from content
                if (content.TrimStart().StartsWith("[playlist]", StringComparison.OrdinalIgnoreCase))
                {
                    result.PlaylistType = "PLS";
                    result.StreamUrls = ParsePlsPlaylist(content);
                }
                else if (content.Contains("#EXTM3U") || content.Contains("http://") || content.Contains("https://"))
                {
                    result.PlaylistType = "M3U";
                    result.StreamUrls = ParseM3uPlaylist(content);
                }
                else
                {
                    result.ErrorMessage = "Unable to detect playlist format";
                    return result;
                }
            }

            if (result.StreamUrls.Count == 0)
            {
                result.ErrorMessage = "No stream URLs found in playlist";
                return result;
            }

            DebugLogger.Log("PLAYLIST", $"Found {result.StreamUrls.Count} stream URL(s) in {result.PlaylistType} playlist");
            result.IsSuccess = true;
            return result;
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = cancellationToken.IsCancellationRequested
                ? "Playlist download cancelled"
                : $"Playlist download timeout after {DownloadTimeoutSeconds} seconds";
            return result;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Failed to download playlist: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error parsing playlist: {ex.Message}";
            DebugLogger.Log("PLAYLIST_ERROR", $"Parse error: {ex}");
            return result;
        }
    }

    private List<string> ParsePlsPlaylist(string content)
    {
        var urls = new List<string>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // PLS format: File1=http://...
            if (trimmedLine.StartsWith("File", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    var url = parts[1].Trim();
                    if (IsValidStreamUrl(url))
                    {
                        urls.Add(url);
                        DebugLogger.Log("PLAYLIST", $"Found stream URL: {url}");
                    }
                }
            }
        }

        return urls;
    }

    private List<string> ParseM3uPlaylist(string content)
    {
        var urls = new List<string>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip comments and metadata
            if (trimmedLine.StartsWith("#"))
                continue;

            // Check if line is a URL
            if (IsValidStreamUrl(trimmedLine))
            {
                urls.Add(trimmedLine);
                DebugLogger.Log("PLAYLIST", $"Found stream URL: {trimmedLine}");
            }
        }

        return urls;
    }

    private bool IsValidStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
