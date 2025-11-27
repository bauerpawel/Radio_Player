using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class AddCustomStationDialog : Window
{
    private readonly IStreamValidationService _validationService;
    private StreamValidationResult? _lastValidationResult;
    private CancellationTokenSource? _validationCts;

    public RadioStation? Station { get; private set; }

    public AddCustomStationDialog(IStreamValidationService validationService)
    {
        _validationService = validationService;
        InitializeComponent();
    }

    private async void TestStreamButton_Click(object sender, RoutedEventArgs e)
    {
        var streamUrl = StreamUrlTextBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            ShowValidationStatus(false, "Please enter a stream URL first.");
            return;
        }

        // Cancel any ongoing validation
        _validationCts?.Cancel();
        _validationCts = new CancellationTokenSource();

        // Show loading state
        TestStreamButton.IsEnabled = false;
        ShowValidationStatus(null, "Testing stream...");

        try
        {
            _lastValidationResult = await _validationService.ValidateStreamAsync(streamUrl, _validationCts.Token);

            if (_lastValidationResult.IsValid)
            {
                var details = $"Codec: {_lastValidationResult.Codec ?? "Unknown"}";
                if (_lastValidationResult.Bitrate.HasValue)
                {
                    details += $"\nBitrate: {_lastValidationResult.Bitrate} kbps";
                }
                if (!string.IsNullOrEmpty(_lastValidationResult.StationName))
                {
                    details += $"\nStation: {_lastValidationResult.StationName}";
                }
                if (!string.IsNullOrEmpty(_lastValidationResult.Genre))
                {
                    details += $"\nGenre: {_lastValidationResult.Genre}";
                }

                ShowValidationStatus(true, $"Stream is valid!\n{details}");

                // Auto-fill fields if available from stream
                if (!string.IsNullOrEmpty(_lastValidationResult.StationName) && string.IsNullOrWhiteSpace(StationNameTextBox.Text))
                {
                    StationNameTextBox.Text = _lastValidationResult.StationName;
                }
                if (!string.IsNullOrEmpty(_lastValidationResult.Genre) && string.IsNullOrWhiteSpace(GenreTextBox.Text))
                {
                    GenreTextBox.Text = _lastValidationResult.Genre;
                }
            }
            else
            {
                ShowValidationStatus(false, $"Stream validation failed:\n{_lastValidationResult.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            ShowValidationStatus(false, "Validation cancelled");
        }
        catch (Exception ex)
        {
            ShowValidationStatus(false, $"Error testing stream:\n{ex.Message}");
        }
        finally
        {
            TestStreamButton.IsEnabled = true;
        }
    }

    private void ShowValidationStatus(bool? isValid, string message)
    {
        ValidationStatusPanel.Visibility = Visibility.Visible;

        if (isValid == null)
        {
            // Loading state
            ValidationStatusPanel.BorderBrush = new SolidColorBrush(Colors.Gray);
            ValidationStatusText.Text = "Testing...";
            ValidationStatusText.Foreground = new SolidColorBrush(Colors.Gray);
        }
        else if (isValid.Value)
        {
            // Success state
            ValidationStatusPanel.BorderBrush = new SolidColorBrush(Colors.Green);
            ValidationStatusText.Text = "✓ Success";
            ValidationStatusText.Foreground = new SolidColorBrush(Colors.Green);
        }
        else
        {
            // Error state
            ValidationStatusPanel.BorderBrush = new SolidColorBrush(Colors.Red);
            ValidationStatusText.Text = "✗ Error";
            ValidationStatusText.Foreground = new SolidColorBrush(Colors.Red);
        }

        ValidationDetailsText.Text = message;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var streamUrl = StreamUrlTextBox.Text?.Trim();
        var stationName = StationNameTextBox.Text?.Trim();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            MessageBox.Show("Stream URL is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            StreamUrlTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(stationName))
        {
            MessageBox.Show("Station name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            StationNameTextBox.Focus();
            return;
        }

        // Validate URL format
        if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show("Please enter a valid HTTP or HTTPS URL.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            StreamUrlTextBox.Focus();
            return;
        }

        // Create station object
        Station = new RadioStation
        {
            StationUuid = Guid.NewGuid().ToString(), // Generate unique UUID for custom station
            Name = stationName,
            UrlResolved = streamUrl,
            Favicon = LogoUrlTextBox.Text?.Trim() ?? string.Empty,
            Tags = GenreTextBox.Text?.Trim() ?? string.Empty,
            Country = CountryTextBox.Text?.Trim() ?? string.Empty,
            Language = LanguageTextBox.Text?.Trim() ?? string.Empty,
            Homepage = HomepageTextBox.Text?.Trim() ?? string.Empty,
            IsCustom = true,
            IsActive = true,
            DateAdded = DateTime.UtcNow
        };

        // Use validation result if available
        if (_lastValidationResult?.IsValid == true)
        {
            Station.Codec = _lastValidationResult.Codec ?? "Unknown";
            Station.Bitrate = _lastValidationResult.Bitrate ?? 0;
        }
        else
        {
            Station.Codec = "Unknown";
            Station.Bitrate = 0;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _validationCts?.Cancel();
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _validationCts?.Cancel();
        _validationCts?.Dispose();
        base.OnClosed(e);
    }
}
