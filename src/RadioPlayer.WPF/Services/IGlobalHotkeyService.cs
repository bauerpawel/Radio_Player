using System;
using System.Collections.Generic;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for managing global hotkeys (keyboard shortcuts that work even when the application is minimized)
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>
    /// Registers all global hotkeys with the specified callbacks
    /// </summary>
    /// <param name="onPlayPause">Callback for Play/Pause hotkey</param>
    /// <param name="onStop">Callback for Stop hotkey</param>
    /// <param name="onNextStation">Callback for Next Station hotkey</param>
    /// <param name="onPreviousStation">Callback for Previous Station hotkey</param>
    /// <param name="onVolumeUp">Callback for Volume Up hotkey</param>
    /// <param name="onVolumeDown">Callback for Volume Down hotkey</param>
    /// <param name="onMute">Callback for Mute/Unmute hotkey</param>
    void RegisterHotkeys(
        Action onPlayPause,
        Action onStop,
        Action onNextStation,
        Action onPreviousStation,
        Action onVolumeUp,
        Action onVolumeDown,
        Action onMute);

    /// <summary>
    /// Registers global hotkeys with custom configurations
    /// </summary>
    /// <param name="hotkeyConfigurations">Dictionary of action IDs to hotkey configurations</param>
    /// <param name="callbacks">Dictionary of action IDs to callback actions</param>
    void RegisterHotkeysWithConfigurations(
        Dictionary<string, HotkeyConfiguration> hotkeyConfigurations,
        Dictionary<string, Action> callbacks);

    /// <summary>
    /// Unregisters all global hotkeys
    /// </summary>
    void UnregisterHotkeys();

    /// <summary>
    /// Gets whether hotkeys are currently registered
    /// </summary>
    bool AreHotkeysRegistered { get; }
}
