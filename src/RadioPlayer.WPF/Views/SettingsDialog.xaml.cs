using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class SettingsDialog : Window
{
    private readonly ILanguageService? _languageService;

    public SettingsDialog(ILanguageService? languageService = null)
    {
        _languageService = languageService;
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        DebugLoggingCheckBox.IsChecked = AppConstants.Debug.EnableLogging;
        MinimizeToTrayCheckBox.IsChecked = AppConstants.UI.MinimizeToTray;

        // Load current language
        if (_languageService != null)
        {
            var currentLang = _languageService.CurrentLanguage;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentLang)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // If no selection, default to first item (Polish)
            if (LanguageComboBox.SelectedItem == null && LanguageComboBox.Items.Count > 0)
            {
                LanguageComboBox.SelectedIndex = 0;
            }
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        AppConstants.Debug.EnableLogging = DebugLoggingCheckBox.IsChecked ?? false;
        AppConstants.UI.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;

        // Save language preference
        if (_languageService != null && LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var languageCode = selectedItem.Tag?.ToString() ?? "pl";
            if (_languageService.SetLanguage(languageCode))
            {
                await _languageService.SaveLanguagePreferenceAsync();
            }
        }

        MessageBox.Show(
            "Settings saved successfully!\n\nNote: Some language changes may require application restart to take full effect.",
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

        // Reset to default language (Polish)
        if (LanguageComboBox.Items.Count > 0)
        {
            LanguageComboBox.SelectedIndex = 0; // Polish is first
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
