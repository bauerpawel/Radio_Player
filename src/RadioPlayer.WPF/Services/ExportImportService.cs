using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for exporting and importing application data (favorites, settings, custom stations)
/// </summary>
public class ExportImportService : IExportImportService
{
    private readonly IRadioStationRepository _repository;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExportImportService(IRadioStationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task ExportToJsonAsync(string filePath, bool includeCustomStations = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        var backupData = await GetBackupDataAsync(includeCustomStations);

        var json = JsonSerializer.Serialize(backupData, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <inheritdoc />
    public async Task<BackupData> GetBackupDataAsync(bool includeCustomStations = true)
    {
        var backupData = new BackupData
        {
            ExportDate = DateTime.UtcNow,
            Version = "1.0"
        };

        // Export settings
        // Load saved volume from database, fallback to default if not found
        var volumeStr = await _repository.GetSettingAsync("Volume");
        var savedVolume = AppConstants.UI.DefaultVolume;
        if (!string.IsNullOrWhiteSpace(volumeStr) && float.TryParse(volumeStr, out var vol))
        {
            savedVolume = vol;
        }

        backupData.Settings = new BackupSettings
        {
            EnableLogging = AppConstants.Debug.EnableLogging,
            MinimizeToTray = AppConstants.UI.MinimizeToTray,
            BufferDurationSeconds = AppConstants.AudioBuffer.BufferDuration.TotalSeconds,
            PreBufferDurationSeconds = AppConstants.AudioBuffer.PreBufferDuration.TotalSeconds,
            Volume = savedVolume,
            Language = await _repository.GetSettingAsync("Language")
        };

        // Export favorites
        var favoriteStations = await _repository.GetFavoriteStationsAsync();
        backupData.Favorites = favoriteStations.Select((station, index) => new BackupFavorite
        {
            StationUuid = station.StationUuid,
            Name = station.Name,
            Country = station.Country,
            DateAdded = station.DateAdded,
            SortOrder = index,
            IsCustom = station.IsCustom
        }).ToList();

        // Export custom stations (if requested)
        if (includeCustomStations)
        {
            var customStations = await _repository.GetCustomStationsAsync();
            backupData.CustomStations = customStations.Select(station => new BackupStation
            {
                StationUuid = station.StationUuid,
                Name = station.Name,
                UrlResolved = station.UrlResolved,
                Codec = station.Codec,
                Bitrate = station.Bitrate,
                Country = station.Country,
                CountryCode = station.CountryCode,
                Language = station.Language,
                Tags = station.Tags,
                Favicon = station.Favicon,
                Homepage = station.Homepage,
                DateAdded = station.DateAdded
            }).ToList();
        }

        return backupData;
    }

    /// <inheritdoc />
    public async Task ImportFromJsonAsync(string filePath, bool mergeWithExisting = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Backup file not found", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var backupData = JsonSerializer.Deserialize<BackupData>(json, _jsonOptions);

        if (backupData == null)
            throw new InvalidOperationException("Failed to parse backup file");

        // Import settings
        if (backupData.Settings != null)
        {
            AppConstants.Debug.EnableLogging = backupData.Settings.EnableLogging;
            AppConstants.UI.MinimizeToTray = backupData.Settings.MinimizeToTray;
            AppConstants.AudioBuffer.BufferDuration = TimeSpan.FromSeconds(backupData.Settings.BufferDurationSeconds);
            AppConstants.AudioBuffer.PreBufferDuration = TimeSpan.FromSeconds(backupData.Settings.PreBufferDurationSeconds);

            if (!string.IsNullOrWhiteSpace(backupData.Settings.Language))
            {
                await _repository.SetSettingAsync("Language", backupData.Settings.Language);
            }

            // Import volume setting (validate range 0.0 to 1.0)
            if (backupData.Settings.Volume >= 0f && backupData.Settings.Volume <= 1.0f)
            {
                await _repository.SetSettingAsync("Volume", backupData.Settings.Volume.ToString("F2"));
            }
        }

        // Import custom stations first (so they exist when importing favorites)
        if (backupData.CustomStations?.Count > 0)
        {
            foreach (var customStation in backupData.CustomStations)
            {
                // Check if station already exists
                var existingStation = await _repository.GetStationByUuidAsync(customStation.StationUuid);
                if (existingStation == null)
                {
                    var station = new RadioStation
                    {
                        StationUuid = customStation.StationUuid,
                        Name = customStation.Name,
                        UrlResolved = customStation.UrlResolved,
                        Codec = customStation.Codec,
                        Bitrate = customStation.Bitrate,
                        Country = customStation.Country,
                        CountryCode = customStation.CountryCode,
                        Language = customStation.Language,
                        Tags = customStation.Tags,
                        Favicon = customStation.Favicon,
                        Homepage = customStation.Homepage,
                        IsCustom = true,
                        DateAdded = customStation.DateAdded
                    };

                    await _repository.AddStationAsync(station);
                }
            }
        }

        // Import favorites
        if (backupData.Favorites?.Count > 0)
        {
            // If not merging, clear existing favorites first
            if (!mergeWithExisting)
            {
                var existingFavorites = await _repository.GetFavoriteStationsAsync();
                foreach (var favorite in existingFavorites)
                {
                    await _repository.RemoveFromFavoritesAsync(favorite.Id);
                }
            }

            foreach (var favorite in backupData.Favorites.OrderBy(f => f.SortOrder))
            {
                // Ensure the station exists in the database
                var station = await _repository.GetStationByUuidAsync(favorite.StationUuid);

                if (station == null)
                {
                    // Station not in database - skip it or log a warning
                    // In a real scenario, you might want to fetch it from Radio Browser API
                    continue;
                }

                // Check if already a favorite
                var isFavorite = await _repository.IsFavoriteAsync(station.Id);
                if (!isFavorite)
                {
                    await _repository.AddToFavoritesAsync(station.Id);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateBackupFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var json = await File.ReadAllTextAsync(filePath);
            var backupData = JsonSerializer.Deserialize<BackupData>(json, _jsonOptions);

            return backupData != null && !string.IsNullOrWhiteSpace(backupData.Version);
        }
        catch
        {
            return false;
        }
    }
}
