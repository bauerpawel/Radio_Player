# Radio Player - Desktop Internet Radio Application

> **Status: ✅ COMPLETED & READY TO USE** - Full-featured desktop radio player with access to 51,000+ internet radio stations worldwide!

[🇬🇧 English](#english) | [🇵🇱 Polski](#polski)

---

## <a name="english"></a>🇬🇧 English

A modern desktop radio player built with .NET 10 and WPF, providing access to 51,000+ internet radio stations worldwide through the Radio Browser API.

![Radio Player](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-Apache--2.0-blue)

## 🎵 Features

### Core Features
- ✅ **Browse 51,000+ radio stations** from around the world
- ✅ **Advanced search** by name, country, language, genre, codec, bitrate
- ✅ **Multi-format streaming** - MP3, AAC, OGG Vorbis support with NAudio
- ✅ **ICY metadata parsing** - displays "Now Playing" (Artist - Title)
- ✅ **Smart buffering** - configurable buffer with automatic underrun recovery
- ✅ **Auto-reconnect** - resilient streaming with Polly retry policy
- ✅ **Material Design UI** - beautiful, modern interface with MaterialDesignThemes

### Station Management
- ✅ **Favorites management** - save and organize your favorite stations
- ✅ **Station details** - view comprehensive station information, similar stations
- ✅ **Station logos** - display station favicon/logo in player controls
- ✅ **Listening history** - track what you've listened to with duration tracking
- ✅ **Search and filter** - case-insensitive search across all fields

### Player Controls
- ✅ **Volume control** with mute support
- ✅ **Play/Stop controls** with status indicators
- ✅ **Buffering visualization** - see real-time buffer status
- ✅ **Now Playing display** - shows current track with station logo

### Additional Features
- ✅ **System tray integration** - minimize to tray (optional in settings)
- ✅ **Debug logging** - optional logging for troubleshooting buffering issues
- ✅ **Configurable buffers** - adjust buffer sizes in settings
- ✅ **About dialog** - version info and repository link
- ✅ **SQLite database** for local storage
- ✅ **Clean MVVM architecture** with dependency injection

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2022 (17.10+) or JetBrains Rider (optional)
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
7. View station details with the Details button
8. Check your listening history with the History button

## 📦 Building Standalone EXE

### Method 1: Self-Contained Deployment (Recommended)

Creates a standalone executable with all dependencies included (no .NET runtime required):

```bash
# Navigate to project directory
cd Radio_Player

# Build for Windows x64 (single-file EXE with .NET runtime included)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true

# Output will be in:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**File size:** ~90-100 MB (includes full .NET runtime)

### Method 2: Framework-Dependent Deployment

Creates a smaller executable (requires .NET 10 runtime installed on target system):

```bash
# Build for Windows x64 (requires .NET 10 installed)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true

# Output will be in:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**File size:** ~2-5 MB (requires .NET 10 runtime on target PC)

### Method 3: Trimmed Build (Smallest Size)

Creates the smallest possible executable with unused code removed:

```bash
# Build with trimming (advanced, may require testing)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:TrimMode=link

# Output will be in:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**File size:** ~50-60 MB (fully self-contained, trimmed)

### Distribution

After building, the `RadioPlayer.exe` file can be distributed to other Windows computers. For self-contained builds, just copy the entire `publish` folder contents.

**Note:** The application creates its database in `%LocalAppData%\RadioPlayer\` on first run.

## 📋 Technology Stack

Based on comprehensive research for optimal performance and reliability:

- **Framework**: WPF (.NET 10) for Windows desktop
- **UI Components**: MaterialDesignThemes 5.1.0 (Material Design styling)
- **Audio Streaming**: NAudio 2.2.1, NAudio.Vorbis 1.5.0 (MIT license)
- **Database**: Microsoft.Data.Sqlite 10.0.0
- **API**: Radio Browser API (free, community-driven)
- **Resilience**: Polly 8.5.0 for retry logic
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection 10.0.0
- **System Tray**: System.Windows.Forms (NotifyIcon)

## 📁 Project Structure

```
Radio_Player/
├── RadioPlayer.sln                          # Visual Studio solution
├── README.md                                # This file
├── LICENSE                                  # Apache-2.0 License
└── src/RadioPlayer.WPF/
    ├── Models/                              # Domain models
    │   ├── RadioStation.cs                 # Radio station model
    │   ├── IcyMetadata.cs                  # ICY/Shoutcast metadata
    │   ├── Favorite.cs                     # Favorite stations
    │   ├── ListeningHistory.cs             # Playback history
    │   └── StreamProgress.cs               # Streaming progress
    ├── ViewModels/                          # MVVM ViewModels
    │   └── MainViewModel.cs                # Main window ViewModel
    ├── Views/                               # XAML views
    │   ├── MainWindow.xaml                 # Main window UI
    │   ├── MainWindow.xaml.cs              # Code-behind
    │   ├── SettingsDialog.xaml             # Settings dialog
    │   ├── StreamInfoDialog.xaml           # Stream info viewer
    │   ├── StationDetailsDialog.xaml       # Station details viewer
    │   ├── HistoryDialog.xaml              # Listening history viewer
    │   └── AboutDialog.xaml                # About dialog
    ├── Services/                            # Business logic
    │   ├── IRadioBrowserService.cs         # Radio Browser API interface
    │   ├── RadioBrowserService.cs          # Radio Browser API client
    │   ├── IRadioStationRepository.cs      # Repository interface
    │   ├── RadioStationRepository.cs       # SQLite database operations
    │   ├── IRadioPlayer.cs                 # Radio player interface
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
- **Multi-format support**: MP3 (StreamMediaFoundationReader), AAC (MediaFoundation), OGG Vorbis (VorbisWaveReader)
- **Two-thread architecture**: Separate download and playback threads
- **Configurable buffering**: Adjustable buffer duration (3-30s), pre-buffer duration (1-10s)
- **Smart accumulation**: Format-specific data accumulation (32KB MP3, 64KB OGG, 32KB AAC)
- **Chunk size**: 16 KB per read operation
- **Resilience**: 5 retry attempts with exponential backoff (2^n seconds)
- **Timeout**: 30-second HTTP timeout
- **ICY metadata**: Automatic parsing of "Now Playing" information

### Database (SQLite)
- **WAL mode**: Write-Ahead Logging for better concurrency
- **Connection pooling**: Enabled by default
- **Location**: `%LocalAppData%/RadioPlayer/radioplayer.db`
- **ADO.NET**: Direct database access for optimal performance
- **Tables**: RadioStations, Favorites, ListeningHistory
- **Indexes**: Optimized for fast queries
- **History tracking**: Automatic listening session tracking (≥5 seconds)

### Radio Browser API Integration
- **Base URL**: Distributed servers (de1, fi1, nl1)
- **DNS Lookup**: `all.api.radio-browser.info` for automatic server discovery
- **No API keys**: Completely free and open
- **No rate limits**: Community-driven infrastructure
- **51,000+ stations**: Global coverage with metadata
- **Resilient HTTP**: Polly retry policy for network failures
- **Similar stations**: Tag-based station discovery

### MVVM Pattern
- **ViewModels**: Clean separation of UI and business logic
- **Commands**: RelayCommand from CommunityToolkit.Mvvm
- **Data Binding**: Two-way binding for reactive UI
- **Events**: Event-driven architecture for player state changes
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Interfaces**: Abstraction for testability and maintainability

## 🔧 Configuration

Key configuration values in `AppConstants.cs` (adjustable via Settings dialog):

```csharp
// Audio buffering (configurable in Settings)
BufferDuration = 10 seconds           // Total buffer size (3-30s)
PreBufferDuration = 3 seconds         // Minimum before playback (1-10s)
ChunkSize = 16 KB                     // Read chunk size
BufferCheckInterval = 250ms           // Buffer monitoring frequency

// Format-specific accumulation
MinimumMp3DataSize = 32 KB            // MP3 frame processing threshold
MinimumOggDataSize = 64 KB            // OGG page processing threshold
MinimumAacDataSize = 32 KB            // AAC frame processing threshold

// Network resilience
RetryAttempts = 5                     // Number of retries
HttpTimeout = 30 seconds              // Request timeout
ExponentialBackoffBase = 2            // 2^n backoff strategy

// Database
DatabasePath = %LocalAppData%/RadioPlayer/radioplayer.db
UseWalMode = true                     // Write-Ahead Logging
UseConnectionPooling = true           // Connection reuse

// UI
DefaultVolume = 0.8                   // 80% volume
StationsPerPage = 50                  // Pagination size
MaxRecentHistoryItems = 50            // History limit in main view
MinimizeToTray = false                // System tray behavior (configurable)

// Debug
EnableLogging = false                 // Debug logging (configurable)
```

## 📖 Usage Guide

### Browsing Stations

1. **Top Stations**: Click "Top Stations" to load the most popular stations
2. **Search**: Enter station name, genre, or country in filters and click "Apply Filters"
3. **Favorites**: Click "Favorites" to view your saved stations
4. **History**: Click "History" to view your listening history

### Playing Radio

1. **Select a station** from the list
2. **Double-click** or click the **Play button**
3. Watch the status: Connecting → Buffering → Playing
4. **Now Playing** section shows current song metadata with station logo
5. Use **volume slider** to adjust playback volume
6. Click **Stop** to end playback

### Managing Favorites

1. Select a station
2. Click the **⭐ button** to add to favorites (or remove if already favorited)
3. Click "Favorites" to view all saved stations
4. Double-click to play from favorites

### Station Details

1. Click the **Details** button on any station
2. View comprehensive information: country, language, tags, homepage
3. See technical details: codec, bitrate, stream URL
4. Browse similar stations based on matching tags
5. Click "Play Station" to start playback directly from details

### Listening History

1. Click **History** button in sidebar
2. View all listening sessions with date, time, and duration
3. Search history by station name
4. Click "Play Again" to replay a station
5. See statistics (total records and listening time)
6. Clear all history if needed

### Settings

1. Click the **Settings** icon (⚙️) in top-right
2. Adjust **Buffer Duration** (3-30s) for smoother playback
3. Adjust **Pre-Buffer Duration** (1-10s) for faster start
4. Enable **Debug Logging** to troubleshoot buffering issues
5. Enable **Minimize to Tray** to minimize to system tray instead of taskbar

### System Tray

1. Enable "Minimize to system tray" in Settings
2. Minimize window or click X to send to tray
3. Double-click tray icon to restore window
4. Right-click tray icon for context menu (Restore, Exit)

## 🎨 User Interface

- **Header**: Now Playing information with station logo and real-time metadata
- **Toolbar**: About, Settings, and Stream Info buttons
- **Sidebar**: Navigation buttons (Top Stations, Favorites, Search, History)
- **Main Area**: Station list with sortable columns
- **Filter Panel**: Advanced search filters with Apply Filters button
- **Player Controls**: Play, Stop, Favorite, Volume, Buffering indicator
- **Status Bar**: Connection status and stream information

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

- **Total Files**: 50+ files
- **Lines of Code**: ~5,500 lines
- **NuGet Packages**: 10 packages
- **Architecture**: Clean MVVM with DI
- **Version**: 1.0

## 🔒 Security & Privacy

- **No telemetry**: Application doesn't track user behavior
- **Local storage**: All data stored locally in SQLite
- **No authentication**: Radio Browser API is completely open
- **HTTPS**: Secure connections to Radio Browser API
- **No personal data**: Only station preferences and listening history stored locally

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
- Increase buffer duration in Settings (try 15-20s)
- Slow internet connection may cause frequent buffering
- Try stations with lower bitrate
- Enable debug logging in Settings to diagnose issues
- Application auto-reconnects on network interruptions

### MP3 Stations Rebuffering
- MP3 buffer threshold reduced to 32KB for optimal performance
- If issues persist, try increasing buffer duration in Settings

## 🤝 Contributing

Contributions are welcome! This project follows research-based best practices documented in the project documentation.

When contributing:
1. Follow the established MVVM architecture
2. Maintain separation of concerns
3. Use dependency injection
4. Add XML documentation to public APIs
5. Write async methods properly (avoid `.Result` or `.Wait()`)
6. Use Material Design icons and styles for consistency

## 📄 License

This project is licensed under the Apache-2.0 License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Radio Browser API**: Free, community-driven radio station database
- **NAudio**: Excellent .NET audio library by Mark Heath
- **MaterialDesignThemes**: Beautiful Material Design components for WPF
- **Polly**: Resilience and transient-fault-handling library
- **CommunityToolkit.Mvvm**: Modern MVVM framework

## 📞 Support

For issues, questions, or suggestions:
- Open an issue on [GitHub](https://github.com/bauerpawel/Radio_Player/issues)
- Check the troubleshooting section above
- Enable debug logging in Settings to diagnose buffering issues

---

## <a name="polski"></a>🇵🇱 Polski

Nowoczesny odtwarzacz radia internetowego zbudowany w .NET 10 i WPF, zapewniający dostęp do ponad 51,000 stacji radiowych z całego świata poprzez Radio Browser API.

## 🎵 Funkcje

### Podstawowe Funkcje
- ✅ **Przeglądaj ponad 51,000 stacji radiowych** z całego świata
- ✅ **Zaawansowane wyszukiwanie** według nazwy, kraju, języka, gatunku, kodeka, bitrate
- ✅ **Strumieniowanie wielu formatów** - wsparcie dla MP3, AAC, OGG Vorbis z NAudio
- ✅ **Parsowanie metadanych ICY** - wyświetla "Teraz odtwarzane" (Artysta - Tytuł)
- ✅ **Inteligentne buforowanie** - konfigurowalne bufory z automatycznym odzyskiwaniem
- ✅ **Automatyczne ponowne łączenie** - odporne strumieniowanie z polityką ponawiania Polly
- ✅ **Interfejs Material Design** - piękny, nowoczesny interfejs z MaterialDesignThemes

### Zarządzanie Stacjami
- ✅ **Zarządzanie ulubionymi** - zapisuj i organizuj ulubione stacje
- ✅ **Szczegóły stacji** - wyświetlaj szczegółowe informacje o stacji, podobne stacje
- ✅ **Logo stacji** - wyświetlanie favicon/logo stacji w kontrolkach odtwarzacza
- ✅ **Historia odtwarzania** - śledź co słuchałeś z czasem trwania
- ✅ **Wyszukiwanie i filtrowanie** - wyszukiwanie bez rozróżniania wielkości liter

### Kontrolki Odtwarzacza
- ✅ **Kontrola głośności** z obsługą wyciszenia
- ✅ **Kontrolki Play/Stop** ze wskaźnikami statusu
- ✅ **Wizualizacja buforowania** - zobacz status bufora w czasie rzeczywistym
- ✅ **Wyświetlanie "Teraz odtwarzane"** - pokazuje bieżący utwór z logo stacji

### Dodatkowe Funkcje
- ✅ **Integracja z zasobnikiem systemowym** - minimalizacja do zasobnika (opcjonalnie w ustawieniach)
- ✅ **Logowanie debugowania** - opcjonalne logowanie do rozwiązywania problemów z buforowaniem
- ✅ **Konfigurowalne bufory** - dostosuj rozmiary buforów w ustawieniach
- ✅ **Okno "O programie"** - informacje o wersji i link do repozytorium
- ✅ **Baza danych SQLite** do lokalnego przechowywania
- ✅ **Czysta architektura MVVM** z wstrzykiwaniem zależności

## 🚀 Szybki Start

### Wymagania

- .NET 10 SDK ([Pobierz](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2022 (17.10+) lub JetBrains Rider (opcjonalnie)
- Windows 10/11

### Uruchamianie Aplikacji

1. **Sklonuj repozytorium:**
   ```bash
   git clone https://github.com/bauerpawel/Radio_Player.git
   cd Radio_Player
   ```

2. **Przywróć pakiety i zbuduj:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Uruchom aplikację:**
   ```bash
   dotnet run --project src/RadioPlayer.WPF
   ```

4. **Lub otwórz w Visual Studio:**
   - Otwórz `RadioPlayer.sln`
   - Naciśnij F5 aby zbudować i uruchomić

### Pierwsze Użycie

1. Aplikacja uruchamia się i inicjalizuje bazę danych SQLite
2. Automatycznie ładuje top 100 najlepiej ocenianych stacji
3. Kliknij dwukrotnie stację lub wybierz i kliknij "Play" aby rozpocząć strumieniowanie
4. Użyj wyszukiwania aby znaleźć stacje według nazwy, gatunku lub kraju
5. Kliknij ⭐ aby dodać stacje do ulubionych
6. Dostosuj głośność suwakiem
7. Zobacz szczegóły stacji przyciskiem Details
8. Sprawdź historię odtwarzania przyciskiem History

## 📦 Kompilacja do Samodzielnego Pliku EXE

### Metoda 1: Wdrożenie Samodzielne (Zalecane)

Tworzy samodzielny plik wykonywalny ze wszystkimi zależnościami (nie wymaga środowiska .NET runtime):

```bash
# Przejdź do katalogu projektu
cd Radio_Player

# Zbuduj dla Windows x64 (pojedynczy plik EXE z dołączonym .NET runtime)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true

# Wynik będzie w:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**Rozmiar pliku:** ~90-100 MB (zawiera pełne środowisko .NET runtime)

### Metoda 2: Wdrożenie Zależne od Framework

Tworzy mniejszy plik wykonywalny (wymaga zainstalowanego .NET 10 runtime w systemie docelowym):

```bash
# Zbuduj dla Windows x64 (wymaga zainstalowanego .NET 10)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true

# Wynik będzie w:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**Rozmiar pliku:** ~2-5 MB (wymaga .NET 10 runtime na docelowym PC)

### Metoda 3: Kompilacja z Przycinaniem (Najmniejszy Rozmiar)

Tworzy najmniejszy możliwy plik wykonywalny z usuniętym nieużywanym kodem:

```bash
# Zbuduj z przycinaniem (zaawansowane, może wymagać testowania)
dotnet publish src/RadioPlayer.WPF/RadioPlayer.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:TrimMode=link

# Wynik będzie w:
# src/RadioPlayer.WPF/bin/Release/net10.0-windows/win-x64/publish/RadioPlayer.exe
```

**Rozmiar pliku:** ~50-60 MB (w pełni samodzielny, przycięty)

### Dystrybucja

Po zbudowaniu, plik `RadioPlayer.exe` może być dystrybuowany na inne komputery z Windows. Dla wersji samodzielnych, wystarczy skopiować całą zawartość folderu `publish`.

**Uwaga:** Aplikacja tworzy swoją bazę danych w `%LocalAppData%\RadioPlayer\` przy pierwszym uruchomieniu.

## 📋 Stos Technologiczny

- **Framework**: WPF (.NET 10) dla Windows
- **Komponenty UI**: MaterialDesignThemes 5.1.0 (stylizacja Material Design)
- **Strumieniowanie Audio**: NAudio 2.2.1, NAudio.Vorbis 1.5.0 (licencja MIT)
- **Baza Danych**: Microsoft.Data.Sqlite 10.0.0
- **API**: Radio Browser API (darmowe, napędzane przez społeczność)
- **Odporność**: Polly 8.5.0 dla logiki ponawiania
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **Wstrzykiwanie Zależności**: Microsoft.Extensions.DependencyInjection 10.0.0
- **Zasobnik Systemowy**: System.Windows.Forms (NotifyIcon)

## 📖 Przewodnik Użytkownika

### Przeglądanie Stacji

1. **Top Stacji**: Kliknij "Top Stations" aby załadować najpopularniejsze stacje
2. **Wyszukiwanie**: Wprowadź nazwę stacji, gatunek lub kraj w filtrach i kliknij "Apply Filters"
3. **Ulubione**: Kliknij "Favorites" aby zobaczyć zapisane stacje
4. **Historia**: Kliknij "History" aby zobaczyć historię odtwarzania

### Odtwarzanie Radia

1. **Wybierz stację** z listy
2. **Kliknij dwukrotnie** lub kliknij przycisk **Play**
3. Obserwuj status: Łączenie → Buforowanie → Odtwarzanie
4. Sekcja **"Teraz odtwarzane"** pokazuje metadane bieżącego utworu z logo stacji
5. Użyj **suwaka głośności** aby dostosować głośność odtwarzania
6. Kliknij **Stop** aby zakończyć odtwarzanie

### Zarządzanie Ulubionymi

1. Wybierz stację
2. Kliknij przycisk **⭐** aby dodać do ulubionych (lub usuń jeśli już dodana)
3. Kliknij "Favorites" aby zobaczyć wszystkie zapisane stacje
4. Kliknij dwukrotnie aby odtworzyć z ulubionych

### Szczegóły Stacji

1. Kliknij przycisk **Details** przy dowolnej stacji
2. Zobacz szczegółowe informacje: kraj, język, tagi, strona główna
3. Zobacz szczegóły techniczne: kodek, bitrate, URL strumienia
4. Przeglądaj podobne stacje na podstawie pasujących tagów
5. Kliknij "Play Station" aby rozpocząć odtwarzanie bezpośrednio ze szczegółów

### Historia Odtwarzania

1. Kliknij przycisk **History** w pasku bocznym
2. Zobacz wszystkie sesje odtwarzania z datą, godziną i czasem trwania
3. Wyszukaj w historii według nazwy stacji
4. Kliknij "Play Again" aby ponownie odtworzyć stację
5. Zobacz statystyki (całkowita liczba rekordów i czas słuchania)
6. Wyczyść całą historię jeśli potrzeba

### Ustawienia

1. Kliknij ikonę **Ustawień** (⚙️) w prawym górnym rogu
2. Dostosuj **Czas Trwania Bufora** (3-30s) dla płynniejszego odtwarzania
3. Dostosuj **Czas Trwania Wstępnego Buforowania** (1-10s) dla szybszego startu
4. Włącz **Logowanie Debugowania** aby rozwiązywać problemy z buforowaniem
5. Włącz **Minimalizacja do Zasobnika** aby minimalizować do zasobnika systemowego zamiast paska zadań

### Zasobnik Systemowy

1. Włącz "Minimize to system tray" w Ustawieniach
2. Zminimalizuj okno lub kliknij X aby wysłać do zasobnika
3. Kliknij dwukrotnie ikonę zasobnika aby przywrócić okno
4. Kliknij prawym przyciskiem ikonę zasobnika dla menu kontekstowego (Przywróć, Wyjdź)

## 🐛 Rozwiązywanie Problemów

### Nie Można Zainicjalizować Bazy Danych
- Sprawdź czy katalog `%LocalAppData%/RadioPlayer` jest zapisywalny
- Zamknij inne instancje aplikacji
- Usuń `radioplayer.db` i uruchom ponownie aby odtworzyć

### Nie Można Połączyć się ze Stacjami
- Sprawdź połączenie internetowe
- Niektóre stacje mogą być offline (spróbuj innych)
- Zapora może blokować połączenia
- Sprawdź ustawienia urządzenia wyjściowego audio Windows

### Brak Dźwięku
- Sprawdź czy mixer głośności Windows pokazuje RadioPlayer
- Sprawdź suwak głośności aplikacji (czy nie jest wyciszony)
- Upewnij się że wybrano prawidłowe urządzenie wyjściowe audio
- Spróbuj innych stacji (niektóre mogą używać nieobsługiwanych kodeków)

### Problemy z Buforowaniem
- Zwiększ czas trwania bufora w Ustawieniach (spróbuj 15-20s)
- Wolne połączenie internetowe może powodować częste buforowanie
- Spróbuj stacji z niższym bitrate
- Włącz logowanie debugowania w Ustawieniach aby zdiagnozować problemy
- Aplikacja automatycznie ponownie łączy się przy przerwach w sieci

### Stacje MP3 Ponownie Buforują
- Próg bufora MP3 zredukowany do 32KB dla optymalnej wydajności
- Jeśli problemy utrzymują się, spróbuj zwiększyć czas trwania bufora w Ustawieniach

## 📄 Licencja

Ten projekt jest licencjonowany na licencji Apache-2.0 - zobacz plik [LICENSE](LICENSE) po szczegóły.

## 📞 Wsparcie

W przypadku problemów, pytań lub sugestii:
- Otwórz zgłoszenie na [GitHub](https://github.com/bauerpawel/Radio_Player/issues)
- Sprawdź sekcję rozwiązywania problemów powyżej
- Włącz logowanie debugowania w Ustawieniach aby zdiagnozować problemy z buforowaniem

---

**Zbudowano z ❤️ używając .NET 10 i WPF**

*Gotowy do strumieniowania ponad 51,000 stacji radiowych z całego świata!* 🌍📻
