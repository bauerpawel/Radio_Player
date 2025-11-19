using System.Windows;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class StreamInfoDialog : Window
{
    public StreamInfoDialog(RadioStation? station, IRadioPlayer player, string nowPlaying)
    {
        InitializeComponent();

        if (station == null)
        {
            StationNameText.Text = "No station selected";
            return;
        }

        // Station Info
        StationNameText.Text = station.Name;
        CountryText.Text = station.Country ?? "Unknown";
        LanguageText.Text = station.Language ?? "Unknown";
        TagsText.Text = station.Tags ?? "None";
        HomepageText.Text = station.Homepage ?? "N/A";
        UrlText.Text = station.UrlResolved;

        // Stream Technical Info
        CodecText.Text = station.Codec?.ToUpperInvariant() ?? "Unknown";
        BitrateText.Text = station.Bitrate > 0 ? $"{station.Bitrate} kbps" : "Unknown";

        // Player Info (if available)
        StateText.Text = player.State.ToString();

        // Now Playing
        NowPlayingText.Text = string.IsNullOrWhiteSpace(nowPlaying)
            ? "No metadata available"
            : nowPlaying;

        // Audio Format Information (from active stream)
        if (player.SampleRate > 0)
        {
            SampleRateText.Text = $"{player.SampleRate} Hz";
        }
        else
        {
            SampleRateText.Text = player.State == Services.PlaybackState.Playing || player.State == Services.PlaybackState.Buffering
                ? "Detecting..."
                : "Not available";
        }

        if (player.Channels > 0)
        {
            ChannelsText.Text = player.Channels == 1 ? "Mono (1 channel)" : $"Stereo ({player.Channels} channels)";
        }
        else
        {
            ChannelsText.Text = player.State == Services.PlaybackState.Playing || player.State == Services.PlaybackState.Buffering
                ? "Detecting..."
                : "Not available";
        }

        // Buffer Status
        if (player.State == Services.PlaybackState.Playing || player.State == Services.PlaybackState.Buffering)
        {
            var bufferPercentage = (int)(player.BufferFillPercentage * 100);
            var bufferSeconds = player.BufferedDuration.TotalSeconds;
            BufferText.Text = $"{bufferPercentage}% ({bufferSeconds:F1}s buffered)";
        }
        else
        {
            BufferText.Text = "Not active";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
