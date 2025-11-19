using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class StationDetailsDialog : Window
{
    private readonly RadioStation _station;
    private readonly IRadioBrowserService? _radioBrowserService;

    public RadioStation? SelectedStation { get; private set; }
    public bool ShouldPlayStation { get; private set; }

    public StationDetailsDialog(RadioStation station, IRadioBrowserService? radioBrowserService = null)
    {
        InitializeComponent();
        _station = station;
        _radioBrowserService = radioBrowserService;

        LoadStationDetails();

        if (_radioBrowserService != null)
        {
            _ = LoadSimilarStationsAsync();
        }
        else
        {
            SimilarStationsLoadingText.Visibility = Visibility.Collapsed;
            NoSimilarStationsText.Visibility = Visibility.Visible;
        }
    }

    private void LoadStationDetails()
    {
        // Station Name and Tagline
        StationNameText.Text = _station.Name;
        StationTaglineText.Text = string.IsNullOrWhiteSpace(_station.Tags)
            ? "Internet Radio Station"
            : _station.Tags.Split(',').FirstOrDefault()?.Trim() ?? "Internet Radio Station";

        // Load favicon if available
        if (!string.IsNullOrWhiteSpace(_station.Favicon))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_station.Favicon);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                StationLogoImage.Source = bitmap;
            }
            catch
            {
                // If favicon fails to load, just hide it
                StationLogoImage.Source = null;
            }
        }

        // Basic Information
        CountryText.Text = string.IsNullOrWhiteSpace(_station.Country) ? "Unknown" : _station.Country;
        LanguageText.Text = string.IsNullOrWhiteSpace(_station.Language) ? "Unknown" : _station.Language;
        TagsText.Text = string.IsNullOrWhiteSpace(_station.Tags) ? "No tags" : _station.Tags;

        // Homepage
        if (!string.IsNullOrWhiteSpace(_station.Homepage))
        {
            HomepageLinkText.Text = _station.Homepage;
            HomepageLink.NavigateUri = new Uri(_station.Homepage);
            HomepageText.Visibility = Visibility.Visible;
        }
        else
        {
            HomepageText.Text = "Not available";
            HomepageLink.NavigateUri = null;
        }

        // Technical Information
        CodecText.Text = string.IsNullOrWhiteSpace(_station.Codec) ? "Unknown" : _station.Codec.ToUpperInvariant();
        BitrateText.Text = _station.Bitrate > 0 ? $"{_station.Bitrate} kbps" : "Unknown";
        StreamUrlText.Text = _station.UrlResolved ?? "Not available";
    }

    private async Task LoadSimilarStationsAsync()
    {
        if (_radioBrowserService == null)
            return;

        try
        {
            SimilarStationsLoadingText.Visibility = Visibility.Visible;
            SimilarStationsList.Visibility = Visibility.Collapsed;
            NoSimilarStationsText.Visibility = Visibility.Collapsed;

            // Get similar stations based on tags
            List<RadioStation> similarStations = new();

            // Try to find stations with same tags
            if (!string.IsNullOrWhiteSpace(_station.Tags))
            {
                var tags = _station.Tags.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Take(2) // Use first 2 tags
                    .ToList();

                foreach (var tag in tags)
                {
                    var stations = await _radioBrowserService.SearchStationsAsync(
                        searchTerm: null,
                        country: null,
                        language: null,
                        tag: tag,
                        codec: null,
                        limit: 10);

                    similarStations.AddRange(stations);
                }
            }

            // Remove duplicates and the current station
            similarStations = similarStations
                .GroupBy(s => s.StationUuid)
                .Select(g => g.First())
                .Where(s => s.StationUuid != _station.StationUuid)
                .OrderByDescending(s => s.Votes)
                .Take(5)
                .ToList();

            SimilarStationsLoadingText.Visibility = Visibility.Collapsed;

            if (similarStations.Count > 0)
            {
                SimilarStationsList.ItemsSource = similarStations;
                SimilarStationsList.Visibility = Visibility.Visible;
            }
            else
            {
                NoSimilarStationsText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading similar stations: {ex.Message}");
            SimilarStationsLoadingText.Visibility = Visibility.Collapsed;
            NoSimilarStationsText.Text = "Error loading similar stations";
            NoSimilarStationsText.Visibility = Visibility.Visible;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open URL: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PlayStationButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedStation = _station;
        ShouldPlayStation = true;
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SimilarStationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SimilarStationsList.SelectedItem is RadioStation station)
        {
            SelectedStation = station;
            ShouldPlayStation = true;
            DialogResult = true;
            Close();
        }
    }
}
