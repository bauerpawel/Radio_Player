using System;
using System.Windows;
using RadioPlayer.WPF.Helpers;

namespace RadioPlayer.WPF.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        BufferSlider.Value = AppConstants.AudioBuffer.BufferDuration.TotalSeconds;
        PreBufferSlider.Value = AppConstants.AudioBuffer.PreBufferDuration.TotalSeconds;
        DebugLoggingCheckBox.IsChecked = AppConstants.Debug.EnableLogging;
        MinimizeToTrayCheckBox.IsChecked = AppConstants.UI.MinimizeToTray;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        AppConstants.AudioBuffer.BufferDuration = TimeSpan.FromSeconds(BufferSlider.Value);
        AppConstants.AudioBuffer.PreBufferDuration = TimeSpan.FromSeconds(PreBufferSlider.Value);
        AppConstants.Debug.EnableLogging = DebugLoggingCheckBox.IsChecked ?? false;
        AppConstants.UI.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;

        MessageBox.Show(
            "Settings saved! Restart playback for changes to take effect.",
            "Settings Saved",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DialogResult = true;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        BufferSlider.Value = 10;
        PreBufferSlider.Value = 3;
        DebugLoggingCheckBox.IsChecked = false;
        MinimizeToTrayCheckBox.IsChecked = false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
