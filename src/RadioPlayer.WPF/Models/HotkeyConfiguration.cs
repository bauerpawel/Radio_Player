using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace RadioPlayer.WPF.Models;

/// <summary>
/// Represents a global hotkey configuration
/// </summary>
public class HotkeyConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this hotkey action
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key for this hotkey (e.g., Key.P, Key.S)
    /// </summary>
    public Key Key { get; set; }

    /// <summary>
    /// Gets or sets the modifier keys (e.g., ModifierKeys.Control | ModifierKeys.Shift)
    /// </summary>
    public ModifierKeys Modifiers { get; set; }

    /// <summary>
    /// Gets or sets whether this hotkey is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the display name for this hotkey action
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the hotkey as a formatted string (e.g., "Ctrl+Shift+P")
    /// </summary>
    public string HotkeyString
    {
        get
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }
    }

    /// <summary>
    /// Creates a default hotkey configuration
    /// </summary>
    public static HotkeyConfiguration CreateDefault(string actionId, Key key, ModifierKeys modifiers, string displayName)
    {
        return new HotkeyConfiguration
        {
            ActionId = actionId,
            Key = key,
            Modifiers = modifiers,
            IsEnabled = true,
            DisplayName = displayName
        };
    }

    /// <summary>
    /// Gets all default hotkey configurations
    /// </summary>
    public static Dictionary<string, HotkeyConfiguration> GetDefaults()
    {
        var defaults = new Dictionary<string, HotkeyConfiguration>
        {
            ["PlayPause"] = CreateDefault("PlayPause", Key.P, ModifierKeys.Control | ModifierKeys.Shift, "Play/Pause"),
            ["Stop"] = CreateDefault("Stop", Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Stop"),
            ["NextStation"] = CreateDefault("NextStation", Key.Right, ModifierKeys.Control | ModifierKeys.Shift, "Next Station"),
            ["PreviousStation"] = CreateDefault("PreviousStation", Key.Left, ModifierKeys.Control | ModifierKeys.Shift, "Previous Station"),
            ["VolumeUp"] = CreateDefault("VolumeUp", Key.Up, ModifierKeys.Control | ModifierKeys.Shift, "Volume Up"),
            ["VolumeDown"] = CreateDefault("VolumeDown", Key.Down, ModifierKeys.Control | ModifierKeys.Shift, "Volume Down"),
            ["Mute"] = CreateDefault("Mute", Key.M, ModifierKeys.Control | ModifierKeys.Shift, "Mute/Unmute")
        };

        return defaults;
    }

    /// <summary>
    /// Converts the hotkey to a serializable string format for database storage
    /// Format: "ActionId|Key|Modifiers|IsEnabled|DisplayName"
    /// </summary>
    public string ToStorageString()
    {
        return $"{ActionId}|{(int)Key}|{(int)Modifiers}|{IsEnabled}|{DisplayName}";
    }

    /// <summary>
    /// Creates a hotkey configuration from a storage string
    /// </summary>
    public static HotkeyConfiguration? FromStorageString(string storageString)
    {
        if (string.IsNullOrWhiteSpace(storageString))
            return null;

        var parts = storageString.Split('|');
        if (parts.Length < 5)
            return null;

        try
        {
            return new HotkeyConfiguration
            {
                ActionId = parts[0],
                Key = (Key)int.Parse(parts[1]),
                Modifiers = (ModifierKeys)int.Parse(parts[2]),
                IsEnabled = bool.Parse(parts[3]),
                DisplayName = parts[4]
            };
        }
        catch
        {
            return null;
        }
    }
}
