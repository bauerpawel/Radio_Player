using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Repository for managing radio stations in local SQLite database
/// </summary>
public interface IRadioStationRepository
{
    // Station management
    Task<List<RadioStation>> GetAllStationsAsync();
    Task<RadioStation?> GetStationByIdAsync(int id);
    Task<RadioStation?> GetStationByUuidAsync(string uuid);
    Task<int> AddStationAsync(RadioStation station);
    Task UpdateStationAsync(RadioStation station);
    Task DeleteStationAsync(int id);
    Task<List<RadioStation>> SearchStationsAsync(string searchTerm);
    Task<List<RadioStation>> GetCustomStationsAsync();

    // Favorites management
    Task<List<RadioStation>> GetFavoriteStationsAsync();
    Task AddToFavoritesAsync(int stationId);
    Task RemoveFromFavoritesAsync(int stationId);
    Task<bool> IsFavoriteAsync(int stationId);
    Task UpdateFavoriteSortOrderAsync(int favoriteId, int newSortOrder);

    // Listening history
    Task AddListeningHistoryAsync(ListeningHistory history);
    Task<List<ListeningHistory>> GetRecentHistoryAsync(int limit = 50);
    Task<List<ListeningHistory>> GetHistoryForStationAsync(int stationId);
    Task<Dictionary<int, int>> GetMostListenedStationsAsync(int limit = 10);
    Task ClearListeningHistoryAsync();

    // Database maintenance
    Task InitializeDatabaseAsync();
    Task<long> GetDatabaseSizeAsync();
    Task VacuumDatabaseAsync();

    // Settings management
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);

    // Hotkey settings
    Task<HotkeyConfiguration?> GetHotkeyAsync(string actionId);
    Task SetHotkeyAsync(HotkeyConfiguration hotkey);
    Task<bool> GetHotkeysEnabledAsync();
    Task SetHotkeysEnabledAsync(bool enabled);
    Task<Dictionary<string, HotkeyConfiguration>> GetAllHotkeysAsync();

    // Station caching
    Task BulkAddOrUpdateStationsAsync(IEnumerable<RadioStation> stations);
    Task<List<RadioStation>> GetCachedStationsAsync(int limit = 100, bool excludeCustom = true);
    Task<DateTime?> GetLastCacheUpdateAsync();
    Task UpdateCacheTimestampAsync();
    Task ClearStationCacheAsync();
}
