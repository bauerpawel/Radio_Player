using System;
using System.Collections.Generic;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Implementation of global hotkey service using NHotkey.Wpf
/// </summary>
public class GlobalHotkeyService : IGlobalHotkeyService
{
    private bool _areHotkeysRegistered;
    private bool _disposed;

    // Hotkey names for registration
    private const string HotkeyPlayPause = "RadioPlayer_PlayPause";
    private const string HotkeyStop = "RadioPlayer_Stop";
    private const string HotkeyNextStation = "RadioPlayer_NextStation";
    private const string HotkeyPreviousStation = "RadioPlayer_PreviousStation";
    private const string HotkeyVolumeUp = "RadioPlayer_VolumeUp";
    private const string HotkeyVolumeDown = "RadioPlayer_VolumeDown";
    private const string HotkeyMute = "RadioPlayer_Mute";

    /// <inheritdoc/>
    public bool AreHotkeysRegistered => _areHotkeysRegistered;

    /// <inheritdoc/>
    public void RegisterHotkeys(
        Action onPlayPause,
        Action onStop,
        Action onNextStation,
        Action onPreviousStation,
        Action onVolumeUp,
        Action onVolumeDown,
        Action onMute)
    {
        if (_areHotkeysRegistered)
        {
            UnregisterHotkeys();
        }

        try
        {
            // Register Play/Pause - Ctrl+Shift+P
            HotkeyManager.Current.AddOrReplace(
                HotkeyPlayPause,
                Key.P,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onPlayPause?.Invoke();
                    e.Handled = true;
                });

            // Register Stop - Ctrl+Shift+S
            HotkeyManager.Current.AddOrReplace(
                HotkeyStop,
                Key.S,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onStop?.Invoke();
                    e.Handled = true;
                });

            // Register Next Station - Ctrl+Shift+Right
            HotkeyManager.Current.AddOrReplace(
                HotkeyNextStation,
                Key.Right,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onNextStation?.Invoke();
                    e.Handled = true;
                });

            // Register Previous Station - Ctrl+Shift+Left
            HotkeyManager.Current.AddOrReplace(
                HotkeyPreviousStation,
                Key.Left,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onPreviousStation?.Invoke();
                    e.Handled = true;
                });

            // Register Volume Up - Ctrl+Shift+Up
            HotkeyManager.Current.AddOrReplace(
                HotkeyVolumeUp,
                Key.Up,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onVolumeUp?.Invoke();
                    e.Handled = true;
                });

            // Register Volume Down - Ctrl+Shift+Down
            HotkeyManager.Current.AddOrReplace(
                HotkeyVolumeDown,
                Key.Down,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onVolumeDown?.Invoke();
                    e.Handled = true;
                });

            // Register Mute - Ctrl+Shift+M
            HotkeyManager.Current.AddOrReplace(
                HotkeyMute,
                Key.M,
                ModifierKeys.Control | ModifierKeys.Shift,
                (sender, e) =>
                {
                    onMute?.Invoke();
                    e.Handled = true;
                });

            _areHotkeysRegistered = true;
            System.Diagnostics.Debug.WriteLine("[GlobalHotkeyService] All global hotkeys registered successfully");
        }
        catch (HotkeyAlreadyRegisteredException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Hotkey already registered: {ex.Message}");
            // Some hotkey is already registered by another application
            // We'll continue anyway - the other hotkeys might still work
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Failed to register hotkeys: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public void RegisterHotkeysWithConfigurations(
        Dictionary<string, HotkeyConfiguration> hotkeyConfigurations,
        Dictionary<string, Action> callbacks)
    {
        if (_areHotkeysRegistered)
        {
            UnregisterHotkeys();
        }

        try
        {
            foreach (var (actionId, config) in hotkeyConfigurations)
            {
                // Skip disabled hotkeys
                if (!config.IsEnabled)
                {
                    System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Skipping disabled hotkey: {actionId}");
                    continue;
                }

                // Get callback for this action
                if (!callbacks.TryGetValue(actionId, out var callback))
                {
                    System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] No callback for action: {actionId}");
                    continue;
                }

                var hotkeyName = $"RadioPlayer_{actionId}";

                try
                {
                    HotkeyManager.Current.AddOrReplace(
                        hotkeyName,
                        config.Key,
                        config.Modifiers,
                        (sender, e) =>
                        {
                            callback?.Invoke();
                            e.Handled = true;
                        });

                    System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Registered hotkey: {actionId} = {config.HotkeyString}");
                }
                catch (HotkeyAlreadyRegisteredException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Hotkey already registered for {actionId}: {ex.Message}");
                    // Continue with other hotkeys
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Failed to register hotkey {actionId}: {ex.Message}");
                    // Continue with other hotkeys
                }
            }

            _areHotkeysRegistered = true;
            System.Diagnostics.Debug.WriteLine("[GlobalHotkeyService] All configured hotkeys registered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Failed to register hotkeys: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public void UnregisterHotkeys()
    {
        if (!_areHotkeysRegistered)
            return;

        try
        {
            HotkeyManager.Current.Remove(HotkeyPlayPause);
            HotkeyManager.Current.Remove(HotkeyStop);
            HotkeyManager.Current.Remove(HotkeyNextStation);
            HotkeyManager.Current.Remove(HotkeyPreviousStation);
            HotkeyManager.Current.Remove(HotkeyVolumeUp);
            HotkeyManager.Current.Remove(HotkeyVolumeDown);
            HotkeyManager.Current.Remove(HotkeyMute);

            _areHotkeysRegistered = false;
            System.Diagnostics.Debug.WriteLine("[GlobalHotkeyService] All global hotkeys unregistered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyService] Error unregistering hotkeys: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        UnregisterHotkeys();
        _disposed = true;
    }
}
