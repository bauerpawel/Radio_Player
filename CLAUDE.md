# CLAUDE.md - AI Assistant Guide for Radio Player

This document provides comprehensive guidance for AI assistants working with the Radio Player codebase. It explains the project structure, development workflows, key conventions, and important considerations when making changes.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Architecture Patterns](#architecture-patterns)
5. [Code Conventions](#code-conventions)
6. [Development Workflows](#development-workflows)
7. [Key Components](#key-components)
8. [Testing Guidelines](#testing-guidelines)
9. [Common Tasks](#common-tasks)
10. [Important Considerations](#important-considerations)

---

## Project Overview

**Radio Player** is a desktop internet radio application built with .NET 10 and WPF. It provides access to 51,000+ radio stations worldwide through the Radio Browser API.

### Core Features
- Multi-format audio streaming (MP3, AAC, OGG/Vorbis, OGG/Opus, OGG/FLAC)
- Stream recording to WAV/MP3 with ICY metadata tagging
- Station browsing, search, and favorites management
- Custom station support - add your own radio stations
- Backup/Export and Import of settings and favorites
- ICY metadata parsing for "Now Playing" information
- Listening history tracking
- Multi-language interface (English, German, Polish)
- Material Design UI with system tray support
- Content filtering - automatic filtering of Russian/Belarusian stations
- Playback time tracking with visual display (HH:MM:SS)
- Global hotkeys - system-wide keyboard shortcuts that work even when minimized
- Column sorting - sortable station list columns
- Station context menu - right-click actions for stations
- Volume persistence - volume level saved between sessions
- Favorite button in station list - quick favorite toggle per station

### Key Statistics
- **Version**: 1.5
- **Lines of Code**: ~10,000 lines
- **Files**: 50 C# files, 8 XAML views
- **Framework**: .NET 10 (WPF)
- **Architecture**: MVVM with Dependency Injection
- **Database**: SQLite with ADO.NET
- **License**: Apache-2.0

---

## Technology Stack

### Core Framework
- **.NET 10** (`net10.0-windows`) - Latest .NET framework
- **WPF** - Windows Presentation Foundation for UI
- **C# 12** - Latest C# language features with nullable reference types enabled

### NuGet Packages

#### Audio Streaming
- **NAudio 2.2.1** - Core audio library (MIT license)
- **NAudio.Vorbis 1.5.0** - OGG/Vorbis decoder
- **Concentus 1.1.0** - OGG/Opus decoder (pure C#)

#### Native Libraries
- **libFLAC 1.5.0+** - FLAC decoder via P/Invoke (required for OGG/FLAC)
  - Location: `libs/flac/libFLAC.dll`
  - Auto-copied to output directory during build
  - Optional: app works without it, but OGG/FLAC streams won't play

#### Database
- **Microsoft.Data.Sqlite 10.0.0** - SQLite ADO.NET provider
- **Microsoft.EntityFrameworkCore.Sqlite 10.0.0** - EF Core SQLite (referenced but using ADO.NET directly)

#### HTTP & Resilience
- **Polly 8.5.0** - Retry policies and resilience patterns

#### MVVM & DI
- **CommunityToolkit.Mvvm 8.4.0** - Modern MVVM framework (RelayCommand, ObservableObject)
- **Microsoft.Extensions.DependencyInjection 10.0.0** - Dependency injection container

#### UI
- **MaterialDesignThemes 5.1.0** - Material Design styling
- **MaterialDesignColors 3.1.0** - Material Design color palettes
- **System.Windows.Forms** - For NotifyIcon (system tray)
- **NHotkey.Wpf 3.0.0** - Global hotkey registration (system-wide keyboard shortcuts)

---

## Project Structure

```
Radio_Player/
├── RadioPlayer.sln                    # Visual Studio solution file
├── README.md                          # User documentation (comprehensive)
├── LICENSE                            # Apache-2.0 license
├── CompileToEXE.bat                   # Build script for standalone EXE
├── CLAUDE.md                          # This file (AI assistant guide)
│
├── libs/                              # Native library dependencies
│   └── flac/
│       ├── libFLAC.dll               # FLAC decoder (x64, ~300-400KB)
│       └── README.md                 # Download instructions for libFLAC
│
└── src/RadioPlayer.WPF/              # Main WPF application project
    │
    ├── Models/                        # Domain models (POCOs)
    │   ├── RadioStation.cs           # Radio station entity
    │   ├── Favorite.cs               # Favorite station entity
    │   ├── ListeningHistory.cs       # Listening history entry
    │   ├── IcyMetadata.cs            # ICY/Shoutcast metadata
    │   ├── StreamProgress.cs         # Streaming progress info
    │   └── BackupData.cs             # Backup/export data structure
    │
    ├── ViewModels/                    # MVVM ViewModels
    │   └── MainViewModel.cs          # Main window ViewModel (single VM)
    │
    ├── Views/                         # XAML views and dialogs
    │   ├── MainWindow.xaml(.cs)      # Main application window
    │   ├── SettingsDialog.xaml(.cs)  # Settings configuration
    │   ├── StationDetailsDialog.xaml(.cs) # Station details viewer
    │   ├── HistoryDialog.xaml(.cs)   # Listening history viewer
    │   ├── StreamInfoDialog.xaml(.cs) # Stream debug info
    │   ├── AddCustomStationDialog.xaml(.cs) # Add custom station dialog
    │   └── AboutDialog.xaml(.cs)     # About dialog
    │
    ├── Services/                      # Business logic and data access
    │   ├── IRadioPlayer.cs           # Radio player interface
    │   ├── NAudioRadioPlayer.cs      # NAudio-based player implementation
    │   ├── IRadioRecorder.cs         # Radio recorder interface
    │   ├── NAudioRadioRecorder.cs    # Stream recorder implementation
    │   ├── IRadioBrowserService.cs   # Radio Browser API interface
    │   ├── RadioBrowserService.cs    # Radio Browser API client
    │   ├── IRadioStationRepository.cs # Repository interface
    │   ├── RadioStationRepository.cs # SQLite database operations (ADO.NET)
    │   ├── IExportImportService.cs   # Backup/export service interface
    │   ├── ExportImportService.cs    # Backup/export implementation
    │   ├── IGlobalHotkeyService.cs   # Global hotkey service interface
    │   ├── GlobalHotkeyService.cs    # Global hotkey implementation
    │   ├── ILanguageService.cs       # Language/translation service interface
    │   ├── LanguageService.cs        # Language/translation implementation
    │   ├── IStationCacheService.cs   # Station cache service interface
    │   ├── StationCacheService.cs    # Station cache implementation
    │   ├── IPlaylistParserService.cs # Playlist parser interface (PLS, M3U, M3U8)
    │   ├── PlaylistParserService.cs  # Playlist parser implementation
    │   ├── IStreamValidationService.cs # Stream validation interface
    │   ├── StreamValidationService.cs  # Stream validation implementation
    │   └── DTOs/                     # Data transfer objects
    │       ├── RadioStationDto.cs    # Station DTO from API
    │       ├── CountryDto.cs         # Country DTO
    │       ├── LanguageDto.cs        # Language DTO
    │       └── TagDto.cs             # Tag DTO
    │
    ├── Helpers/                       # Utility classes and parsers
    │   ├── AppConstants.cs           # Configuration constants (CRITICAL)
    │   ├── IcyMetadataParser.cs      # ICY metadata parser
    │   ├── IcyFilterStream.cs        # ICY metadata stream filter
    │   ├── RadioBrowserDnsHelper.cs  # DNS-based server discovery
    │   ├── OggStreamParser.cs        # OGG container parser
    │   ├── OggContentDetector.cs     # OGG codec detection
    │   ├── OpusStreamDecoder.cs      # Opus decoder wrapper
    │   ├── PeekableStream.cs         # Stream wrapper for peeking
    │   ├── DebugLogger.cs            # Optional debug logging
    │   └── TranslateExtension.cs     # XAML translation markup extension
    │
    ├── Converters/                    # XAML value converters
    │   └── ValueConverters.cs        # UI binding converters
    │
    ├── Resources/                     # Application resources
    │   ├── schema.sql                # SQLite database schema
    │   ├── app.ico                   # Application icon
    │   ├── README_ICON.md            # Icon documentation
    │   └── ICON_DOWNLOAD_INSTRUCTIONS.md
    │
    ├── lang/                          # Translation files for i18n
    │   ├── en.json                   # English translations
    │   ├── de.json                   # German translations
    │   └── pl.json                   # Polish translations
    │
    ├── App.xaml(.cs)                 # Application entry point and DI setup
    └── RadioPlayer.WPF.csproj        # Project file with dependencies
```

---

## Architecture Patterns

### MVVM (Model-View-ViewModel)

The application follows clean MVVM architecture:

#### Models (Data Layer)
- **POCOs** (Plain Old CLR Objects) with properties
- No business logic, just data containers
- Located in `Models/` directory
- Example: `RadioStation`, `Favorite`, `ListeningHistory`

#### ViewModels (Presentation Logic)
- Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `RelayCommand` and `AsyncRelayCommand` for commands
- Implement `INotifyPropertyChanged` via base class
- No direct UI references (no XAML, no controls)
- **Single ViewModel pattern**: Only `MainViewModel.cs` for main window
- Dialogs use code-behind for simplicity

#### Views (UI Layer)
- XAML files with code-behind
- Data binding to ViewModels
- Material Design theming via MaterialDesignThemes
- No business logic in code-behind

### Dependency Injection

**Setup in `App.xaml.cs`:**

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Singletons (shared instances)
    services.AddSingleton<HttpClient>();
    services.AddSingleton<IRadioBrowserService, RadioBrowserService>();
    services.AddSingleton<IRadioStationRepository, RadioStationRepository>();
    services.AddSingleton<IRadioPlayer, NAudioRadioPlayer>();
    services.AddSingleton<IRadioRecorder, NAudioRadioRecorder>();
    services.AddSingleton<IExportImportService, ExportImportService>();
    services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
    services.AddSingleton<ILanguageService, LanguageService>();
    services.AddSingleton<IStationCacheService, StationCacheService>();
    services.AddSingleton<IPlaylistParserService, PlaylistParserService>();
    services.AddSingleton<IStreamValidationService, StreamValidationService>();

    // Transients (new instance each time)
    services.AddTransient<MainViewModel>();
    services.AddTransient<MainWindow>();
}
```

**Key Principles:**
- Use interfaces for abstraction (`IRadioPlayer`, `IRadioBrowserService`, `IRadioStationRepository`)
- Constructor injection pattern
- Singletons for stateful services (player, repository)
- Transients for ViewModels and Views
- Services resolved via `IServiceProvider` in `App.OnStartup`

### Repository Pattern

**Interface:** `IRadioStationRepository`

```csharp
public interface IRadioStationRepository
{
    Task InitializeDatabaseAsync();
    Task<List<RadioStation>> GetTopStationsAsync(int limit);
    Task AddOrUpdateStationsAsync(IEnumerable<RadioStation> stations);
    Task<bool> AddFavoriteAsync(string stationUuid);
    Task<bool> RemoveFavoriteAsync(string stationUuid);
    Task<List<RadioStation>> GetFavoritesAsync();
    Task AddListeningHistoryAsync(string stationUuid, TimeSpan duration);
    // ... more methods
}
```

**Implementation:** `RadioStationRepository.cs`
- Uses **ADO.NET** directly (not EF Core) for optimal performance
- SQLite with WAL mode for concurrency
- Located at: `%LocalAppData%\RadioPlayer\radioplayer.db`
- Schema in `Resources/schema.sql`

### Service Pattern

**Radio Browser API Service:**
- Interface: `IRadioBrowserService`
- Implementation: `RadioBrowserService.cs`
- Uses Polly for retry logic (5 attempts, exponential backoff)
- DNS-based server discovery via `RadioBrowserDnsHelper`
- Free API, no authentication required

**Radio Player Service:**
- Interface: `IRadioPlayer`
- Implementation: `NAudioRadioPlayer.cs`
- Multi-format streaming support
- Event-driven architecture (PlaybackStateChanged, MetadataReceived, ProgressUpdated)
- Two-thread architecture: download thread + playback thread

---

## Code Conventions

### General C# Style

1. **Nullable Reference Types**: ENABLED
   - Use `?` for nullable types: `string?`, `RadioStation?`
   - Avoid null warnings with proper null checking

2. **File-Scoped Namespaces**: YES
   ```csharp
   namespace RadioPlayer.WPF.Services;  // File-scoped (preferred)
   ```

3. **XML Documentation**: REQUIRED for public APIs
   ```csharp
   /// <summary>
   /// Plays a radio station asynchronously
   /// </summary>
   /// <param name="station">The station to play</param>
   /// <param name="cancellationToken">Cancellation token</param>
   Task PlayAsync(RadioStation station, CancellationToken cancellationToken = default);
   ```

4. **Async/Await**: Proper async patterns
   - Use `async Task` (not `async void` except event handlers)
   - Avoid `.Result` or `.Wait()` - use `await`
   - Use `ConfigureAwait(false)` in library code (not needed in UI code)

5. **LINQ**: Prefer LINQ for collections
   ```csharp
   var stations = allStations
       .Where(s => s.Country == country)
       .OrderByDescending(s => s.Votes)
       .Take(100)
       .ToList();
   ```

### Naming Conventions

- **Classes**: PascalCase (`RadioStation`, `MainViewModel`)
- **Interfaces**: I-prefix (`IRadioPlayer`, `IRadioBrowserService`)
- **Methods**: PascalCase (`PlayAsync`, `GetStationsAsync`)
- **Properties**: PascalCase (`CurrentStation`, `IsPlaying`)
- **Private fields**: `_camelCase` with underscore (`_serviceProvider`, `_httpClient`)
- **Constants**: PascalCase (`BufferDuration`, `ChunkSize`)
- **Async methods**: Suffix with `Async` (`LoadStationsAsync`)

### Error Handling

1. **Try-Catch in UI Layer**: Show user-friendly messages
   ```csharp
   try
   {
       await _radioPlayer.PlayAsync(station);
   }
   catch (Exception ex)
   {
       MessageBox.Show($"Failed to play station: {ex.Message}",
                       "Playback Error",
                       MessageBoxButton.OK,
                       MessageBoxImage.Error);
   }
   ```

2. **Polly for Network Resilience**: Already configured in services
   - 5 retry attempts
   - Exponential backoff (2^n seconds)
   - 30-second timeout

3. **Event-Based Errors**: Use `ErrorOccurred` event in `IRadioPlayer`

### Resource Disposal

1. **IDisposable Pattern**: Implement properly
   ```csharp
   public class NAudioRadioPlayer : IRadioPlayer, IDisposable
   {
       private bool _disposed;

       public void Dispose()
       {
           if (_disposed) return;

           Stop();
           _waveOut?.Dispose();
           _httpClient?.Dispose();

           _disposed = true;
       }
   }
   ```

2. **Using Statements**: For temporary resources
   ```csharp
   using var connection = new SqliteConnection(connectionString);
   await connection.OpenAsync();
   // ... use connection
   ```

---

## Development Workflows

### Building the Project

#### Development Build
```bash
dotnet restore
dotnet build
dotnet run --project src/RadioPlayer.WPF
```

#### Release Build
```bash
dotnet build -c Release
```

#### Standalone EXE (Self-Contained)
```bash
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

**Output:** `src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe`

**File size:** ~90-100 MB (includes .NET runtime)

### Native Library Setup

**IMPORTANT:** For OGG/FLAC support, `libFLAC.dll` must be present:

1. Download FLAC 1.5.0: https://ftp.osuosl.org/pub/xiph/releases/flac/flac-1.5.0-win.zip
2. Extract and copy: `bin/win64/libFLAC.dll` → `libs/flac/libFLAC.dll`
3. Build project (auto-copies to output directory)

**Build Target:**
```xml
<Target Name="CopyNativeLibraries" AfterTargets="Build">
  <Copy SourceFiles="..\..\libs\flac\libFLAC.dll"
        DestinationFolder="$(OutDir)" />
</Target>
```

### Git Workflow

**No CI/CD currently configured** - manual builds and testing.

**Branching:**
- Main branch: `main` (or default branch)
- Feature branches: Use descriptive names (`feature/add-equalizer`, `fix/mp3-buffering`)

**Commits:**
- Use clear, descriptive messages
- Follow conventional commits if preferred: `feat:`, `fix:`, `refactor:`, `docs:`

---

## Key Components

### 1. Audio Streaming (`NAudioRadioPlayer.cs`)

**Architecture:**
- Two-thread design: Download thread + Playback thread
- BufferedWaveProvider for audio buffering
- Format-specific decoders (MP3, AAC, OGG/Vorbis, OGG/Opus, OGG/FLAC)

**Supported Formats:**
- **MP3**: `Mp3FileReader` (NAudio) - Frame-by-frame ACM decoding
- **AAC**: `MediaFoundationReader` (NAudio) - MediaFoundation
- **OGG/Vorbis**: `VorbisWaveReader` (NAudio.Vorbis) - IEEE float format
- **OGG/Opus**: `Concentus` + custom OGG parser - No seeking required
- **OGG/FLAC**: libFLAC P/Invoke - Streaming decoder, samples are right-justified

**Buffering Strategy:**
```csharp
// Default configuration (in AppConstants.cs)
BufferDuration = 10 seconds        // Total buffer
PreBufferDuration = 3 seconds      // Minimum before playback
ChunkSize = 32 KB                  // Read chunk size
BufferCheckInterval = 250ms        // Monitor frequency
```

**Critical Configuration:**
- MP3: 32 KB minimum data size before processing
- OGG: 64 KB minimum data size before processing
- AAC: 32 KB minimum data size before processing

**ICY Metadata:**
- Parsed via `IcyMetadataParser.cs`
- Filtered via `IcyFilterStream.cs`
- Provides "Now Playing" artist/title information

### 2. Database (`RadioStationRepository.cs`)

**Technology:** SQLite with ADO.NET (not EF Core)

**Schema:** See `Resources/schema.sql`

**Tables:**
- `RadioStations` - Station cache (StationUuid as unique identifier)
- `Favorites` - User favorites with sort order
- `ListeningHistory` - Playback history with duration tracking

**Configuration:**
- **Location:** `%LocalAppData%\RadioPlayer\radioplayer.db`
- **WAL mode:** Enabled for better concurrency
- **Connection pooling:** Enabled by default
- **Indexes:** Optimized for name, genre, country, UUID lookups

**Key Methods:**
```csharp
await repository.InitializeDatabaseAsync();
var stations = await repository.GetTopStationsAsync(100);
await repository.AddFavoriteAsync(stationUuid);
var favorites = await repository.GetFavoritesAsync();
await repository.AddListeningHistoryAsync(stationUuid, duration);
```

### 3. Radio Browser API (`RadioBrowserService.cs`)

**Base URL:** `https://de1.api.radio-browser.info`

**DNS Discovery:** `all.api.radio-browser.info` for server list

**Key Endpoints:**
- `GET /json/stations/topvote/{limit}` - Top voted stations
- `GET /json/stations/search` - Search with filters
- `GET /json/stations/byuuid/{uuid}` - Get station by UUID
- `POST /json/url/{uuid}` - Click tracking (required before playback)

**Search Filters:**
- `name` - Station name
- `country` - Country name
- `language` - Language
- `tag` - Genre/tags
- `codec` - Audio codec
- `bitrateMin` / `bitrateMax` - Bitrate range

**IMPORTANT:** Always use `StationUuid` (not `id`) for station identification

**Resilience:** Polly retry policy (5 attempts, exponential backoff)

### 4. Stream Recording (`NAudioRadioRecorder.cs`)

**NEW in v1.2** - Record radio streams to audio files.

**Supported Formats:**
- **WAV**: Uncompressed PCM audio (lossless)
- **MP3**: MPEG-1 Audio Layer 3 (compressed, requires LAME encoder)

**Architecture:**
- Captures audio from the player's BufferedWaveProvider
- Real-time recording with metadata tracking
- Automatic filename generation with station name and timestamp
- ICY metadata embedding in MP3 files (ID3 tags)

**Interface:** `IRadioRecorder`

```csharp
public interface IRadioRecorder : IDisposable
{
    bool IsRecording { get; }
    TimeSpan RecordingDuration { get; }
    string? CurrentRecordingPath { get; }

    event EventHandler<string>? RecordingStarted;
    event EventHandler<RecordingResult>? RecordingStopped;
    event EventHandler<Exception>? RecordingError;

    Task StartRecordingAsync(RadioStation station, string outputPath, RecordingFormat format);
    Task<RecordingResult> StopRecordingAsync();
    void UpdateMetadata(IcyMetadata metadata);
}
```

**Key Features:**
- User-selectable output location (SaveFileDialog)
- Real-time duration tracking
- Metadata tagging (Artist, Title, Album from ICY metadata)
- Error handling with user notifications
- Clean disposal and file finalization

**Integration:**
```csharp
// In DI container (App.xaml.cs)
services.AddSingleton<IRadioRecorder, NAudioRadioRecorder>();

// In ViewModel
await _radioRecorder.StartRecordingAsync(currentStation, filePath, RecordingFormat.Mp3);
// ... recording in progress ...
var result = await _radioRecorder.StopRecordingAsync();
```

### 5. Custom Stations (`AddCustomStationDialog`)

**NEW in v1.2** - Users can add their own radio stations.

**Features:**
- Manual station entry with custom metadata
- Support for all streaming formats (MP3, AAC, OGG, etc.)
- Custom logo/favicon URLs
- Full integration with favorites and history
- Backup/export support

**Required Fields:**
- Station name
- Stream URL (UrlResolved)
- Codec (MP3, AAC, OGG/Vorbis, etc.)

**Optional Fields:**
- Bitrate, Country, Language, Tags
- Homepage, Favicon
- Country code (ISO 3166-1 alpha-2)

**Storage:**
- Custom stations stored in `RadioStations` table with `IsCustom = true`
- Unique UUID generated for each custom station
- Can be added to favorites like regular stations

**Repository Methods:**
```csharp
Task<bool> AddCustomStationAsync(RadioStation station);
Task<List<RadioStation>> GetCustomStationsAsync();
Task<bool> DeleteCustomStationAsync(string stationUuid);
```

### 6. Backup & Export/Import (`ExportImportService`)

**NEW in v1.2** - Backup and restore settings, favorites, and custom stations.

**Interface:** `IExportImportService`

```csharp
public interface IExportImportService
{
    Task ExportToJsonAsync(string filePath, bool includeCustomStations = true);
    Task ImportFromJsonAsync(string filePath, bool mergeWithExisting = true);
    Task<BackupData> GetBackupDataAsync(bool includeCustomStations = true);
    Task<bool> ValidateBackupFileAsync(string filePath);
}
```

**Backup Format:** JSON (human-readable)

**Backup Contents:**
```csharp
public class BackupData
{
    string Version { get; set; }                      // Format version (1.0)
    DateTime ExportDate { get; set; }                 // UTC timestamp
    BackupSettings Settings { get; set; }             // App settings
    List<BackupFavorite> Favorites { get; set; }      // Favorite stations
    List<BackupStation> CustomStations { get; set; }  // User-added stations
}
```

**Settings Included:**
- Debug logging enabled/disabled
- Minimize to tray preference
- Buffer duration and pre-buffer duration
- Language preference
- Volume level

**Import Modes:**
- **Merge**: Combine with existing data (default)
- **Replace**: Replace all settings and favorites

**Validation:**
- JSON schema validation
- Version compatibility check
- Station UUID validation
- URL validation for custom stations

**Use Cases:**
- Backup before system migration
- Share favorites between computers
- Restore after reinstall
- Preserve custom station lists

### 7. Multi-Language Support (i18n)

**NEW in v1.2** - Interface supports multiple languages.

**Supported Languages:**
- **English** (en.json) - Default
- **German** (de.json) - Deutsch
- **Polish** (pl.json) - Polski

**Architecture:**
- JSON-based translation files in `lang/` directory
- Runtime language switching without restart
- All UI strings externalized
- Dialogs, menus, buttons, and messages translated

**Translation File Structure:**
```json
{
  "AppName": "Radio Player",
  "Version": "Version 1.2",
  "MainWindow": { "Title": "...", "..." },
  "Browse": { "TopStations": "...", "Favorites": "...", "..." },
  "Search": { "Placeholder": "...", "..." },
  "Player": { "Play": "...", "Stop": "...", "..." },
  "Recording": { "StartRecording": "...", "..." }
}
```

**Language Loading:**
- Language selection in Settings dialog
- Preference stored in database (Settings table)
- Loaded on application startup
- Fallback to English if translation missing

**Adding New Languages:**
1. Create new JSON file: `lang/xx.json` (ISO 639-1 code)
2. Copy structure from `en.json`
3. Translate all strings
4. Add language option to Settings dialog
5. Test all dialogs and UI elements

**Best Practices:**
- Use placeholders for dynamic content: `{0}`, `{1}`, etc.
- Keep translations concise for UI space constraints
- Test with longest language (often German) for layout
- Update all language files when adding new strings

### 8. Content Filtering (Russian Station Filter)

**NEW in v1.3** - Automatic filtering of Russian and Belarusian stations.

**Overview:**
The application automatically filters out radio stations from Russia and Belarus from all station lists. This filtering is applied transparently across all browsing, search, and cached station results.

**Detection Criteria:**
Stations are identified as Russian/Belarusian based on:
- **Country Code**: RU (Russia), BY (Belarus)
- **Country Name** (case-insensitive):
  - "Russia"
  - "Russian Federation"
  - "Россия" (Russia in Cyrillic)
  - "Belarus"
  - "Беларусь" (Belarus in Cyrillic)

**Implementation in `MainViewModel.cs`:**

```csharp
/// <summary>
/// Checks if a station is from Russia or Belarus
/// </summary>
private bool IsRussianStation(RadioStation station)
{
    // Check country code
    if (station.CountryCode?.Equals("RU", StringComparison.OrdinalIgnoreCase) == true ||
        station.CountryCode?.Equals("BY", StringComparison.OrdinalIgnoreCase) == true)
        return true;

    // Check country name
    var country = station.Country?.ToLowerInvariant();
    return country == "russia" ||
           country == "russian federation" ||
           country == "россия" ||
           country == "belarus" ||
           country == "беларусь";
}

/// <summary>
/// Filters Russian stations from a list
/// </summary>
private List<RadioStation> FilterOutRussianStations(List<RadioStation> stations)
{
    return stations.Where(s => !IsRussianStation(s)).ToList();
}
```

**Where Applied:**
The filter is automatically applied in:
- `LoadTopStationsAsync()` - Top voted stations
- `SearchStationsAsync()` - Search results
- `LoadCachedStationsAsync()` - Cached station browsing
- `LoadFavoritesAsync()` - **NOT filtered** - existing favorites are preserved

**Important Notes:**
- **Custom stations are NOT filtered** - User-added stations are always shown
- **Favorites are NOT filtered** - Previously saved favorites remain accessible
- **Transparent operation** - No UI indication of filtering
- **Performance** - Filtering happens client-side after API results
- **No configuration** - Filter is always active and not user-configurable

**Rationale:**
This feature was added to provide content curation and align with certain content policies. The filter is comprehensive and catches both ASCII and Cyrillic country names.

### 9. Playback Time Display

**NEW in v1.3** - Visual playback time tracking in the UI.

**Overview:**
The application now displays the current playback duration prominently in the station information area, making it easy to see how long you've been listening to the current station.

**Visual Format:**
```
[Clock Icon] HH:MM:SS • Status (Playing/Buffering)
```

**Implementation:**

**In `MainViewModel.cs`:**
```csharp
private string _playbackTime = "00:00:00";
public string PlaybackTime
{
    get => _playbackTime;
    set => SetProperty(ref _playbackTime, value);
}

// Timer updates every second
private void OnPlaybackTimerTick(object? sender, EventArgs e)
{
    if (_playbackStartTime.HasValue)
    {
        var elapsed = DateTime.Now - _playbackStartTime.Value;
        PlaybackTime = elapsed.ToString(@"hh\:mm\:ss");
    }
}
```

**In `MainWindow.xaml`:**
```xaml
<!-- Playback time display with clock icon -->
<StackPanel Orientation="Horizontal" Margin="0,5,0,0"
            Visibility="{Binding IsPlaying, Converter={StaticResource BooleanToVisibilityConverter}}">
    <materialDesign:PackIcon Kind="ClockOutline"
                            Width="16" Height="16"
                            VerticalAlignment="Center"
                            Margin="0,0,5,0" />
    <TextBlock Text="{Binding PlaybackTime}"
              FontWeight="SemiBold"
              VerticalAlignment="Center" />
    <TextBlock Text=" • "
              Margin="5,0"
              VerticalAlignment="Center" />
    <TextBlock Text="{Binding StatusMessage}"
              VerticalAlignment="Center" />
</StackPanel>
```

**Features:**
- **Real-time updates** - Updates every second via DispatcherTimer
- **Format** - HH:MM:SS (hours:minutes:seconds)
- **Visibility** - Only shown when station is playing
- **Position** - Displayed in station info area below song metadata
- **Icon** - Material Design clock icon for visual clarity
- **Combined status** - Shows playback time and status together

**User Benefits:**
- Track how long you've been listening
- See session duration at a glance
- Combined with status for comprehensive playback info
- No longer hidden in status message area

**Technical Details:**
- Uses `DispatcherTimer` with 1-second interval
- Starts when playback begins
- Resets when station changes
- Stops when playback stops
- Format ensures consistent two-digit display

### 10. Global Hotkeys

**NEW in v1.4** - System-wide keyboard shortcuts that work even when the application is minimized.

**Overview:**
The application now supports global hotkeys (also known as system-wide keyboard shortcuts) that allow users to control playback from anywhere in the system, even when the Radio Player window is minimized or not in focus.

**Supported Hotkeys:**
- **Ctrl+Shift+P** - Play/Pause toggle
- **Ctrl+Shift+S** - Stop playback
- **Ctrl+Shift+Right** - Next station
- **Ctrl+Shift+Left** - Previous station
- **Ctrl+Shift+Up** - Volume up (increase by 10%)
- **Ctrl+Shift+Down** - Volume down (decrease by 10%)
- **Ctrl+Shift+M** - Mute/Unmute toggle

**Implementation in `GlobalHotkeyService.cs`:**

```csharp
public interface IGlobalHotkeyService : IDisposable
{
    void RegisterHotkeys(
        Action onPlayPause,
        Action onStop,
        Action onNextStation,
        Action onPreviousStation,
        Action onVolumeUp,
        Action onVolumeDown,
        Action onMute);

    void UnregisterHotkeys();
    bool AreHotkeysRegistered { get; }
}
```

**Integration in `MainViewModel.cs`:**
- `TogglePlayPauseCommand` - Plays if stopped, stops if playing
- `NextStationCommand` - Navigates to next station in list and plays it
- `PreviousStationCommand` - Navigates to previous station in list and plays it
- `ToggleMuteCommand` - Toggles mute on/off, preserving volume level
- `VolumeUpCommand` - Increases volume by 10%, unmutes if muted
- `VolumeDownCommand` - Decreases volume by 10%, mutes if volume reaches 0

**Key Features:**
- **System-wide** - Works even when app is minimized to tray
- **Thread-safe** - All callbacks use Dispatcher.Invoke for UI thread safety
- **Error handling** - Gracefully handles hotkey registration failures
- **Auto-cleanup** - Hotkeys automatically unregistered when app closes
- **Non-blocking** - Registration failures don't prevent app startup

**Technical Details:**
- Uses **NHotkey.Wpf 3.0.0** library for hotkey registration
- Registered in `MainWindow.Loaded` event
- Cleaned up in `MainWindow.Closing` event
- Commands check `CanExecute` before execution
- All callbacks marshalled to UI thread via `Dispatcher.Invoke`

**Station Navigation:**
- Next/Previous commands cycle through the current station list
- Automatically wraps around (previous from first goes to last, next from last goes to first)
- Plays the selected station immediately
- Works with any loaded station list (Top Stations, Search Results, Favorites, Custom Stations)

**Volume Control:**
- Volume changes in 10% increments (0.0 to 1.0 scale)
- Volume up unmutes if currently muted
- Volume down to 0 automatically mutes
- Mute toggle preserves previous volume level

**Conflict Handling:**
- If hotkeys are already registered by another application, the app continues without errors
- Individual hotkey registration failures don't affect other hotkeys
- Debug logging reports registration status

**Use Cases:**
- Control playback while working in other applications
- Quickly switch stations without switching windows
- Adjust volume system-wide
- Pause playback instantly from anywhere

**Important Notes:**
- Hotkeys are NOT user-configurable in the current version (fixed shortcuts)
- Future enhancement could add settings dialog for custom hotkey assignment
- Windows only - uses Windows API via NHotkey.Wpf

### 11. Station Cache Service (`StationCacheService.cs`)

**NEW in v1.5** - Background station cache management.

**Overview:**
The Station Cache Service manages automatic updates of the local station database from the Radio Browser API. It ensures the station list stays fresh while minimizing unnecessary API calls.

**Interface:** `IStationCacheService`

```csharp
public interface IStationCacheService
{
    Task<int> UpdateCacheAsync(CancellationToken cancellationToken = default);
    Task<bool> ShouldUpdateCacheAsync();
    Task<TimeSpan?> GetCacheAgeAsync();
    Task<bool> UpdateCacheIfNeededAsync(CancellationToken cancellationToken = default);
    Task ClearCacheAsync();

    event EventHandler? CacheUpdateStarted;
    event EventHandler<int>? CacheUpdateCompleted;
    event EventHandler<Exception>? CacheUpdateFailed;
}
```

**Key Features:**
- Automatic cache age checking
- Configurable update intervals
- Event-driven updates for UI feedback
- Graceful error handling

### 12. Playlist Parser Service (`PlaylistParserService.cs`)

**NEW in v1.5** - Parse internet radio playlist files.

**Overview:**
The Playlist Parser Service handles parsing of common internet radio playlist formats, extracting stream URLs from playlist files.

**Supported Formats:**
- **PLS** - Shoutcast/Winamp playlist format
- **M3U** - Standard playlist format
- **M3U8** - Extended M3U (HLS) format

**Interface:** `IPlaylistParserService`

```csharp
public interface IPlaylistParserService
{
    Task<PlaylistParseResult> ParsePlaylistAsync(string playlistUrl, CancellationToken cancellationToken = default);
    bool IsPlaylistUrl(string url);
}
```

**Use Cases:**
- Auto-detect when station URL is a playlist
- Extract actual stream URL from playlist
- Support stations that only provide playlist links

### 13. Stream Validation Service (`StreamValidationService.cs`)

**NEW in v1.5** - Validate radio stream URLs before playback.

**Overview:**
The Stream Validation Service validates stream URLs and extracts metadata from ICY headers before playback starts.

**Interface:** `IStreamValidationService`

```csharp
public interface IStreamValidationService
{
    Task<StreamValidationResult> ValidateStreamAsync(string streamUrl, CancellationToken cancellationToken = default);
}

public class StreamValidationResult
{
    bool IsValid { get; set; }
    string? ErrorMessage { get; set; }
    string? ContentType { get; set; }
    string? Codec { get; set; }
    int? Bitrate { get; set; }
    string? StationName { get; set; }
    string? Genre { get; set; }
    string? ResolvedStreamUrl { get; set; }
}
```

**Key Features:**
- Pre-playback stream validation
- Codec detection from content type
- ICY metadata extraction (bitrate, name, genre)
- Playlist resolution (follows redirects to actual stream)

### 14. Column Sorting

**NEW in v1.5** - Sortable station list columns.

**Overview:**
Users can now sort the station list by clicking on column headers. The sort order toggles between ascending and descending with each click.

**Sortable Columns:**
- Station name
- Country
- Codec
- Bitrate
- Votes

**Implementation:**
- Click column header to sort ascending
- Click again to sort descending
- Visual indicator shows current sort column and direction

### 15. Station Context Menu

**NEW in v1.5** - Right-click context menu for stations.

**Overview:**
Right-clicking on a station in the list shows a context menu with quick actions.

**Menu Options:**
- **Play** - Start playing the station
- **Add to Favorites** / **Remove from Favorites** - Toggle favorite status
- **Copy to My Stations** - Copy station to custom stations list
- **View Details** - Open station details dialog

**Implementation:**
- Context menu bound to station list items
- Actions use same commands as main UI
- Menu items enabled/disabled based on context

### 16. Volume Persistence

**NEW in v1.5** - Volume level saved between sessions.

**Overview:**
The volume level is now persisted to the database and restored when the application starts.

**Technical Details:**
- Volume stored in Settings table
- Loaded during app initialization
- Saved when volume changes
- Range: 0-100 (stored as integer)

### 17. Favorite Button in Station List

**NEW in v1.5** - Quick favorite toggle per station.

**Overview:**
Each station in the list now has a favorite button (heart icon) allowing quick toggle of favorite status without opening a menu.

**Features:**
- Heart icon next to station name
- Filled heart = favorited, outline = not favorited
- Click to toggle favorite status
- Visual feedback on state change

### 18. Configuration (`AppConstants.cs`)

**Structure:**
```csharp
public static class AppConstants
{
    public static class AudioBuffer { /* buffering config */ }
    public static class Network { /* retry, timeout */ }
    public static class Database { /* DB path, settings */ }
    public static class UI { /* volume, pagination */ }
    public static class HttpHeaders { /* user agent */ }
    public static class Debug { /* logging */ }
}
```

**Settings are user-configurable** via `SettingsDialog.xaml`:
- Buffer duration (3-30 seconds)
- Pre-buffer duration (1-10 seconds)
- Debug logging (on/off)
- Minimize to tray (on/off)

**When changing constants:**
1. Update default values in `AppConstants.cs`
2. Update settings dialog UI if user-configurable
3. Update README.md documentation
4. Test thoroughly with different formats (MP3, OGG, AAC)

---

## Testing Guidelines

### Manual Testing (Current Approach)

**No automated tests currently exist.** Testing is done manually.

**Test Checklist:**

1. **Audio Formats**
   - [ ] MP3 streams play correctly
   - [ ] AAC streams play correctly
   - [ ] OGG/Vorbis streams play correctly
   - [ ] OGG/Opus streams play correctly
   - [ ] OGG/FLAC streams play correctly (requires libFLAC.dll)

2. **Buffering**
   - [ ] Pre-buffering works (3s minimum)
   - [ ] Buffer underrun recovery works
   - [ ] Adjusting buffer duration in settings works

3. **Station Management**
   - [ ] Search by name, country, language, genre works
   - [ ] Top stations load correctly (100 stations)
   - [ ] Add/remove favorites works
   - [ ] Favorites persist across restarts

4. **History**
   - [ ] Listening sessions tracked (≥5 seconds)
   - [ ] History displays correctly
   - [ ] Search history works
   - [ ] Clear history works

5. **UI**
   - [ ] Now Playing metadata updates
   - [ ] Station logos display
   - [ ] Volume control works
   - [ ] Mute/unmute works
   - [ ] System tray integration works

6. **Error Handling**
   - [ ] Offline stations show error
   - [ ] Network errors retry automatically
   - [ ] Database errors show user-friendly messages

7. **Recording (v1.2)**
   - [ ] Start recording to MP3 works
   - [ ] Start recording to WAV works
   - [ ] Recording duration updates in real-time
   - [ ] Recording stops cleanly
   - [ ] Metadata embedded in MP3 files (ID3 tags)
   - [ ] Cannot start second recording while one is active
   - [ ] Recording stops automatically when playback stops

8. **Custom Stations (v1.2)**
   - [ ] Add custom station dialog works
   - [ ] Required fields validated (name, URL, codec)
   - [ ] Custom station appears in list
   - [ ] Custom station plays correctly
   - [ ] Can add custom station to favorites
   - [ ] Can delete custom station
   - [ ] Custom stations included in backup/export

9. **Backup/Export/Import (v1.2)**
   - [ ] Export creates valid JSON file
   - [ ] Export includes favorites
   - [ ] Export includes custom stations
   - [ ] Export includes settings
   - [ ] Import validates backup file
   - [ ] Import merge mode works (preserves existing)
   - [ ] Import replace mode works (clears existing)
   - [ ] Import handles invalid files gracefully

10. **Multi-Language (v1.2)**
    - [ ] English UI displays correctly
    - [ ] German UI displays correctly
    - [ ] Polish UI displays correctly
    - [ ] Language switching works without restart
    - [ ] All dialogs translated
    - [ ] Settings persist language choice
    - [ ] Fallback to English for missing translations

11. **Global Hotkeys (v1.4)**
    - [ ] Ctrl+Shift+P toggles play/pause
    - [ ] Ctrl+Shift+S stops playback
    - [ ] Ctrl+Shift+Right plays next station
    - [ ] Ctrl+Shift+Left plays previous station
    - [ ] Ctrl+Shift+Up increases volume by 10%
    - [ ] Ctrl+Shift+Down decreases volume by 10%
    - [ ] Ctrl+Shift+M toggles mute
    - [ ] Hotkeys work when app is minimized
    - [ ] Hotkeys work when app is in system tray
    - [ ] Hotkeys work when other app has focus
    - [ ] Station navigation wraps around list
    - [ ] Volume changes respect min/max bounds (0-100%)
    - [ ] Mute preserves previous volume level

12. **Column Sorting (v1.5)**
    - [ ] Click column header sorts ascending
    - [ ] Click again sorts descending
    - [ ] Sort indicator shows on active column
    - [ ] Name column sorting works
    - [ ] Country column sorting works
    - [ ] Codec column sorting works
    - [ ] Bitrate column sorting works
    - [ ] Votes column sorting works
    - [ ] Sorting persists during search

13. **Station Context Menu (v1.5)**
    - [ ] Right-click shows context menu
    - [ ] Play option works
    - [ ] Add to Favorites option works
    - [ ] Remove from Favorites option works (when favorited)
    - [ ] Copy to My Stations option works
    - [ ] View Details option opens dialog
    - [ ] Menu items enable/disable appropriately

14. **Volume Persistence (v1.5)**
    - [ ] Volume level saved when changed
    - [ ] Volume restored on app restart
    - [ ] Volume persists across sessions
    - [ ] Works with mute toggle

15. **Favorite Button in List (v1.5)**
    - [ ] Heart icon shows next to station name
    - [ ] Filled heart for favorited stations
    - [ ] Outline heart for non-favorited stations
    - [ ] Click toggles favorite status
    - [ ] Icon updates after toggle
    - [ ] Works in all station views

16. **Search Context (v1.5)**
    - [ ] Search in Top Stations searches API
    - [ ] Search in Favorites searches local favorites
    - [ ] Search in Custom Stations searches custom stations
    - [ ] Clear search returns to full list

### Recommended Test Stations

**MP3:**
- Search: "BBC Radio 1" (UK, high quality)

**OGG/Vorbis:**
- Search: "SomaFM" (US, various channels)

**AAC:**
- Search: "NPR" (US, many AAC streams)

**OGG/FLAC:**
- Less common, search "FLAC" in codec filter

---

## Common Tasks

### Adding a New Feature

1. **Plan the change**
   - Which layer? (Model, ViewModel, View, Service)
   - Does it need database changes?
   - Does it need UI changes?

2. **Update Models** (if needed)
   - Add properties to domain models
   - Update database schema (`schema.sql`)
   - Update repository methods

3. **Update Services** (if needed)
   - Add interface methods
   - Implement in concrete classes
   - Add error handling

4. **Update ViewModels** (if needed)
   - Add properties with `ObservableProperty` attribute
   - Add commands with `RelayCommand` attribute
   - Update property change notifications

5. **Update Views** (if needed)
   - Update XAML with data bindings
   - Use Material Design components
   - Test responsive layout

6. **Test thoroughly**
   - Manual testing (see Testing Guidelines)
   - Test error cases
   - Test with different stations/formats

### Fixing Audio Streaming Issues

**Common Issues:**

1. **Buffering/Underruns**
   - Check `AppConstants.AudioBuffer.BufferDuration`
   - Verify format-specific minimum data sizes
   - Enable debug logging (`AppConstants.Debug.EnableLogging = true`)
   - Check `DebugLogger.cs` output in `%LocalAppData%\RadioPlayer\radioplayer-debug.log`

2. **Format Not Playing**
   - MP3: Check `Mp3FileReader` initialization
   - AAC: Verify MediaFoundation is available
   - OGG/Vorbis: Check NAudio.Vorbis package
   - OGG/Opus: Verify Concentus + OggStreamParser
   - OGG/FLAC: Ensure libFLAC.dll is present

3. **Metadata Not Showing**
   - Check ICY metadata header: `Icy-MetaInt`
   - Verify `IcyFilterStream` is correctly parsing
   - Enable debug logging to see raw metadata

### Modifying Database Schema

1. **Update `Resources/schema.sql`**
   - Add/modify table definitions
   - Add indexes for performance
   - Add foreign keys for referential integrity

2. **Update Repository**
   - Modify `InitializeDatabaseAsync()` to handle schema changes
   - Add migration logic if needed (currently no migrations)
   - Update CRUD methods

3. **Update Models**
   - Add/modify properties
   - Update ToString() if needed

4. **Test Database Operations**
   - Delete existing database: `%LocalAppData%\RadioPlayer\radioplayer.db`
   - Run app to recreate database
   - Test all CRUD operations

### Adding UI Components

**Material Design Guidelines:**

1. **Use MaterialDesignThemes components**
   ```xaml
   xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"

   <Button Style="{StaticResource MaterialDesignRaisedButton}" />
   <materialDesign:PackIcon Kind="Play" />
   ```

2. **Icons:** Use MaterialDesign icons via `PackIcon`
   - Browse: https://materialdesignicons.com/
   - Example: `<materialDesign:PackIcon Kind="Heart" />`

3. **Colors:** Use Material Design color palette
   - Primary: `PrimaryHueMidBrush`
   - Accent: `SecondaryHueMidBrush`

4. **Data Binding:**
   ```xaml
   <TextBlock Text="{Binding CurrentStation.Name}" />
   <Button Command="{Binding PlayCommand}" />
   ```

### Working with Recording

**Adding Recording to UI:**

1. **Check if recording is active**
   ```csharp
   bool canRecord = _radioPlayer.IsPlaying && !_radioRecorder.IsRecording;
   ```

2. **Start recording**
   ```csharp
   var saveDialog = new SaveFileDialog
   {
       Filter = "MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav",
       FileName = $"{station.Name}_{DateTime.Now:yyyyMMdd_HHmmss}"
   };

   if (saveDialog.ShowDialog() == true)
   {
       var format = saveDialog.FilterIndex == 1 ? RecordingFormat.Mp3 : RecordingFormat.Wav;
       await _radioRecorder.StartRecordingAsync(station, saveDialog.FileName, format);
   }
   ```

3. **Stop recording**
   ```csharp
   var result = await _radioRecorder.StopRecordingAsync();
   MessageBox.Show($"Recording saved: {result.FilePath}\nDuration: {result.Duration:hh\\:mm\\:ss}");
   ```

4. **Subscribe to events**
   ```csharp
   _radioRecorder.RecordingStarted += (s, path) =>
       StatusMessage = $"Recording to: {path}";
   _radioRecorder.RecordingStopped += (s, result) =>
       StatusMessage = $"Recording stopped ({result.Duration:hh\\:mm\\:ss})";
   _radioRecorder.RecordingError += (s, ex) =>
       MessageBox.Show($"Recording error: {ex.Message}");
   ```

**Important Notes:**
- MP3 recording requires LAME encoder (handled by NAudio)
- WAV files are larger but lossless
- Metadata updates automatically during recording
- Stop recording before switching stations or closing app

### Working with Custom Stations

**Adding a Custom Station:**

1. **Show AddCustomStationDialog**
   ```csharp
   var dialog = new AddCustomStationDialog();
   if (dialog.ShowDialog() == true)
   {
       var customStation = dialog.CustomStation;
       await _repository.AddCustomStationAsync(customStation);
       await LoadCustomStationsAsync();
   }
   ```

2. **Validate Custom Station Input**
   ```csharp
   if (string.IsNullOrWhiteSpace(Name) ||
       string.IsNullOrWhiteSpace(UrlResolved) ||
       string.IsNullOrWhiteSpace(Codec))
   {
       MessageBox.Show("Name, URL, and Codec are required");
       return false;
   }

   // Validate URL format
   if (!Uri.TryCreate(UrlResolved, UriKind.Absolute, out var uri))
   {
       MessageBox.Show("Invalid URL format");
       return false;
   }
   ```

3. **Generate UUID for Custom Station**
   ```csharp
   var customStation = new RadioStation
   {
       StationUuid = Guid.NewGuid().ToString(),
       Name = name,
       UrlResolved = url,
       Codec = codec,
       IsCustom = true,
       // ... other fields
   };
   ```

4. **Delete Custom Station**
   ```csharp
   if (MessageBox.Show($"Delete '{station.Name}'?",
                       "Confirm Delete",
                       MessageBoxButton.YesNo) == MessageBoxResult.Yes)
   {
       await _repository.DeleteCustomStationAsync(station.StationUuid);
       await LoadCustomStationsAsync();
   }
   ```

**Important Notes:**
- Custom stations are marked with `IsCustom = true`
- Can be added to favorites like regular stations
- Included in backup/export operations
- Test streaming URL before adding

### Working with Backup/Export/Import

**Exporting Data:**

1. **Export with SaveFileDialog**
   ```csharp
   var saveDialog = new SaveFileDialog
   {
       Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
       FileName = $"RadioPlayer_Backup_{DateTime.Now:yyyyMMdd}.json"
   };

   if (saveDialog.ShowDialog() == true)
   {
       await _exportImportService.ExportToJsonAsync(saveDialog.FileName,
                                                     includeCustomStations: true);
       MessageBox.Show("Backup created successfully!");
   }
   ```

2. **Preview Backup Data**
   ```csharp
   var backupData = await _exportImportService.GetBackupDataAsync(includeCustomStations: true);
   var summary = $"Favorites: {backupData.Favorites.Count}\n" +
                 $"Custom Stations: {backupData.CustomStations.Count}\n" +
                 $"Settings: Buffer {backupData.Settings.BufferDurationSeconds}s";
   MessageBox.Show(summary, "Backup Preview");
   ```

**Importing Data:**

1. **Import with OpenFileDialog**
   ```csharp
   var openDialog = new OpenFileDialog
   {
       Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
   };

   if (openDialog.ShowDialog() == true)
   {
       // Validate first
       if (!await _exportImportService.ValidateBackupFileAsync(openDialog.FileName))
       {
           MessageBox.Show("Invalid backup file!");
           return;
       }

       // Ask merge or replace
       var result = MessageBox.Show("Merge with existing data?",
                                    "Import Options",
                                    MessageBoxButton.YesNoCancel);
       if (result == MessageBoxResult.Cancel) return;

       bool mergeWithExisting = result == MessageBoxResult.Yes;
       await _exportImportService.ImportFromJsonAsync(openDialog.FileName, mergeWithExisting);

       MessageBox.Show("Import completed successfully!");
       await ReloadAllDataAsync();
   }
   ```

2. **Handle Import Errors**
   ```csharp
   try
   {
       await _exportImportService.ImportFromJsonAsync(filePath, merge);
   }
   catch (JsonException ex)
   {
       MessageBox.Show($"Invalid JSON format: {ex.Message}");
   }
   catch (Exception ex)
   {
       MessageBox.Show($"Import failed: {ex.Message}");
   }
   ```

**Important Notes:**
- Backup files are human-readable JSON
- Import validates file format and version
- Merge mode preserves existing favorites
- Replace mode clears existing data first
- Custom stations are optional in backup

### Adding New Translations

**Steps to Add a New Language:**

1. **Create Translation File**
   ```bash
   # Copy English template
   cp src/RadioPlayer.WPF/lang/en.json src/RadioPlayer.WPF/lang/xx.json
   ```

2. **Translate All Strings**
   - Open `xx.json` in text editor
   - Translate all string values (preserve keys)
   - Use appropriate placeholder syntax: `{0}`, `{1}`, etc.
   - Test special characters and encoding (UTF-8)

3. **Add to Settings Dialog**
   ```csharp
   // In SettingsDialog.xaml.cs or ViewModel
   var languages = new[]
   {
       new { Code = "en", Name = "English" },
       new { Code = "de", Name = "Deutsch" },
       new { Code = "pl", Name = "Polski" },
       new { Code = "xx", Name = "Your Language" }  // Add new language
   };
   ```

4. **Update .csproj (if needed)**
   - Translation files should auto-copy with existing glob pattern:
   ```xml
   <ItemGroup>
     <None Include="lang\*.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </None>
   </ItemGroup>
   ```

5. **Test Translation**
   - Run app and switch to new language in Settings
   - Check all dialogs and UI elements
   - Verify text fits in UI controls (adjust XAML if needed)
   - Test dynamic strings with placeholders

**Translation Best Practices:**
- Keep button text short (fits in small buttons)
- Use formal/informal tone consistently
- Preserve technical terms (MP3, AAC, UUID, etc.)
- Test RTL languages carefully (may need layout changes)
- Include context comments for translators

---

## Important Considerations

### Radio Browser API

**CRITICAL:**
1. **Always use `StationUuid`** for station identification (not `id`)
2. **Call click endpoint** before playback: `POST /json/url/{uuid}`
3. **Use `UrlResolved`** for streaming (not `url`)
4. **No API keys required** - completely free
5. **DNS-based server discovery** - use `RadioBrowserDnsHelper`

### Audio Streaming

**CRITICAL:**
1. **Two-thread architecture** - Don't block download thread
2. **Format detection** - Use `OggContentDetector` for OGG formats
3. **FLAC samples** - Right-justified (not left-justified)
4. **Buffer underrun recovery** - Auto-reconnect on buffer empty
5. **Dispose properly** - Stop playback before disposing

### Database

**CRITICAL:**
1. **Use StationUuid** as unique identifier (not database Id)
2. **WAL mode** - Better for concurrent access
3. **Foreign keys** - Cascade deletes for favorites/history
4. **DateTime format** - Use ISO 8601: `DateTime.UtcNow.ToString("o")`
5. **Connection disposal** - Use `using` statements

### Performance

**Optimization Tips:**
1. **Async/await** - Keep UI responsive
2. **Pagination** - Load stations in batches (50 per page)
3. **Indexes** - Database queries are optimized
4. **Buffering** - Balance between start delay and stability
5. **Dispose** - Clean up resources promptly

### Security

**Considerations:**
1. **No authentication** - Radio Browser API is open
2. **Local storage only** - No cloud sync
3. **No personal data** - Only preferences and history
4. **HTTPS** - All API calls use HTTPS
5. **SQL injection** - Use parameterized queries (already done)

### Cross-Platform

**Windows Only:**
- WPF is Windows-specific
- MediaFoundation (AAC) is Windows-specific
- System tray (NotifyIcon) is Windows-specific

**Potential for Cross-Platform:**
- Core logic (services, models) could be shared
- Would need Avalonia/MAUI for cross-platform UI
- Would need alternative to MediaFoundation for AAC

---

## Dependency Updates

### Safe to Update
- MaterialDesignThemes
- CommunityToolkit.Mvvm
- Polly
- Microsoft.Data.Sqlite

### Update with Caution
- **NAudio** - Test all formats after updating
- **NAudio.Vorbis** - May break OGG/Vorbis playback
- **Concentus** - May affect OGG/Opus decoding

### Platform-Specific
- **.NET version** - Currently net10.0-windows
  - Update requires testing all functionality
  - May affect package compatibility

---

## Additional Resources

### Documentation
- **README.md** - Comprehensive user documentation
- **libs/flac/README.md** - libFLAC setup instructions
- **Resources/schema.sql** - Database schema

### External APIs
- **Radio Browser API**: https://api.radio-browser.info/
- **Radio Browser Docs**: https://de1.api.radio-browser.info/

### Libraries
- **NAudio**: https://github.com/naudio/NAudio
- **MaterialDesignThemes**: https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit
- **CommunityToolkit.Mvvm**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/

---

## Summary for AI Assistants

### When Making Changes

1. **Understand the architecture** - MVVM, DI, Repository pattern
2. **Follow conventions** - Naming, async/await, error handling
3. **Test thoroughly** - Multiple formats, error cases, UI responsiveness
4. **Update documentation** - README.md, XML comments, this file
5. **Consider performance** - Async operations, buffering, database indexes

### Key Files to Review Before Changing

- **AppConstants.cs** - All configuration values
- **NAudioRadioPlayer.cs** - Audio streaming logic
- **NAudioRadioRecorder.cs** - Stream recording logic (v1.2)
- **RadioStationRepository.cs** - Database operations
- **RadioBrowserService.cs** - API integration
- **ExportImportService.cs** - Backup/export operations (v1.2)
- **GlobalHotkeyService.cs** - Global hotkey registration (v1.4)
- **LanguageService.cs** - Translation management
- **StationCacheService.cs** - Station cache management (v1.5)
- **PlaylistParserService.cs** - Playlist parsing (v1.5)
- **StreamValidationService.cs** - Stream URL validation (v1.5)
- **MainViewModel.cs** - UI logic and commands
- **MainWindow.xaml** - Main window UI layout
- **lang/*.json** - Translation files for multi-language support (v1.2)

### Common Pitfalls to Avoid

1. ❌ Blocking UI thread with `.Result` or `.Wait()`
2. ❌ Not disposing IDisposable resources
3. ❌ Using `id` instead of `StationUuid` for stations
4. ❌ Hardcoding values instead of using AppConstants
5. ❌ Forgetting to update both interface and implementation
6. ❌ Not testing with all audio formats (MP3, AAC, OGG)
7. ❌ Breaking MVVM pattern (ViewModels shouldn't reference Views)

### Best Practices

1. ✅ Use dependency injection
2. ✅ Write async methods properly
3. ✅ Add XML documentation to public APIs
4. ✅ Use Polly for network resilience
5. ✅ Handle errors gracefully with user-friendly messages
6. ✅ Keep ViewModels testable (no UI dependencies)
7. ✅ Use Material Design components for consistency
8. ✅ Test with real radio stations from different countries/formats

---

## Version History

### Version 1.5 (2025-12-13)
**New Features & Enhancements:**
- **Column sorting** - Sortable station list columns
  - Click column headers to sort ascending/descending
  - Supports Name, Country, Codec, Bitrate, Votes columns
  - Visual indicator shows current sort state
- **Station context menu** - Right-click actions for stations
  - Play, Add/Remove Favorite, Copy to My Stations, View Details
  - Context-sensitive menu items
- **Volume persistence** - Volume level saved between sessions
  - Stored in Settings table
  - Restored on application startup
- **Favorite button in station list** - Quick favorite toggle
  - Heart icon next to each station name
  - One-click toggle without menus
- **Search context awareness** - Search within current view
  - Searches within Top Stations, Favorites, or Custom Stations as appropriate

**New Services:**
- `IStationCacheService` / `StationCacheService.cs` - Station cache management
- `IPlaylistParserService` / `PlaylistParserService.cs` - PLS/M3U/M3U8 parsing
- `IStreamValidationService` / `StreamValidationService.cs` - Stream URL validation

**Modified Components:**
- `MainViewModel.cs` - Added sorting, context menu commands, volume persistence
- `MainWindow.xaml` - Added favorite buttons, context menu, sortable columns
- `RadioStationRepository.cs` - Volume persistence methods

**Statistics:**
- Lines of Code: ~10,000 (up from ~7,900)
- Total Files: 50 C# files, 8 XAML views (up from 46/7)
- New features: 5+ enhancements
- New services: 3 new service interfaces

### Version 1.4 (2025-12-02)
**New Features & Enhancements:**
- **Global hotkeys** - System-wide keyboard shortcuts for controlling playback
  - Ctrl+Shift+P: Play/Pause toggle
  - Ctrl+Shift+S: Stop playback
  - Ctrl+Shift+Right: Next station
  - Ctrl+Shift+Left: Previous station
  - Ctrl+Shift+Up: Volume up (10% increment)
  - Ctrl+Shift+Down: Volume down (10% decrement)
  - Ctrl+Shift+M: Mute/Unmute toggle
  - Works even when app is minimized or in system tray

**New Components:**
- `IGlobalHotkeyService.cs` - Global hotkey service interface
- `GlobalHotkeyService.cs` - Global hotkey service implementation using NHotkey.Wpf

**Modified Components:**
- `MainViewModel.cs` - Added TogglePlayPause, NextStation, PreviousStation, ToggleMute, VolumeUp, VolumeDown commands
- `MainWindow.xaml.cs` - Integrated global hotkey service, registered hotkeys on window load
- `App.xaml.cs` - Registered IGlobalHotkeyService in DI container
- `RadioPlayer.WPF.csproj` - Added NHotkey.Wpf 3.0.0 package

**Purpose:**
- Enhanced user experience with system-wide playback control
- Improved productivity - control radio without switching windows
- Better integration with system tray usage

### Version 1.3 (2025-12-01)
**New Features & Enhancements:**
- **Content filtering** - Automatic filtering of Russian and Belarusian stations
  - Detects stations by country code (RU, BY) and country name (including Cyrillic)
  - Applied to top stations, search results, and cached station browsing
  - Custom stations and existing favorites are not filtered
- **Playback time display** - Visual playback duration tracking
  - Displays current session duration with clock icon in HH:MM:SS format
  - Positioned prominently in station info area below song metadata
  - Updates in real-time every second during playback

**Modified Components:**
- `MainViewModel.cs` - Added station filtering logic and playback time tracking
- `MainWindow.xaml` - Enhanced UI with playback time display

**Purpose:**
- Content curation and policy compliance
- Improved user experience with visible playback duration

### Version 1.2 (2025-11-28)
**Major Features Added:**
- Stream recording to WAV/MP3 with ICY metadata tagging
- Custom station support - add your own radio stations
- Backup/Export and Import of settings, favorites, and custom stations
- Multi-language interface (English, German, Polish)
- Enhanced UI with translation support across all dialogs

**New Components:**
- `NAudioRadioRecorder.cs` - Stream recording service
- `ExportImportService.cs` - Backup/export service
- `AddCustomStationDialog.xaml` - Custom station entry
- `BackupData.cs` - Backup data model
- `lang/*.json` - Translation files

**Statistics:**
- Lines of Code: ~7,700 (up from ~5,500)
- Total Files: 44 C# files, 7 XAML views
- New features: 4 major features added
- Languages: 3 supported languages

### Version 1.0 (2025-11-26)
**Initial comprehensive documentation:**
- Project overview and architecture
- Technology stack and dependencies
- Development workflows
- Code conventions and patterns
- Testing guidelines

---

**Document Version:** 1.5
**Last Updated:** 2025-12-13
**Maintained By:** Radio Player Development Team

For questions or clarifications, refer to the README.md or examine the source code directly.
