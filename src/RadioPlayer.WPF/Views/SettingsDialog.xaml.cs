using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class SettingsDialog : Window
{
    private readonly ILanguageService? _languageService;
    private readonly IExportImportService? _exportImportService;

    public SettingsDialog(ILanguageService? languageService = null, IExportImportService? exportImportService = null)
    {
        _languageService = languageService;
        _exportImportService = exportImportService;
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

        var message = _languageService?.GetString("Messages.SettingsSaved")
            ?? "Settings saved successfully!\n\nNote: Some language changes may require application restart to take full effect.";
        var title = _languageService?.GetString("Messages.Success") ?? "Settings Saved";

        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

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

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_exportImportService == null)
        {
            MessageBox.Show("Export service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = _languageService?.GetString("Settings.ExportBackup") ?? "Export Backup",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"RadioPlayer_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Disable button during export
                var button = (Button)sender;
                button.IsEnabled = false;

                try
                {
                    await _exportImportService.ExportToJsonAsync(saveFileDialog.FileName, includeCustomStations: true);

                    var message = _languageService?.GetString("Messages.ExportSuccess")
                        ?? $"Backup exported successfully!\n\nFile: {saveFileDialog.FileName}";
                    var title = _languageService?.GetString("Messages.Success") ?? "Export Successful";

                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export backup:\n\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_exportImportService == null)
        {
            MessageBox.Show("Import service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = _languageService?.GetString("Settings.ImportBackup") ?? "Import Backup",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Validate the backup file first
                if (!await _exportImportService.ValidateBackupFileAsync(openFileDialog.FileName))
                {
                    MessageBox.Show(
                        "The selected file is not a valid backup file.",
                        "Invalid Backup",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Ask user if they want to merge or replace
                var mergeMessage = _languageService?.GetString("Messages.ImportMergeQuestion")
                    ?? "Do you want to merge with existing favorites?\n\nYes: Add to existing favorites\nNo: Replace all favorites\nCancel: Abort import";
                var mergeTitle = _languageService?.GetString("Messages.ImportMode") ?? "Import Mode";

                var result = MessageBox.Show(mergeMessage, mergeTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                bool mergeWithExisting = (result == MessageBoxResult.Yes);

                // Disable button during import
                var button = (Button)sender;
                button.IsEnabled = false;

                try
                {
                    await _exportImportService.ImportFromJsonAsync(openFileDialog.FileName, mergeWithExisting);

                    // Reload settings
                    LoadCurrentSettings();

                    var message = _languageService?.GetString("Messages.ImportSuccess")
                        ?? "Backup imported successfully!\n\nSettings and favorites have been restored.";
                    var title = _languageService?.GetString("Messages.Success") ?? "Import Successful";

                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to import backup:\n\n{ex.Message}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
