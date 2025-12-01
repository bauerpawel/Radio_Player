using System;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for managing station cache updates from Radio Browser API
/// </summary>
public interface IStationCacheService
{
    /// <summary>
    /// Updates the station cache from Radio Browser API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of stations cached</returns>
    Task<int> UpdateCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if cache needs updating based on configured interval
    /// </summary>
    /// <returns>True if cache should be updated</returns>
    Task<bool> ShouldUpdateCacheAsync();

    /// <summary>
    /// Gets the age of the current cache
    /// </summary>
    /// <returns>TimeSpan since last cache update, null if never updated</returns>
    Task<TimeSpan?> GetCacheAgeAsync();

    /// <summary>
    /// Updates cache if needed (based on age and settings)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cache was updated, false if skipped</returns>
    Task<bool> UpdateCacheIfNeededAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the station cache
    /// </summary>
    Task ClearCacheAsync();

    /// <summary>
    /// Event raised when cache update starts
    /// </summary>
    event EventHandler? CacheUpdateStarted;

    /// <summary>
    /// Event raised when cache update completes
    /// </summary>
    event EventHandler<int>? CacheUpdateCompleted;

    /// <summary>
    /// Event raised when cache update fails
    /// </summary>
    event EventHandler<Exception>? CacheUpdateFailed;
}
