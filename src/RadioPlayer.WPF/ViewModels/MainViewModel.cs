using System.Collections.ObjectModel;
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
    private string _nowPlaying = "Select a station to start playing";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isBuffering;

    [ObservableProperty]
    private float _volume = 0.8f;

    [ObservableProperty]
    private string _searchText = string.Empty;

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
            var stations = await _radioBrowserService.GetTopVotedStationsAsync(limit: 100);
            Stations = new ObservableCollection<RadioStation>(stations);
            StatusMessage = $"Loaded {stations.Count} stations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading stations: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchStationsAsync()
    {
        if (_radioBrowserService == null || string.IsNullOrWhiteSpace(SearchText)) return;

        try
        {
            StatusMessage = "Searching...";
            var stations = await _radioBrowserService.SearchStationsAsync(searchTerm: SearchText);
            Stations = new ObservableCollection<RadioStation>(stations);
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
        }
    }

    [RelayCommand]
    private void StopStation()
    {
        _radioPlayer?.Stop();
        StatusMessage = "Stopped";
        NowPlaying = "Select a station to start playing";
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (_repository == null || SelectedStation == null) return;

        try
        {
            if (SelectedStation.IsFavorite)
            {
                await _repository.RemoveFromFavoritesAsync(SelectedStation.Id);
                SelectedStation.IsFavorite = false;
            }
            else
            {
                await _repository.AddToFavoritesAsync(SelectedStation.Id);
                SelectedStation.IsFavorite = true;
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
            var favorites = await _repository.GetFavoriteStationsAsync();
            FavoriteStations = new ObservableCollection<RadioStation>(favorites);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading favorites: {ex.Message}";
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
    }

    partial void OnVolumeChanged(float value)
    {
        if (_radioPlayer != null)
        {
            _radioPlayer.Volume = value;
        }
    }
}
