using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IRadioBrowserService? _radioBrowserService;
    private readonly IRadioStationRepository? _repository;
    private readonly IRadioPlayer? _radioPlayer;

    [ObservableProperty]
    private ObservableCollection<RadioStation> _stations = new();

    [ObservableProperty]
    private ObservableCollection<RadioStation> _favoriteStations = new();

    [ObservableProperty]
    private RadioStation? _selectedStation;

    [ObservableProperty]
    private RadioStation? _currentlyPlayingStation;

    [ObservableProperty]
    private string _currentTrack = "No track information";

    [ObservableProperty]
    private string _nowPlaying = "Select a station to start playing";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isBuffering;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private float _volume = 0.8f;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _countryFilter = string.Empty;

    [ObservableProperty]
    private string _languageFilter = string.Empty;

    [ObservableProperty]
    private string _genreFilter = string.Empty;

    [ObservableProperty]
    private string _codecFilter = string.Empty;

    public MainViewModel()
    {
        // Constructor for design-time support
    }

    public MainViewModel(
        IRadioBrowserService radioBrowserService,
        IRadioStationRepository repository,
        IRadioPlayer radioPlayer)
    {
        _radioBrowserService = radioBrowserService;
        _repository = repository;
        _radioPlayer = radioPlayer;

        // Subscribe to player events
        if (_radioPlayer != null)
        {
            _radioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _radioPlayer.MetadataReceived += OnMetadataReceived;
            _radioPlayer.ProgressUpdated += OnProgressUpdated;
            _radioPlayer.ErrorOccurred += OnErrorOccurred;
        }
    }

    [RelayCommand]
    private async Task LoadTopStationsAsync()
    {
        if (_radioBrowserService == null) return;

        try
        {
            StatusMessage = "Loading top stations...";

            // If filters are active, use SearchStationsAsync instead
            if (!string.IsNullOrWhiteSpace(CountryFilter) ||
                !string.IsNullOrWhiteSpace(LanguageFilter) ||
                !string.IsNullOrWhiteSpace(GenreFilter) ||
                !string.IsNullOrWhiteSpace(CodecFilter))
            {
                var stations = await _radioBrowserService.SearchStationsAsync(
                    searchTerm: null,
                    country: NormalizeFilter(CountryFilter),
                    language: NormalizeFilter(LanguageFilter),
                    tag: NormalizeFilter(GenreFilter),
                    codec: NormalizeFilter(CodecFilter),
                    limit: 100);

                await UpdateFavoriteStatusAsync(stations);
                Stations = new ObservableCollection<RadioStation>(stations);
                UpdateCurrentlyPlayingStationReference();
                StatusMessage = $"Loaded {stations.Count} stations with filters";
            }
            else
            {
                var stations = await _radioBrowserService.GetTopVotedStationsAsync(limit: 100);
                await UpdateFavoriteStatusAsync(stations);
                Stations = new ObservableCollection<RadioStation>(stations);
                UpdateCurrentlyPlayingStationReference();
                StatusMessage = $"Loaded {stations.Count} stations";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading stations: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchStationsAsync()
    {
        if (_radioBrowserService == null) return;

        // Allow search with just filters, even without search text
        if (string.IsNullOrWhiteSpace(SearchText) &&
            string.IsNullOrWhiteSpace(CountryFilter) &&
            string.IsNullOrWhiteSpace(LanguageFilter) &&
            string.IsNullOrWhiteSpace(GenreFilter) &&
            string.IsNullOrWhiteSpace(CodecFilter))
        {
            StatusMessage = "Please enter search text or select filters";
            return;
        }

        try
        {
            StatusMessage = "Searching...";
            var stations = await _radioBrowserService.SearchStationsAsync(
                searchTerm: NormalizeFilter(SearchText),
                country: NormalizeFilter(CountryFilter),
                language: NormalizeFilter(LanguageFilter),
                tag: NormalizeFilter(GenreFilter),
                codec: NormalizeFilter(CodecFilter),
                limit: 100);

            // Update favorite status for stations that exist in local database
            await UpdateFavoriteStatusAsync(stations);

            Stations = new ObservableCollection<RadioStation>(stations);
            UpdateCurrentlyPlayingStationReference();
            StatusMessage = $"Found {stations.Count} stations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PlayStationAsync()
    {
        if (_radioPlayer == null || SelectedStation == null) return;

        try
        {
            StatusMessage = "Connecting...";
            CurrentlyPlayingStation = SelectedStation;
            CurrentTrack = "Loading...";
            await _radioPlayer.PlayAsync(SelectedStation);

            // Register click with Radio Browser API
            if (_radioBrowserService != null)
            {
                await _radioBrowserService.RegisterClickAsync(SelectedStation.StationUuid);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing: {ex.Message}";
            CurrentlyPlayingStation = null;
            CurrentTrack = "No track information";
        }
    }

    [RelayCommand]
    private void StopStation()
    {
        _radioPlayer?.Stop();
        StatusMessage = "Stopped";
        CurrentlyPlayingStation = null;
        CurrentTrack = "No track information";
        NowPlaying = "Select a station to start playing";
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (_repository == null || SelectedStation == null) return;

        try
        {
            // Ensure station exists in local database before adding to favorites
            // Stations from Radio Browser API don't have local database ID
            if (SelectedStation.Id == 0)
            {
                // Check if station already exists in database by UUID
                var existingStation = await _repository.GetStationByUuidAsync(SelectedStation.StationUuid);

                if (existingStation != null)
                {
                    // Station exists, use its ID
                    SelectedStation.Id = existingStation.Id;
                }
                else
                {
                    // Station doesn't exist, add it to local database first
                    var newId = await _repository.AddStationAsync(SelectedStation);
                    SelectedStation.Id = newId;
                }
            }

            // Now toggle favorite status
            if (SelectedStation.IsFavorite)
            {
                await _repository.RemoveFromFavoritesAsync(SelectedStation.Id);
                SelectedStation.IsFavorite = false;
                StatusMessage = $"Removed {SelectedStation.Name} from favorites";
            }
            else
            {
                await _repository.AddToFavoritesAsync(SelectedStation.Id);
                SelectedStation.IsFavorite = true;
                StatusMessage = $"Added {SelectedStation.Name} to favorites";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating favorite: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadFavoritesAsync()
    {
        if (_repository == null) return;

        try
        {
            StatusMessage = "Loading favorites...";
            var favorites = await _repository.GetFavoriteStationsAsync();
            Stations = new ObservableCollection<RadioStation>(favorites);
            UpdateCurrentlyPlayingStationReference();
            StatusMessage = favorites.Count > 0
                ? $"Loaded {favorites.Count} favorite stations"
                : "No favorite stations yet";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading favorites: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates IsFavorite status and local database ID for stations loaded from API
    /// </summary>
    private async Task UpdateFavoriteStatusAsync(List<RadioStation> stations)
    {
        if (_repository == null || stations == null || stations.Count == 0) return;

        try
        {
            foreach (var station in stations)
            {
                // Check if station exists in local database by UUID
                var existingStation = await _repository.GetStationByUuidAsync(station.StationUuid);

                if (existingStation != null)
                {
                    // Station exists in database - update ID and check if it's a favorite
                    station.Id = existingStation.Id;
                    station.IsFavorite = await _repository.IsFavoriteAsync(existingStation.Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating favorite status: {ex.Message}");
        }
    }

    /// <summary>
    /// Normalizes filter text for case-insensitive API searches
    /// </summary>
    private static string? NormalizeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return null;

        return filter.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Updates CurrentlyPlayingStation reference to match the new station instance in the list
    /// This is needed because when we reload stations, we get new object instances
    /// but we want to keep the same station selected (by UUID) for the playing indicator
    /// </summary>
    private void UpdateCurrentlyPlayingStationReference()
    {
        if (CurrentlyPlayingStation == null || Stations == null || Stations.Count == 0)
            return;

        // Find the station in the new list that matches the currently playing station by UUID
        var matchingStation = Stations.FirstOrDefault(s => s.StationUuid == CurrentlyPlayingStation.StationUuid);

        if (matchingStation != null)
        {
            // Update to the new instance so the visual indicator works
            CurrentlyPlayingStation = matchingStation;
        }
    }

    // Event handlers for RadioPlayer events
    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        IsPlaying = state == PlaybackState.Playing;
        IsBuffering = state == PlaybackState.Buffering;

        StatusMessage = state switch
        {
            PlaybackState.Stopped => "Stopped",
            PlaybackState.Connecting => "Connecting...",
            PlaybackState.Buffering => "Buffering...",
            PlaybackState.Playing => "Playing",
            PlaybackState.Paused => "Paused",
            PlaybackState.Error => "Error occurred",
            _ => "Unknown"
        };
    }

    private void OnMetadataReceived(object? sender, IcyMetadata metadata)
    {
        CurrentTrack = metadata.ToString();
        NowPlaying = metadata.ToString();
    }

    private void OnProgressUpdated(object? sender, StreamProgress progress)
    {
        if (progress.IsBuffering)
        {
            StatusMessage = $"Buffering... {progress.BufferDuration.TotalSeconds:F1}s";
        }
    }

    private void OnErrorOccurred(object? sender, Exception ex)
    {
        StatusMessage = $"Error: {ex.Message}";
        IsPlaying = false;
        CurrentlyPlayingStation = null;
        CurrentTrack = "No track information";
    }

    partial void OnVolumeChanged(float value)
    {
        if (_radioPlayer != null)
        {
            _radioPlayer.Volume = value;
        }
    }

    partial void OnIsPlayingChanged(bool value)
    {
        CanStop = IsPlaying || IsBuffering;
    }

    partial void OnIsBufferingChanged(bool value)
    {
        CanStop = IsPlaying || IsBuffering;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsDialog = new Views.SettingsDialog();
        settingsDialog.ShowDialog();
    }

    [RelayCommand]
    private void ShowStreamInfo()
    {
        if (_radioPlayer == null) return;

        var streamInfoDialog = new Views.StreamInfoDialog(SelectedStation, _radioPlayer, NowPlaying);
        streamInfoDialog.ShowDialog();
    }
}
