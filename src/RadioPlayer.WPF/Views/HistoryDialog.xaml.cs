using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services;

namespace RadioPlayer.WPF.Views;

public partial class HistoryDialog : Window
{
    private readonly IRadioStationRepository _repository;
    private List<ListeningHistory> _allHistory = new();
    private List<ListeningHistory> _filteredHistory = new();

    public RadioStation? SelectedStationToPlay { get; private set; }

    public HistoryDialog(IRadioStationRepository repository)
    {
        InitializeComponent();
        _repository = repository;

        Loaded += async (s, e) => await LoadHistoryAsync();
    }

    private async System.Threading.Tasks.Task LoadHistoryAsync()
    {
        try
        {
            // Show loading
            LoadingPanel.Visibility = Visibility.Visible;
            HistoryListView.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;

            // Load history
            _allHistory = await _repository.GetRecentHistoryAsync(200);
            _filteredHistory = _allHistory;

            // Update UI
            LoadingPanel.Visibility = Visibility.Collapsed;

            if (_allHistory.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                TotalRecordsText.Text = "No records";
            }
            else
            {
                HistoryListView.ItemsSource = _filteredHistory;
                HistoryListView.Visibility = Visibility.Visible;
                UpdateTotalRecordsText();
            }
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show(
                $"Error loading history: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void UpdateTotalRecordsText()
    {
        var totalDuration = TimeSpan.FromSeconds(_filteredHistory.Sum(h => h.DurationSeconds));
        var hours = (int)totalDuration.TotalHours;
        var minutes = totalDuration.Minutes;

        TotalRecordsText.Text = _filteredHistory.Count == _allHistory.Count
            ? $"{_filteredHistory.Count} records • Total: {hours}h {minutes}m"
            : $"{_filteredHistory.Count} of {_allHistory.Count} records • Total: {hours}h {minutes}m";
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchTextBox.Text?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredHistory = _allHistory;
        }
        else
        {
            _filteredHistory = _allHistory
                .Where(h => h.Station?.Name?.ToLowerInvariant().Contains(searchText) == true)
                .ToList();
        }

        HistoryListView.ItemsSource = _filteredHistory;
        UpdateTotalRecordsText();

        if (_filteredHistory.Count == 0 && _allHistory.Count > 0)
        {
            HistoryListView.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Visible;
            TotalRecordsText.Text = "No matching records";
        }
        else if (_filteredHistory.Count > 0)
        {
            HistoryListView.Visibility = Visibility.Visible;
            EmptyPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = string.Empty;
        await LoadHistoryAsync();
    }

    private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all playback history? This action cannot be undone.",
            "Clear History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Clear history via repository
                await _repository.ClearListeningHistoryAsync();

                // Reload
                await LoadHistoryAsync();

                MessageBox.Show(
                    "Playback history cleared successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing history: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ListeningHistory history)
        {
            PlayStation(history);
        }
    }

    private void HistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryListView.SelectedItem is ListeningHistory history)
        {
            PlayStation(history);
        }
    }

    private void PlayStation(ListeningHistory history)
    {
        if (history.Station != null)
        {
            SelectedStationToPlay = history.Station;
            DialogResult = true;
            Close();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
