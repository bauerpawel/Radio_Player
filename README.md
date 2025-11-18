# Radio Player - Desktop Internet Radio Application

A modern desktop radio player built with .NET 10 and WPF, providing access to 51,000+ internet radio stations worldwide through the Radio Browser API.

## Technology Stack

Based on comprehensive research documented in the project documentation, this application uses:

- **Framework**: WPF (.NET 10) for Windows desktop
- **Audio Streaming**: NAudio 2.2.1 (MIT license) - supports MP3, AAC, OGG streams
- **Database**: Microsoft.Data.Sqlite 10.0.0 for local storage
- **API**: Radio Browser API (free, community-driven, no API keys required)
- **Resilience**: Polly 8.5.0 for retry logic with exponential backoff
- **MVVM**: CommunityToolkit.Mvvm 8.4.0 for clean architecture

## Project Structure

```
Radio_Player/
├── RadioPlayer.sln                   # Visual Studio solution
├── src/
│   └── RadioPlayer.WPF/              # Main WPF application
│       ├── Models/                   # Data models
│       │   ├── RadioStation.cs       # Station model from Radio Browser API
│       │   ├── IcyMetadata.cs        # ICY/Shoutcast metadata
│       │   ├── Favorite.cs           # Favorite stations
│       │   ├── ListeningHistory.cs   # Playback history
│       │   └── StreamProgress.cs     # Streaming progress info
│       ├── ViewModels/               # MVVM ViewModels
│       │   └── MainViewModel.cs      # Main window ViewModel
│       ├── Views/                    # XAML views
│       │   └── MainWindow.xaml       # Main application window
│       ├── Services/                 # Business logic services
│       │   ├── IRadioBrowserService.cs      # Radio Browser API client
│       │   ├── IRadioStationRepository.cs   # SQLite database operations
│       │   └── IRadioPlayer.cs              # Audio streaming player
│       ├── Helpers/                  # Utility classes
│       │   └── AppConstants.cs       # Application constants
│       └── Resources/                # Images, icons, etc.
└── LICENSE
```

## Key Features (Planned)

- ✅ Browse 51,000+ radio stations worldwide
- ✅ Search by name, country, language, genre, codec
- ✅ Favorite stations management
- ✅ Listening history tracking
- ✅ ICY/Shoutcast metadata display (now playing info)
- ✅ Resilient streaming with auto-reconnect
- ✅ Smart buffering (5-second buffer, 2-second pre-buffer)
- ✅ SQLite local database for favorites and history
- ✅ Clean MVVM architecture

## Architecture Highlights

### Audio Streaming
- **Two-thread architecture**: Separate download and playback threads
- **Buffer configuration**: 5-second buffer duration, 2-second pre-buffer
- **Chunk size**: 16 KB per read operation
- **Resilience**: 5 retry attempts with exponential backoff (2^n seconds)
- **Timeout**: 30-second HTTP timeout

### Database
- **WAL mode**: Write-Ahead Logging for better concurrency
- **Connection pooling**: Enabled by default (v6.0+)
- **Location**: `%LocalAppData%/RadioPlayer/radioplayer.db`
- **Hybrid approach**: EF Core for complex queries, ADO.NET for high-frequency logging

### Radio Browser API
- **Base URL**: Distributed servers (de1, fi1, nl1)
- **DNS Lookup**: `all.api.radio-browser.info` for server discovery
- **No API keys**: Completely free and open
- **No rate limits**: Community-driven infrastructure

## Getting Started

### Prerequisites

- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2022 (17.10+) or JetBrains Rider
- Windows 10/11

### Building the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/bauerpawel/Radio_Player.git
   cd Radio_Player
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run --project src/RadioPlayer.WPF
   ```

### Opening in Visual Studio

1. Open `RadioPlayer.sln` in Visual Studio 2022
2. Set `RadioPlayer.WPF` as the startup project
3. Press F5 to build and run

## Current Implementation Status

This is the **initial project structure** with:

✅ Complete MVVM folder structure
✅ All model classes defined
✅ Service interfaces defined
✅ Basic UI layout (MainWindow)
✅ ViewModel with MVVM Toolkit
✅ NuGet package references configured
✅ Application constants and configuration

🚧 **Next Steps** (implementations needed):
- RadioBrowserService implementation
- RadioStationRepository implementation with SQLite
- NAudioRadioPlayer implementation
- IcyMetadataParser implementation
- Dependency injection setup in App.xaml.cs
- UI refinements and styling

## Configuration

Key configuration values in `AppConstants.cs`:

```csharp
// Audio buffering
BufferDuration = 5 seconds
PreBufferDuration = 2 seconds
ChunkSize = 16 KB
BufferCheckInterval = 250ms

// Network resilience
RetryAttempts = 5
HttpTimeout = 30 seconds
ExponentialBackoffBase = 2

// Database
DatabasePath = %LocalAppData%/RadioPlayer/radioplayer.db
UseWalMode = true
UseConnectionPooling = true
```

## Development Guidelines

### MVVM Pattern
- **Models**: Data structures only, no business logic
- **Views**: XAML UI, minimal code-behind
- **ViewModels**: Business logic, data binding, commands

### Service Implementations
1. Always use `async/await` for I/O operations
2. Implement `CancellationToken` support for long operations
3. Use Polly for resilient HTTP calls
4. Log errors appropriately

### Database Operations
- Use short-lived connections (connection pooling handles reuse)
- Enable WAL mode for concurrency
- Prefer ADO.NET for simple CRUD, EF Core for complex queries
- Store database in LocalApplicationData folder

## Contributing

This project follows research-based best practices documented in the project documentation. When contributing:

1. Follow the established MVVM architecture
2. Maintain separation of concerns
3. Use dependency injection via `ServiceCollection`
4. Add XML documentation to public APIs
5. Write async methods properly (avoid `.Result` or `.Wait()`)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **Radio Browser API**: Free, community-driven radio station database
- **NAudio**: Excellent .NET audio library by Mark Heath
- **Polly**: Resilience and transient-fault-handling library

## Research Documentation

Detailed technical research and architectural decisions are documented in the project research files covering:
- Radio Browser API capabilities and integration
- Audio streaming libraries comparison (NAudio, Bass.Net, LibVLCSharp)
- SQLite in .NET 10 best practices
- WPF vs WinForms vs .NET MAUI analysis
- Production-ready media player patterns

---

**Status**: 🚧 Initial structure complete, implementation in progress
**Target Platform**: Windows 10/11
**.NET Version**: 10.0
