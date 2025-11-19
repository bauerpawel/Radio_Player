using System;
using System.IO;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Simple debug logger that writes timestamped messages to a file
/// </summary>
public static class DebugLogger
{
    private static readonly object _lockObject = new object();

    /// <summary>
    /// Write a debug log message
    /// </summary>
    public static void Log(string message)
    {
        if (!AppConstants.Debug.EnableLogging)
            return;

        try
        {
            lock (_lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";

                // Ensure directory exists
                var directory = Path.GetDirectoryName(AppConstants.Debug.LogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Append to log file
                File.AppendAllText(AppConstants.Debug.LogFilePath, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail - don't break the app if logging fails
        }
    }

    /// <summary>
    /// Write a debug log message with category
    /// </summary>
    public static void Log(string category, string message)
    {
        Log($"[{category}] {message}");
    }

    /// <summary>
    /// Clear the log file
    /// </summary>
    public static void ClearLog()
    {
        if (!AppConstants.Debug.EnableLogging)
            return;

        try
        {
            lock (_lockObject)
            {
                if (File.Exists(AppConstants.Debug.LogFilePath))
                {
                    File.Delete(AppConstants.Debug.LogFilePath);
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }
}
