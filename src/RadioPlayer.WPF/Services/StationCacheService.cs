using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for managing station cache updates from Radio Browser API
/// </summary>
public class StationCacheService : IStationCacheService
{
    private readonly IRadioBrowserService _radioBrowserService;
    private readonly IRadioStationRepository _repository;

    // Default cache settings
    private const int DefaultCacheHours = 24; // Update cache every 24 hours
    private const int DefaultStationsToCache = 500; // Cache top 500 stations

    public event EventHandler? CacheUpdateStarted;
    public event EventHandler<int>? CacheUpdateCompleted;
    public event EventHandler<Exception>? CacheUpdateFailed;

    public StationCacheService(
        IRadioBrowserService radioBrowserService,
        IRadioStationRepository repository)
    {
        _radioBrowserService = radioBrowserService ?? throw new ArgumentNullException(nameof(radioBrowserService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Updates the station cache from Radio Browser API
    /// </summary>
    public async Task<int> UpdateCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CacheUpdateStarted?.Invoke(this, EventArgs.Empty);

            // Get cache update count from settings, or use default
            var cacheCountSetting = await _repository.GetSettingAsync("CacheStationCount");
            var stationsToCache = int.TryParse(cacheCountSetting, out var count) ? count : DefaultStationsToCache;

            var allStations = new List<RadioStation>();

            // Fetch top voted stations
            var topVotedStations = await _radioBrowserService.GetTopVotedStationsAsync(
                limit: stationsToCache / 2,
                cancellationToken: cancellationToken);
            allStations.AddRange(topVotedStations);

            // Fetch top clicked stations
            var topClickedStations = await _radioBrowserService.GetTopClickedStationsAsync(
                limit: stationsToCache / 2,
                cancellationToken: cancellationToken);
            allStations.AddRange(topClickedStations);

            // Remove duplicates based on StationUuid
            var uniqueStations = allStations
                .GroupBy(s => s.StationUuid)
                .Select(g => g.First())
                .ToList();

            // Bulk insert/update stations in database
            await _repository.BulkAddOrUpdateStationsAsync(uniqueStations);

            // Update cache timestamp
            await _repository.UpdateCacheTimestampAsync();

            CacheUpdateCompleted?.Invoke(this, uniqueStations.Count);

            return uniqueStations.Count;
        }
        catch (Exception ex)
        {
            CacheUpdateFailed?.Invoke(this, ex);
            throw;
        }
    }

    /// <summary>
    /// Checks if cache needs updating based on configured interval
    /// </summary>
    public async Task<bool> ShouldUpdateCacheAsync()
    {
        // Check if auto-update is enabled
        var autoUpdateEnabled = await _repository.GetSettingAsync("CacheAutoUpdate");
        if (autoUpdateEnabled == "false")
        {
            return false;
        }

        // Get cache age
        var cacheAge = await GetCacheAgeAsync();

        // If never cached, should update
        if (cacheAge == null)
        {
            return true;
        }

        // Get cache update interval from settings
        var cacheIntervalSetting = await _repository.GetSettingAsync("CacheUpdateIntervalHours");
        var updateIntervalHours = int.TryParse(cacheIntervalSetting, out var hours) ? hours : DefaultCacheHours;

        // Check if cache is older than interval
        return cacheAge.Value.TotalHours >= updateIntervalHours;
    }

    /// <summary>
    /// Gets the age of the current cache
    /// </summary>
    public async Task<TimeSpan?> GetCacheAgeAsync()
    {
        var lastUpdate = await _repository.GetLastCacheUpdateAsync();

        if (lastUpdate == null)
        {
            return null;
        }

        return DateTime.UtcNow - lastUpdate.Value;
    }

    /// <summary>
    /// Updates cache if needed (based on age and settings)
    /// </summary>
    public async Task<bool> UpdateCacheIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (await ShouldUpdateCacheAsync())
        {
            await UpdateCacheAsync(cancellationToken);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears the station cache
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await _repository.ClearStationCacheAsync();
    }
}
