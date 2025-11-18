using System;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Application-wide constants and configuration values
/// </summary>
public static class AppConstants
{
    // Radio Browser API configuration
    public const string RadioBrowserApiBaseUrl = "https://de1.api.radio-browser.info";
    public const string RadioBrowserDnsLookup = "all.api.radio-browser.info";

    // Audio buffering configuration (based on research recommendations)
    public static class AudioBuffer
    {
        /// <summary>
        /// Total buffer duration (5-10 seconds recommended)
        /// </summary>
        public static readonly TimeSpan BufferDuration = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Minimum buffer before starting playback (2 seconds)
        /// </summary>
        public static readonly TimeSpan PreBufferDuration = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Chunk size per read (8-16 KB recommended)
        /// </summary>
        public const int ChunkSize = 16384; // 16 KB

        /// <summary>
        /// Buffer check interval (250ms = 4x per second)
        /// </summary>
        public static readonly TimeSpan BufferCheckInterval = TimeSpan.FromMilliseconds(250);
    }

    // Network resilience configuration
    public static class Network
    {
        /// <summary>
        /// Number of retry attempts (5 recommended)
        /// </summary>
        public const int RetryAttempts = 5;

        /// <summary>
        /// HTTP request timeout (30 seconds)
        /// </summary>
        public static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Base delay for exponential backoff (2^n seconds)
        /// </summary>
        public const int ExponentialBackoffBase = 2;
    }

    // Database configuration
    public static class Database
    {
        /// <summary>
        /// SQLite database filename
        /// </summary>
        public const string DatabaseFileName = "radioplayer.db";

        /// <summary>
        /// Database location (use LocalApplicationData)
        /// </summary>
        public static string DatabasePath =>
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RadioPlayer",
                DatabaseFileName);

        /// <summary>
        /// WAL mode for better concurrency
        /// </summary>
        public const bool UseWalMode = true;

        /// <summary>
        /// Connection pooling (enabled by default in v6.0+)
        /// </summary>
        public const bool UseConnectionPooling = true;
    }

    // UI configuration
    public static class UI
    {
        /// <summary>
        /// Default volume (0.0 to 1.0)
        /// </summary>
        public const float DefaultVolume = 0.8f;

        /// <summary>
        /// Number of stations to display per page
        /// </summary>
        public const int StationsPerPage = 50;

        /// <summary>
        /// Maximum recent history items
        /// </summary>
        public const int MaxRecentHistoryItems = 50;
    }

    // HTTP headers for Radio Browser API
    public static class HttpHeaders
    {
        public const string UserAgent = "RadioPlayer/1.0";
        public const string IcyMetadata = "1";
    }
}
