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
    private readonly ILanguageService? _languageService;
    private readonly IStreamValidationService? _streamValidationService;
    private readonly IExportImportService? _exportImportService;

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

    [ObservableProperty]
    private ObservableCollection<string> _availableCountries = new();

    // Playback history tracking
    private DateTime? _currentPlaybackStartTime;
    private int? _currentPlaybackStationId;

    public MainViewModel()
    {
        // Constructor for design-time support
    }

    public MainViewModel(
        IRadioBrowserService radioBrowserService,
        IRadioStationRepository repository,
        IRadioPlayer radioPlayer,
        ILanguageService languageService,
        IStreamValidationService streamValidationService,
        IExportImportService exportImportService)
    {
        _radioBrowserService = radioBrowserService;
        _repository = repository;
        _radioPlayer = radioPlayer;
        _languageService = languageService;
        _streamValidationService = streamValidationService;
        _exportImportService = exportImportService;

        // Subscribe to player events
        if (_radioPlayer != null)
        {
            _radioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _radioPlayer.MetadataReceived += OnMetadataReceived;
            _radioPlayer.ProgressUpdated += OnProgressUpdated;
            _radioPlayer.ErrorOccurred += OnErrorOccurred;
        }

        // Subscribe to language change events
        if (_languageService != null)
        {
            _languageService.LanguageChanged += OnLanguageChanged;
        }

        // Load available countries for the filter
        _ = LoadAvailableCountriesAsync();
    }

    /// <summary>
    /// Gets the language service for accessing translated strings
    /// </summary>
    public ILanguageService? Lang => _languageService;

    /// <summary>
    /// Called when the application language changes
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Notify all properties that depend on translations
        OnPropertyChanged(nameof(Lang));
    }

    [RelayCommand]
    private async Task LoadAvailableCountriesAsync()
    {
        if (_radioBrowserService == null) return;

        try
        {
            var countries = await _radioBrowserService.GetCountriesAsync();

            // Add empty option for "All countries"
            AvailableCountries.Clear();
            AvailableCountries.Add("");

            foreach (var country in countries)
            {
                AvailableCountries.Add(country);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading countries: {ex.Message}");
            // Fail silently - user can still type country name manually
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
                    country: string.IsNullOrWhiteSpace(CountryFilter) ? null : CountryFilter.Trim(),
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
                country: string.IsNullOrWhiteSpace(CountryFilter) ? null : CountryFilter.Trim(),
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
            // End previous playback session if any
            await EndCurrentPlaybackHistoryAsync();

            StatusMessage = "Connecting...";
            CurrentlyPlayingStation = SelectedStation;
            CurrentTrack = "Loading...";
            await _radioPlayer.PlayAsync(SelectedStation);

            // Register click with Radio Browser API
            if (_radioBrowserService != null)
            {
                await _radioBrowserService.RegisterClickAsync(SelectedStation.StationUuid);
            }

            // Start new playback history session
            await StartPlaybackHistoryAsync(SelectedStation);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing: {ex.Message}";
            CurrentlyPlayingStation = null;
            CurrentTrack = "No track information";
        }
    }

    [RelayCommand]
    private async Task StopStationAsync()
    {
        // End current playback history session
        await EndCurrentPlaybackHistoryAsync();

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
        var settingsDialog = new Views.SettingsDialog(_languageService, _exportImportService);
        settingsDialog.ShowDialog();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var aboutDialog = new Views.AboutDialog();
        aboutDialog.ShowDialog();
    }

    [RelayCommand]
    private void ShowStreamInfo()
    {
        if (_radioPlayer == null) return;

        var streamInfoDialog = new Views.StreamInfoDialog(SelectedStation, _radioPlayer, NowPlaying);
        streamInfoDialog.ShowDialog();
    }

    [RelayCommand]
    private async Task ShowStationDetailsAsync(RadioStation? station)
    {
        if (station == null) return;

        var detailsDialog = new Views.StationDetailsDialog(station, _radioBrowserService);
        var result = detailsDialog.ShowDialog();

        // If user clicked "Play Station" or double-clicked a similar station
        if (result == true && detailsDialog.ShouldPlayStation && detailsDialog.SelectedStation != null)
        {
            SelectedStation = detailsDialog.SelectedStation;
            await PlayStationAsync();
        }
    }

    [RelayCommand]
    private async Task ShowHistoryAsync()
    {
        if (_repository == null) return;

        var historyDialog = new Views.HistoryDialog(_repository);
        var result = historyDialog.ShowDialog();

        // If user selected a station to play from history
        if (result == true && historyDialog.SelectedStationToPlay != null)
        {
            SelectedStation = historyDialog.SelectedStationToPlay;
            await PlayStationAsync();
        }
    }

    [RelayCommand]
    private async Task AddCustomStationAsync()
    {
        if (_repository == null || _streamValidationService == null) return;

        try
        {
            // Show add custom station dialog
            var dialog = new Views.AddCustomStationDialog(_streamValidationService);
            var result = dialog.ShowDialog();

            if (result == true && dialog.Station != null)
            {
                // Add station to database
                dialog.Station.Id = await _repository.AddStationAsync(dialog.Station);

                StatusMessage = $"Custom station '{dialog.Station.Name}' added successfully";

                // Reload custom stations
                await LoadCustomStationsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding custom station: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadCustomStationsAsync()
    {
        if (_repository == null) return;

        try
        {
            StatusMessage = "Loading custom stations...";

            var customStations = await _repository.GetCustomStationsAsync();
            await UpdateFavoriteStatusAsync(customStations);

            Stations = new ObservableCollection<RadioStation>(customStations);
            UpdateCurrentlyPlayingStationReference();

            StatusMessage = customStations.Count == 0
                ? "No custom stations yet. Add one!"
                : $"Loaded {customStations.Count} custom stations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading custom stations: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteCustomStationAsync()
    {
        if (_repository == null || SelectedStation == null) return;

        // Only allow deleting custom stations
        if (!SelectedStation.IsCustom)
        {
            StatusMessage = "Only custom stations can be deleted";
            return;
        }

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{SelectedStation.Name}'?",
                "Delete Custom Station",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Stop playback if this station is currently playing
                if (CurrentlyPlayingStation?.StationUuid == SelectedStation.StationUuid && IsPlaying)
                {
                    await StopStationAsync();
                }

                await _repository.DeleteStationAsync(SelectedStation.Id);

                StatusMessage = $"Custom station '{SelectedStation.Name}' deleted";

                // Reload custom stations
                await LoadCustomStationsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting custom station: {ex.Message}";
        }
    }

    /// <summary>
    /// Start tracking playback history for the current station
    /// </summary>
    private async Task StartPlaybackHistoryAsync(RadioStation station)
    {
        if (_repository == null) return;

        try
        {
            // Ensure station is in database
            var existingStation = await _repository.GetStationByUuidAsync(station.StationUuid);
            if (existingStation == null)
            {
                // Add station to database
                station.Id = await _repository.AddStationAsync(station);
            }
            else
            {
                station.Id = existingStation.Id;
            }

            // Start tracking
            _currentPlaybackStartTime = DateTime.Now;
            _currentPlaybackStationId = station.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting playback history: {ex.Message}");
        }
    }

    /// <summary>
    /// End current playback history session and save to database
    /// </summary>
    private async Task EndCurrentPlaybackHistoryAsync()
    {
        if (_repository == null || _currentPlaybackStartTime == null || _currentPlaybackStationId == null)
            return;

        try
        {
            var endTime = DateTime.Now;
            var duration = (int)(endTime - _currentPlaybackStartTime.Value).TotalSeconds;

            // Only save if played for at least 5 seconds
            if (duration >= 5)
            {
                var history = new ListeningHistory
                {
                    StationId = _currentPlaybackStationId.Value,
                    StartTime = _currentPlaybackStartTime.Value,
                    DurationSeconds = duration,
                    DateRecorded = DateTime.Now
                };

                await _repository.AddListeningHistoryAsync(history);
            }

            // Reset tracking
            _currentPlaybackStartTime = null;
            _currentPlaybackStationId = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ending playback history: {ex.Message}");
        }
    }
}
