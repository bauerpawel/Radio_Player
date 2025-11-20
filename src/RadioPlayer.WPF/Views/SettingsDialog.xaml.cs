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
        DebugLoggingCheckBox.IsChecked = AppConstants.Debug.EnableLogging;
        MinimizeToTrayCheckBox.IsChecked = AppConstants.UI.MinimizeToTray;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        AppConstants.Debug.EnableLogging = DebugLoggingCheckBox.IsChecked ?? false;
        AppConstants.UI.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;

        MessageBox.Show(
            "Settings saved successfully!",
            "Settings Saved",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DialogResult = true;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLoggingCheckBox.IsChecked = false;
        MinimizeToTrayCheckBox.IsChecked = false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
