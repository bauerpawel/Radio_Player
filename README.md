# Radio Player - Desktop Internet Radio Application

> **Status: ✅ COMPLETED & READY TO USE** - Full-featured desktop radio player with access to 51,000+ internet radio stations worldwide!

A modern desktop radio player built with .NET 10 and WPF, providing access to 51,000+ internet radio stations worldwide through the Radio Browser API.

![Radio Player](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## 🎵 Features

- ✅ **Browse 51,000+ radio stations** from around the world
- ✅ **Advanced search** by name, country, language, genre, codec, bitrate
- ✅ **MP3/AAC streaming** with NAudio library
- ✅ **ICY metadata parsing** - displays "Now Playing" (Artist - Title)
- ✅ **Smart buffering** - 5-second buffer with automatic underrun recovery
- ✅ **Auto-reconnect** - resilient streaming with Polly retry policy
- ✅ **Favorites management** - save and organize your favorite stations
- ✅ **Listening history** - track what you've listened to
- ✅ **Volume control** with mute support
- ✅ **SQLite database** for local storage
- ✅ **Clean MVVM architecture** with dependency injection

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2022 (17.10+) or JetBrains Rider
- Windows 10/11

### Running the Application

1. **Clone the repository:**
   ```bash
   git clone https://github.com/bauerpawel/Radio_Player.git
   cd Radio_Player
   ```

2. **Restore packages and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run --project src/RadioPlayer.WPF
   ```

4. **Or open in Visual Studio:**
   - Open `RadioPlayer.sln`
   - Press F5 to build and run

### First Time Usage

1. Application starts and initializes SQLite database
2. Automatically loads top 100 voted stations
3. Double-click a station or select and click "Play" to start streaming
4. Use search to find stations by name, genre, or country
5. Click ⭐ to add stations to favorites
6. Adjust volume with the slider

## 📋 Technology Stack

Based on comprehensive research for optimal performance and reliability:

- **Framework**: WPF (.NET 10) for Windows desktop
- **Audio Streaming**: NAudio 2.2.1 (MIT license)
- **Database**: Microsoft.Data.Sqlite 10.0.0
- **API**: Radio Browser API (free, community-driven)
- **Resilience**: Polly 8.5.0 for retry logic
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **HTTP**: System.Net.Http.Json 10.0.0

## 📁 Project Structure

```
Radio_Player/
├── RadioPlayer.sln                          # Visual Studio solution
├── README.md                                # This file
├── LICENSE                                  # MIT License
└── src/RadioPlayer.WPF/
    ├── Models/                              # Domain models (5 classes)
    │   ├── RadioStation.cs                 # Radio station model
    │   ├── IcyMetadata.cs                  # ICY/Shoutcast metadata
    │   ├── Favorite.cs                     # Favorite stations
    │   ├── ListeningHistory.cs             # Playback history
    │   └── StreamProgress.cs               # Streaming progress
    ├── ViewModels/                          # MVVM ViewModels
    │   └── MainViewModel.cs                # Main window ViewModel
    ├── Views/                               # XAML views
    │   ├── MainWindow.xaml                 # Main window UI
    │   └── MainWindow.xaml.cs              # Code-behind
    ├── Services/                            # Business logic
    │   ├── RadioBrowserService.cs          # Radio Browser API client
    │   ├── RadioStationRepository.cs       # SQLite database operations
    │   ├── NAudioRadioPlayer.cs            # Audio streaming player
    │   └── DTOs/                            # Data transfer objects
    ├── Converters/                          # Value converters for XAML
    │   └── ValueConverters.cs              # UI binding converters
    ├── Helpers/                             # Utility classes
    │   ├── AppConstants.cs                 # Configuration constants
    │   ├── RadioBrowserDnsHelper.cs        # DNS-based server discovery
    │   └── IcyMetadataParser.cs            # ICY metadata parser
    ├── Resources/                           # Application resources
    │   └── schema.sql                      # Database schema
    ├── App.xaml                             # Application resources
    ├── App.xaml.cs                          # Application entry point
    └── RadioPlayer.WPF.csproj              # Project file
```

## 🎯 Architecture Highlights

### Audio Streaming (NAudio)
- **Two-thread architecture**: Separate download and playback threads
- **Buffer configuration**: 5-second buffer duration, 2-second pre-buffer
- **Chunk size**: 16 KB per read operation
- **Resilience**: 5 retry attempts with exponential backoff (2^n seconds)
- **Timeout**: 30-second HTTP timeout
- **ICY metadata**: Automatic parsing of "Now Playing" information
- **Format support**: MP3 (native), AAC (via Media Foundation)

### Database (SQLite)
- **WAL mode**: Write-Ahead Logging for better concurrency
- **Connection pooling**: Enabled by default (automatic in v6.0+)
- **Location**: `%LocalAppData%/RadioPlayer/radioplayer.db`
- **ADO.NET**: Direct database access for optimal performance
- **Tables**: RadioStations, Favorites, ListeningHistory
- **Indexes**: Optimized for fast queries

### Radio Browser API Integration
- **Base URL**: Distributed servers (de1, fi1, nl1)
- **DNS Lookup**: `all.api.radio-browser.info` for automatic server discovery
- **No API keys**: Completely free and open
- **No rate limits**: Community-driven infrastructure
- **51,000+ stations**: Global coverage with metadata
- **Resilient HTTP**: Polly retry policy for network failures

### MVVM Pattern
- **ViewModels**: Clean separation of UI and business logic
- **Commands**: RelayCommand from CommunityToolkit.Mvvm
- **Data Binding**: Two-way binding for reactive UI
- **Events**: Event-driven architecture for player state changes
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## 🔧 Configuration

Key configuration values in `AppConstants.cs`:

```csharp
// Audio buffering
BufferDuration = 5 seconds           // Total buffer size
PreBufferDuration = 2 seconds        // Minimum before playback
ChunkSize = 16 KB                    // Read chunk size
BufferCheckInterval = 250ms          // Buffer monitoring frequency

// Network resilience
RetryAttempts = 5                    // Number of retries
HttpTimeout = 30 seconds             // Request timeout
ExponentialBackoffBase = 2           // 2^n backoff strategy

// Database
DatabasePath = %LocalAppData%/RadioPlayer/radioplayer.db
UseWalMode = true                    // Write-Ahead Logging
UseConnectionPooling = true          // Connection reuse
```

## 📖 Usage Guide

### Browsing Stations

1. **Top Stations**: Click "Top Stations" to load the most popular stations
2. **Search**: Enter station name, genre, or country in the search box and press Enter
3. **Favorites**: Click "Favorites" to view your saved stations

### Playing Radio

1. **Select a station** from the list
2. **Double-click** or click the **Play button**
3. Watch the status: Connecting → Buffering → Playing
4. **Now Playing** section shows current song metadata
5. Use **volume slider** to adjust playback volume
6. Click **Stop** to end playback

### Managing Favorites

1. Select a station
2. Click the **⭐ button** to add to favorites
3. Click "Favorites" to view all saved stations
4. Double-click to play from favorites

## 🎨 User Interface

- **Header**: Now Playing information with real-time metadata
- **Sidebar**: Navigation buttons (Top Stations, Favorites, Search)
- **Main Area**: Station list with sortable columns
- **Search Bar**: Real-time search with Enter key support
- **Player Controls**: Play, Stop, Favorite, Volume
- **Status Bar**: Connection status and buffering indicator

## 🛠️ Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/bauerpawel/Radio_Player.git
cd Radio_Player

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/RadioPlayer.WPF
```

### Project Statistics

- **Total Files**: 36 files
- **Lines of Code**: ~4,100 lines
- **Commits**: 5 major commits
- **NuGet Packages**: 6 packages
- **Architecture**: Clean MVVM with DI

### Running Tests

```bash
# Unit tests (when implemented)
dotnet test
```

## 🔒 Security & Privacy

- **No telemetry**: Application doesn't track user behavior
- **Local storage**: All data stored locally in SQLite
- **No authentication**: Radio Browser API is completely open
- **HTTPS**: Secure connections to Radio Browser API
- **No personal data**: Only station preferences stored

## 🐛 Troubleshooting

### Database Initialization Failed
- Check that `%LocalAppData%/RadioPlayer` directory is writable
- Close any other instances of the application
- Delete `radioplayer.db` and restart to recreate

### Cannot Connect to Stations
- Check internet connection
- Some stations may be offline (try others)
- Firewall may be blocking connections
- Check Windows audio output device settings

### No Sound
- Verify Windows volume mixer shows RadioPlayer
- Check application volume slider (not muted)
- Ensure correct audio output device is selected
- Try different stations (some may use unsupported codecs)

### Buffering Issues
- Slow internet connection may cause frequent buffering
- Try stations with lower bitrate
- Application auto-reconnects on network interruptions

## 📝 Future Enhancements (Optional)

- 🎨 Material Design or Fluent UI styling
- 🔊 Equalizer with presets
- 📻 AAC/OGG codec support expansion
- 🌐 Multi-language support
- ⚙️ Settings page (buffer size, startup behavior)
- 📊 Statistics dashboard
- 🎵 Recently played stations
- 📱 System tray integration
- ⌨️ Keyboard shortcuts
- 🎨 Custom themes

## 🤝 Contributing

Contributions are welcome! This project follows research-based best practices documented in the project documentation.

When contributing:
1. Follow the established MVVM architecture
2. Maintain separation of concerns
3. Use dependency injection
4. Add XML documentation to public APIs
5. Write async methods properly (avoid `.Result` or `.Wait()`)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Radio Browser API**: Free, community-driven radio station database
- **NAudio**: Excellent .NET audio library by Mark Heath
- **Polly**: Resilience and transient-fault-handling library
- **CommunityToolkit.Mvvm**: Modern MVVM framework

## 📞 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check the troubleshooting section above
- Review the comprehensive research documentation included in the project

---

**Built with ❤️ using .NET 10 and WPF**

*Ready to stream 51,000+ radio stations from around the world!* 🌍📻
