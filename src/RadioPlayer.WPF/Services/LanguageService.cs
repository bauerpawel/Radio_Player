using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for managing application language and translations
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly IRadioStationRepository _repository;
    private readonly string _languageDirectory;
    private Dictionary<string, object> _translations = new();
    private string _currentLanguage = "pl"; // Default: Polish
    private const string LanguageSettingKey = "AppLanguage";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public string[] AvailableLanguages { get; } = new[] { "pl", "en", "de" };

    public LanguageService(IRadioStationRepository repository)
    {
        _repository = repository;

        // Get the language directory path
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _languageDirectory = Path.Combine(appDirectory, "lang");

        // Load default language (Polish)
        LoadLanguageFile("pl");
    }

    /// <summary>
    /// Initialize language from database or use default
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var savedLanguage = await _repository.GetSettingAsync(LanguageSettingKey);

            if (!string.IsNullOrEmpty(savedLanguage) && AvailableLanguages.Contains(savedLanguage))
            {
                SetLanguage(savedLanguage);
            }
        }
        catch
        {
            // If loading fails, keep default language
        }
    }

    /// <summary>
    /// Save current language to database
    /// </summary>
    public async Task SaveLanguagePreferenceAsync()
    {
        try
        {
            await _repository.SetSettingAsync(LanguageSettingKey, _currentLanguage);
        }
        catch
        {
            // Silently fail if saving fails
        }
    }

    /// <summary>
    /// Change the current language
    /// </summary>
    public bool SetLanguage(string languageCode)
    {
        if (!AvailableLanguages.Contains(languageCode))
        {
            return false;
        }

        if (_currentLanguage == languageCode)
        {
            return true;
        }

        if (LoadLanguageFile(languageCode))
        {
            _currentLanguage = languageCode;
            OnPropertyChanged(nameof(CurrentLanguage));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get translated string by key path (e.g., "MainWindow.Title")
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            var keys = key.Split('.');
            object? current = _translations;

            foreach (var k in keys)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue(k, out var value))
                    {
                        current = value;
                    }
                    else
                    {
                        return key; // Key not found, return key itself
                    }
                }
                else
                {
                    return key;
                }
            }

            // Convert final value to string
            return current?.ToString() ?? key;
        }
        catch
        {
            return key; // On error, return key itself
        }
    }

    /// <summary>
    /// Get formatted translated string with parameters
    /// </summary>
    public string GetFormattedString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// Load translation file for specified language
    /// </summary>
    private bool LoadLanguageFile(string languageCode)
    {
        try
        {
            var filePath = Path.Combine(_languageDirectory, $"{languageCode}.json");

            if (!File.Exists(filePath))
            {
                return false;
            }

            var json = File.ReadAllText(filePath);

            // Parse as JsonDocument first to handle nested structures properly
            using var document = JsonDocument.Parse(json);
            _translations = ConvertJsonElement(document.RootElement);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Convert JsonElement to Dictionary for proper nested object handling
    /// </summary>
    private Dictionary<string, object> ConvertJsonElement(JsonElement element)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.Object => ConvertJsonElement(property.Value),
                JsonValueKind.Array => property.Value,
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => string.Empty,
                _ => property.Value.ToString()
            };
        }

        return dictionary;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
