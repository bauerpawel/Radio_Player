using System;
using System.ComponentModel;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for managing application language and translations
/// </summary>
public interface ILanguageService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the current language code (e.g., "pl", "en", "de")
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets the list of available language codes
    /// </summary>
    string[] AvailableLanguages { get; }

    /// <summary>
    /// Gets a translated string by key path (e.g., "MainWindow.Title")
    /// </summary>
    /// <param name="key">The translation key path</param>
    /// <returns>Translated string or the key if not found</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a formatted translated string by key path with parameters
    /// </summary>
    /// <param name="key">The translation key path</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Formatted translated string</returns>
    string GetFormattedString(string key, params object[] args);

    /// <summary>
    /// Changes the current language
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "pl", "en", "de")</param>
    /// <returns>True if language was changed successfully</returns>
    bool SetLanguage(string languageCode);

    /// <summary>
    /// Loads the language preference from database or uses default
    /// </summary>
    System.Threading.Tasks.Task InitializeAsync();

    /// <summary>
    /// Saves the current language preference to database
    /// </summary>
    System.Threading.Tasks.Task SaveLanguagePreferenceAsync();

    /// <summary>
    /// Event raised when language changes
    /// </summary>
    event EventHandler? LanguageChanged;
}
