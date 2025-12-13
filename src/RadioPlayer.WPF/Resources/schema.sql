-- Radio Player SQLite Database Schema
-- Based on research recommendations for .NET 10

-- Enable WAL mode for better concurrency (will be set via PRAGMA in code)
-- PRAGMA journal_mode = WAL;

-- Main stations table
CREATE TABLE IF NOT EXISTS RadioStations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StationUuid TEXT UNIQUE NOT NULL,
    Name TEXT NOT NULL,
    StreamUrl TEXT NOT NULL,
    Genre TEXT,
    Country TEXT,
    CountryCode TEXT,
    Language TEXT,
    Bitrate INTEGER,
    Codec TEXT,
    LogoUrl TEXT,
    Homepage TEXT,
    IsActive INTEGER DEFAULT 1,
    IsCustom INTEGER DEFAULT 0,
    DateAdded TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_stations_name ON RadioStations(Name);
CREATE INDEX IF NOT EXISTS idx_stations_genre ON RadioStations(Genre);
CREATE INDEX IF NOT EXISTS idx_stations_country ON RadioStations(Country);
CREATE INDEX IF NOT EXISTS idx_stations_uuid ON RadioStations(StationUuid);

-- Favorites table
CREATE TABLE IF NOT EXISTS Favorites (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StationId INTEGER NOT NULL,
    DateAdded TEXT DEFAULT CURRENT_TIMESTAMP,
    SortOrder INTEGER DEFAULT 0,
    FOREIGN KEY (StationId) REFERENCES RadioStations(Id) ON DELETE CASCADE
);

-- Index for favorites lookup
CREATE INDEX IF NOT EXISTS idx_favorites_station ON Favorites(StationId);
CREATE INDEX IF NOT EXISTS idx_favorites_sort ON Favorites(SortOrder);

-- Listening history table
CREATE TABLE IF NOT EXISTS ListeningHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StationId INTEGER NOT NULL,
    StartTime TEXT NOT NULL,
    DurationSeconds INTEGER DEFAULT 0,
    DateRecorded TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (StationId) REFERENCES RadioStations(Id) ON DELETE CASCADE
);

-- Indexes for history queries
CREATE INDEX IF NOT EXISTS idx_history_station ON ListeningHistory(StationId);
CREATE INDEX IF NOT EXISTS idx_history_date ON ListeningHistory(DateRecorded);
CREATE INDEX IF NOT EXISTS idx_history_start ON ListeningHistory(StartTime);

-- Settings table for application preferences
-- Keys used:
--   - DebugLogging: Boolean (true/false)
--   - MinimizeToTray: Boolean (true/false)
--   - BufferDuration: Integer (seconds)
--   - PreBufferDuration: Integer (seconds)
--   - Language: String (language code: pl, en, de)
--   - Volume: Integer (0-100)
--   - LastCacheUpdate: DateTime (ISO 8601)
--   - HotkeysEnabled: Boolean (true/false)
--   - Hotkey_PlayPause: String (serialized HotkeyConfiguration)
--   - Hotkey_Stop: String (serialized HotkeyConfiguration)
--   - Hotkey_NextStation: String (serialized HotkeyConfiguration)
--   - Hotkey_PreviousStation: String (serialized HotkeyConfiguration)
--   - Hotkey_VolumeUp: String (serialized HotkeyConfiguration)
--   - Hotkey_VolumeDown: String (serialized HotkeyConfiguration)
--   - Hotkey_Mute: String (serialized HotkeyConfiguration)
CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY NOT NULL,
    Value TEXT NOT NULL,
    DateModified TEXT DEFAULT CURRENT_TIMESTAMP
);
