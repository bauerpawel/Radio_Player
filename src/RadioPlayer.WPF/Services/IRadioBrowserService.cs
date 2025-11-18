using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for interacting with Radio Browser API
/// </summary>
public interface IRadioBrowserService
{
    /// <summary>
    /// Search stations with advanced filtering
    /// </summary>
    /// <param name="searchTerm">Search term for station name</param>
    /// <param name="country">Filter by country</param>
    /// <param name="language">Filter by language</param>
    /// <param name="tag">Filter by tag/genre</param>
    /// <param name="codec">Filter by codec (MP3, AAC, OGG)</param>
    /// <param name="bitrateMin">Minimum bitrate</param>
    /// <param name="limit">Maximum results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching stations</returns>
    Task<List<RadioStation>> SearchStationsAsync(
        string? searchTerm = null,
        string? country = null,
        string? language = null,
        string? tag = null,
        string? codec = null,
        int? bitrateMin = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stations by country
    /// </summary>
    Task<List<RadioStation>> GetStationsByCountryAsync(
        string country,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stations by tag/genre
    /// </summary>
    Task<List<RadioStation>> GetStationsByTagAsync(
        string tag,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top voted stations
    /// </summary>
    Task<List<RadioStation>> GetTopVotedStationsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top clicked stations
    /// </summary>
    Task<List<RadioStation>> GetTopClickedStationsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Register click for a station (call when user starts playback)
    /// </summary>
    Task RegisterClickAsync(
        string stationUuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available countries
    /// </summary>
    Task<List<string>> GetCountriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available languages
    /// </summary>
    Task<List<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available tags/genres
    /// </summary>
    Task<List<string>> GetTagsAsync(CancellationToken cancellationToken = default);
}
