using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for validating radio stream URLs
/// </summary>
public interface IStreamValidationService
{
    /// <summary>
    /// Validates if a stream URL is accessible and returns stream information
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream validation result with metadata</returns>
    Task<StreamValidationResult> ValidateStreamAsync(string streamUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of stream validation
/// </summary>
public class StreamValidationResult
{
    /// <summary>
    /// Whether the stream is valid and accessible
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detected content type (e.g., "audio/mpeg", "audio/aac")
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Detected codec (e.g., "MP3", "AAC", "OGG")
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Bitrate if available from ICY headers
    /// </summary>
    public int? Bitrate { get; set; }

    /// <summary>
    /// Station name from ICY headers if available
    /// </summary>
    public string? StationName { get; set; }

    /// <summary>
    /// Genre from ICY headers if available
    /// </summary>
    public string? Genre { get; set; }
}
