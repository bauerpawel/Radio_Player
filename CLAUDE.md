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
- Station browsing, search, and favorites management
- ICY metadata parsing for "Now Playing" information
- Listening history tracking
- Material Design UI with system tray support

### Key Statistics
- **Lines of Code**: ~5,500 lines
- **Files**: 50+ files
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
    │   └── StreamProgress.cs         # Streaming progress info
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
    │   └── AboutDialog.xaml(.cs)     # About dialog
    │
    ├── Services/                      # Business logic and data access
    │   ├── IRadioPlayer.cs           # Radio player interface
    │   ├── NAudioRadioPlayer.cs      # NAudio-based player implementation
    │   ├── IRadioBrowserService.cs   # Radio Browser API interface
    │   ├── RadioBrowserService.cs    # Radio Browser API client
    │   ├── IRadioStationRepository.cs # Repository interface
    │   ├── RadioStationRepository.cs # SQLite database operations (ADO.NET)
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
    │   └── DebugLogger.cs            # Optional debug logging
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

### 4. Configuration (`AppConstants.cs`)

**CRITICAL FILE** - All configuration constants are defined here.

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
- **RadioStationRepository.cs** - Database operations
- **RadioBrowserService.cs** - API integration
- **MainViewModel.cs** - UI logic and commands

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

**Document Version:** 1.0
**Last Updated:** 2025-11-26
**Maintained By:** Radio Player Development Team

For questions or clarifications, refer to the README.md or examine the source code directly.
