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

        // Note: Sample Rate and Channels would need to be exposed by IRadioPlayer
        SampleRateText.Text = "Available during playback";
        ChannelsText.Text = "Available during playback";
        BufferText.Text = player.State == NAudio.Wave.PlaybackState.Playing ? "Playing" : "Stopped";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
