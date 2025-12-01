using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for validating radio stream URLs
/// </summary>
public class StreamValidationService : IStreamValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IPlaylistParserService _playlistParser;
    private const int ValidationTimeoutSeconds = 10;
    private const int MinBytesToRead = 4096; // Read at least 4KB to verify stream works

    public StreamValidationService(HttpClient httpClient, IPlaylistParserService playlistParser)
    {
        _httpClient = httpClient;
        _playlistParser = playlistParser;
    }

    public async Task<StreamValidationResult> ValidateStreamAsync(string streamUrl, CancellationToken cancellationToken = default)
    {
        var result = new StreamValidationResult();

        try
        {
            // Validate URL format
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                result.ErrorMessage = "Stream URL is empty";
                return result;
            }

            if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                result.ErrorMessage = "Invalid URL format. Must be HTTP or HTTPS.";
                return result;
            }

            // Check if URL is a playlist file
            if (_playlistParser.IsPlaylistUrl(streamUrl))
            {
                var playlistResult = await _playlistParser.ParsePlaylistAsync(streamUrl, cancellationToken);

                if (!playlistResult.IsSuccess)
                {
                    result.ErrorMessage = $"Playlist error: {playlistResult.ErrorMessage}";
                    return result;
                }

                if (playlistResult.StreamUrls.Count == 0)
                {
                    result.ErrorMessage = "No stream URLs found in playlist";
                    return result;
                }

                // Validate the first stream URL from the playlist
                var firstStreamUrl = playlistResult.StreamUrls[0];
                result.ResolvedStreamUrl = firstStreamUrl;

                // Note: We'll recursively validate the resolved URL
                return await ValidateStreamAsync(firstStreamUrl, cancellationToken);
            }

            // Create request with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(ValidationTimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
            request.Headers.Add("User-Agent", "RadioPlayer/1.0");
            request.Headers.Add("Icy-MetaData", "1"); // Request ICY metadata

            // Send request
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                return result;
            }

            // Extract metadata from headers
            result.ContentType = response.Content.Headers.ContentType?.MediaType;

            // Extract ICY headers if present
            if (response.Headers.TryGetValues("icy-name", out var names))
            {
                result.StationName = string.Join("", names);
            }

            if (response.Headers.TryGetValues("icy-genre", out var genres))
            {
                result.Genre = string.Join("", genres);
            }

            if (response.Headers.TryGetValues("icy-br", out var bitrates))
            {
                var bitrateStr = string.Join("", bitrates);
                if (int.TryParse(bitrateStr, out var bitrate))
                {
                    result.Bitrate = bitrate;
                }
            }

            // Determine codec from content type
            result.Codec = DetermineCodecFromContentType(result.ContentType);

            // Try to read some data to verify stream actually works
            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            var buffer = new byte[MinBytesToRead];
            var totalBytesRead = 0;
            var readAttempts = 0;
            const int maxReadAttempts = 3;

            while (totalBytesRead < MinBytesToRead && readAttempts < maxReadAttempts && !cts.Token.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, MinBytesToRead - totalBytesRead), cts.Token);
                if (bytesRead == 0)
                {
                    break; // End of stream
                }

                totalBytesRead += bytesRead;
                readAttempts++;
            }

            if (totalBytesRead == 0)
            {
                result.ErrorMessage = "Stream returned no data";
                return result;
            }

            // If codec not determined from headers, try to detect from data
            if (string.IsNullOrEmpty(result.Codec))
            {
                result.Codec = DetectCodecFromData(buffer, totalBytesRead);
            }

            result.IsValid = true;
            return result;
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = cancellationToken.IsCancellationRequested
                ? "Validation cancelled"
                : $"Connection timeout after {ValidationTimeoutSeconds} seconds";
            return result;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Connection error: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
            return result;
        }
    }

    private string DetermineCodecFromContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return "Unknown";
        }

        return contentType.ToLowerInvariant() switch
        {
            var ct when ct.Contains("mpeg") || ct.Contains("mp3") => "MP3",
            var ct when ct.Contains("aac") || ct.Contains("aacp") => "AAC",
            var ct when ct.Contains("ogg") => "OGG",
            var ct when ct.Contains("flac") => "FLAC",
            var ct when ct.Contains("wav") => "WAV",
            _ => contentType
        };
    }

    private string DetectCodecFromData(byte[] buffer, int length)
    {
        if (length < 4)
        {
            return "Unknown";
        }

        // Check for MP3 frame sync (0xFF 0xFB or 0xFF 0xFA or 0xFF 0xF3 or 0xFF 0xF2)
        for (int i = 0; i < length - 1; i++)
        {
            if (buffer[i] == 0xFF && (buffer[i + 1] & 0xE0) == 0xE0)
            {
                return "MP3";
            }
        }

        // Check for OGG signature (OggS)
        if (length >= 4 &&
            buffer[0] == 0x4F && buffer[1] == 0x67 &&
            buffer[2] == 0x67 && buffer[3] == 0x53)
        {
            return "OGG";
        }

        // Check for FLAC signature (fLaC)
        if (length >= 4 &&
            buffer[0] == 0x66 && buffer[1] == 0x4C &&
            buffer[2] == 0x61 && buffer[3] == 0x43)
        {
            return "FLAC";
        }

        // Check for AAC ADTS header (0xFF 0xFX where X >= 0xF0)
        for (int i = 0; i < length - 1; i++)
        {
            if (buffer[i] == 0xFF && (buffer[i + 1] & 0xF0) == 0xF0)
            {
                return "AAC";
            }
        }

        return "Unknown";
    }
}
