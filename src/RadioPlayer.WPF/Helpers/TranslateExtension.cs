using System;
using System.Windows.Markup;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// XAML Markup Extension for translating strings using the ILanguageService
/// Usage: Text="{helpers:Translate MainWindow.Title}"
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class TranslateExtension : MarkupExtension
{
    /// <summary>
    /// The translation key path (e.g., "MainWindow.Title")
    /// </summary>
    public string Key { get; set; }

    public TranslateExtension()
    {
        Key = string.Empty;
    }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
        {
            return "[Missing Key]";
        }

        // Get the language service from App
        var languageService = App.CurrentLanguageService;

        if (languageService == null)
        {
            // Fallback to key if service not available
            return Key;
        }

        // Get the translated string
        return languageService.GetString(Key);
    }
}
