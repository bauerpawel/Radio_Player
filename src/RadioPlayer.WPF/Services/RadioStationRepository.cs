using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Repository for managing radio stations in local SQLite database
/// Uses ADO.NET for optimal performance with connection pooling
/// NOTE: SQLite does NOT support true async I/O - async methods run synchronously under the hood
/// </summary>
public class RadioStationRepository : IRadioStationRepository
{
    private readonly string _connectionString;
    private readonly string _databasePath;

    public RadioStationRepository()
        : this(AppConstants.Database.DatabasePath)
    {
    }

    public RadioStationRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = $"Data Source={_databasePath};Mode=ReadWriteCreate;Pooling=True";

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    #region Initialization

    public async Task InitializeDatabaseAsync()
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Enable WAL mode for better concurrency
            using (var walCommand = connection.CreateCommand())
            {
                walCommand.CommandText = "PRAGMA journal_mode = 'WAL'";
                walCommand.ExecuteNonQuery();
            }

            // Create tables
            CreateTables(connection);
        });
    }

    private void CreateTables(SqliteConnection connection)
    {
        var schema = @"
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
                DateAdded TEXT DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_stations_name ON RadioStations(Name);
            CREATE INDEX IF NOT EXISTS idx_stations_genre ON RadioStations(Genre);
            CREATE INDEX IF NOT EXISTS idx_stations_country ON RadioStations(Country);
            CREATE INDEX IF NOT EXISTS idx_stations_uuid ON RadioStations(StationUuid);

            CREATE TABLE IF NOT EXISTS Favorites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StationId INTEGER NOT NULL,
                DateAdded TEXT DEFAULT CURRENT_TIMESTAMP,
                SortOrder INTEGER DEFAULT 0,
                FOREIGN KEY (StationId) REFERENCES RadioStations(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_favorites_station ON Favorites(StationId);
            CREATE INDEX IF NOT EXISTS idx_favorites_sort ON Favorites(SortOrder);

            CREATE TABLE IF NOT EXISTS ListeningHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StationId INTEGER NOT NULL,
                StartTime TEXT NOT NULL,
                DurationSeconds INTEGER DEFAULT 0,
                DateRecorded TEXT DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (StationId) REFERENCES RadioStations(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_history_station ON ListeningHistory(StationId);
            CREATE INDEX IF NOT EXISTS idx_history_date ON ListeningHistory(DateRecorded);
            CREATE INDEX IF NOT EXISTS idx_history_start ON ListeningHistory(StartTime);

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY NOT NULL,
                Value TEXT NOT NULL,
                DateModified TEXT DEFAULT CURRENT_TIMESTAMP
            );
        ";

        using var command = connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();
    }

    #endregion

    #region Station Management

    public async Task<List<RadioStation>> GetAllStationsAsync()
    {
        return await Task.Run(() =>
        {
            var stations = new List<RadioStation>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, StationUuid, Name, StreamUrl, Genre, Country, CountryCode,
                       Language, Bitrate, Codec, LogoUrl, Homepage, IsActive, DateAdded
                FROM RadioStations
                WHERE IsActive = 1
                ORDER BY Name";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                stations.Add(MapReaderToStation(reader));
            }

            return stations;
        });
    }

    public async Task<RadioStation?> GetStationByIdAsync(int id)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, StationUuid, Name, StreamUrl, Genre, Country, CountryCode,
                       Language, Bitrate, Codec, LogoUrl, Homepage, IsActive, DateAdded
                FROM RadioStations
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToStation(reader);
            }

            return null;
        });
    }

    public async Task<RadioStation?> GetStationByUuidAsync(string uuid)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, StationUuid, Name, StreamUrl, Genre, Country, CountryCode,
                       Language, Bitrate, Codec, LogoUrl, Homepage, IsActive, DateAdded
                FROM RadioStations
                WHERE StationUuid = @Uuid";

            command.Parameters.AddWithValue("@Uuid", uuid);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToStation(reader);
            }

            return null;
        });
    }

    public async Task<int> AddStationAsync(RadioStation station)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO RadioStations
                (StationUuid, Name, StreamUrl, Genre, Country, CountryCode, Language,
                 Bitrate, Codec, LogoUrl, Homepage, IsActive, DateAdded)
                VALUES
                (@StationUuid, @Name, @StreamUrl, @Genre, @Country, @CountryCode, @Language,
                 @Bitrate, @Codec, @LogoUrl, @Homepage, @IsActive, @DateAdded);
                SELECT last_insert_rowid();";

            AddStationParameters(command, station);

            var id = Convert.ToInt32(command.ExecuteScalar());
            return id;
        });
    }

    public async Task UpdateStationAsync(RadioStation station)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RadioStations
                SET StationUuid = @StationUuid,
                    Name = @Name,
                    StreamUrl = @StreamUrl,
                    Genre = @Genre,
                    Country = @Country,
                    CountryCode = @CountryCode,
                    Language = @Language,
                    Bitrate = @Bitrate,
                    Codec = @Codec,
                    LogoUrl = @LogoUrl,
                    Homepage = @Homepage,
                    IsActive = @IsActive
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Id", station.Id);
            AddStationParameters(command, station);

            command.ExecuteNonQuery();
        });
    }

    public async Task DeleteStationAsync(int id)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RadioStations WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            command.ExecuteNonQuery();
        });
    }

    public async Task<List<RadioStation>> SearchStationsAsync(string searchTerm)
    {
        return await Task.Run(() =>
        {
            var stations = new List<RadioStation>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, StationUuid, Name, StreamUrl, Genre, Country, CountryCode,
                       Language, Bitrate, Codec, LogoUrl, Homepage, IsActive, DateAdded
                FROM RadioStations
                WHERE IsActive = 1
                  AND (Name LIKE @Search OR Genre LIKE @Search OR Country LIKE @Search)
                ORDER BY Name";

            command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                stations.Add(MapReaderToStation(reader));
            }

            return stations;
        });
    }

    #endregion

    #region Favorites Management

    public async Task<List<RadioStation>> GetFavoriteStationsAsync()
    {
        return await Task.Run(() =>
        {
            var stations = new List<RadioStation>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT s.Id, s.StationUuid, s.Name, s.StreamUrl, s.Genre, s.Country, s.CountryCode,
                       s.Language, s.Bitrate, s.Codec, s.LogoUrl, s.Homepage, s.IsActive, s.DateAdded
                FROM RadioStations s
                INNER JOIN Favorites f ON s.Id = f.StationId
                WHERE s.IsActive = 1
                ORDER BY f.SortOrder, f.DateAdded DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var station = MapReaderToStation(reader);
                station.IsFavorite = true;
                stations.Add(station);
            }

            return stations;
        });
    }

    public async Task AddToFavoritesAsync(int stationId)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO Favorites (StationId, DateAdded, SortOrder)
                VALUES (@StationId, @DateAdded, 0)";

            command.Parameters.AddWithValue("@StationId", stationId);
            command.Parameters.AddWithValue("@DateAdded", DateTime.UtcNow.ToString("o"));

            command.ExecuteNonQuery();
        });
    }

    public async Task RemoveFromFavoritesAsync(int stationId)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Favorites WHERE StationId = @StationId";
            command.Parameters.AddWithValue("@StationId", stationId);

            command.ExecuteNonQuery();
        });
    }

    public async Task<bool> IsFavoriteAsync(int stationId)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Favorites WHERE StationId = @StationId";
            command.Parameters.AddWithValue("@StationId", stationId);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        });
    }

    public async Task UpdateFavoriteSortOrderAsync(int favoriteId, int newSortOrder)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Favorites
                SET SortOrder = @SortOrder
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Id", favoriteId);
            command.Parameters.AddWithValue("@SortOrder", newSortOrder);

            command.ExecuteNonQuery();
        });
    }

    #endregion

    #region Listening History

    public async Task AddListeningHistoryAsync(ListeningHistory history)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ListeningHistory (StationId, StartTime, DurationSeconds, DateRecorded)
                VALUES (@StationId, @StartTime, @DurationSeconds, @DateRecorded)";

            command.Parameters.AddWithValue("@StationId", history.StationId);
            command.Parameters.AddWithValue("@StartTime", history.StartTime.ToString("o"));
            command.Parameters.AddWithValue("@DurationSeconds", history.DurationSeconds);
            command.Parameters.AddWithValue("@DateRecorded", history.DateRecorded.ToString("o"));

            command.ExecuteNonQuery();
        });
    }

    public async Task<List<ListeningHistory>> GetRecentHistoryAsync(int limit = 50)
    {
        return await Task.Run(() =>
        {
            var history = new List<ListeningHistory>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT h.Id, h.StationId, h.StartTime, h.DurationSeconds, h.DateRecorded,
                       s.Name, s.StreamUrl
                FROM ListeningHistory h
                INNER JOIN RadioStations s ON h.StationId = s.Id
                ORDER BY h.DateRecorded DESC
                LIMIT @Limit";

            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new ListeningHistory
                {
                    Id = reader.GetInt32(0),
                    StationId = reader.GetInt32(1),
                    StartTime = DateTime.Parse(reader.GetString(2)),
                    DurationSeconds = reader.GetInt32(3),
                    DateRecorded = DateTime.Parse(reader.GetString(4)),
                    Station = new RadioStation
                    {
                        Id = reader.GetInt32(1),
                        Name = reader.GetString(5),
                        UrlResolved = reader.GetString(6)
                    }
                });
            }

            return history;
        });
    }

    public async Task<List<ListeningHistory>> GetHistoryForStationAsync(int stationId)
    {
        return await Task.Run(() =>
        {
            var history = new List<ListeningHistory>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, StationId, StartTime, DurationSeconds, DateRecorded
                FROM ListeningHistory
                WHERE StationId = @StationId
                ORDER BY DateRecorded DESC";

            command.Parameters.AddWithValue("@StationId", stationId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new ListeningHistory
                {
                    Id = reader.GetInt32(0),
                    StationId = reader.GetInt32(1),
                    StartTime = DateTime.Parse(reader.GetString(2)),
                    DurationSeconds = reader.GetInt32(3),
                    DateRecorded = DateTime.Parse(reader.GetString(4))
                });
            }

            return history;
        });
    }

    public async Task<Dictionary<int, int>> GetMostListenedStationsAsync(int limit = 10)
    {
        return await Task.Run(() =>
        {
            var stats = new Dictionary<int, int>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT StationId, SUM(DurationSeconds) as TotalSeconds
                FROM ListeningHistory
                GROUP BY StationId
                ORDER BY TotalSeconds DESC
                LIMIT @Limit";

            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var stationId = reader.GetInt32(0);
                var totalSeconds = reader.GetInt32(1);
                stats[stationId] = totalSeconds;
            }

            return stats;
        });
    }

    public async Task ClearListeningHistoryAsync()
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ListeningHistory";
            command.ExecuteNonQuery();
        });
    }

    #endregion

    #region Database Maintenance

    public async Task<long> GetDatabaseSizeAsync()
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(_databasePath))
                return 0;

            var fileInfo = new FileInfo(_databasePath);
            return fileInfo.Length;
        });
    }

    public async Task VacuumDatabaseAsync()
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM";
            command.ExecuteNonQuery();
        });
    }

    #endregion

    #region Settings Management

    public async Task<string?> GetSettingAsync(string key)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = @Key";
            command.Parameters.AddWithValue("@Key", key);

            var result = command.ExecuteScalar();
            return result?.ToString();
        });
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Settings (Key, Value, DateModified)
                VALUES (@Key, @Value, @DateModified)
                ON CONFLICT(Key) DO UPDATE SET
                    Value = @Value,
                    DateModified = @DateModified";

            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@DateModified", DateTime.UtcNow.ToString("o"));

            command.ExecuteNonQuery();
        });
    }

    #endregion

    #region Helper Methods

    private void AddStationParameters(SqliteCommand command, RadioStation station)
    {
        command.Parameters.AddWithValue("@StationUuid", station.StationUuid);
        command.Parameters.AddWithValue("@Name", station.Name);
        command.Parameters.AddWithValue("@StreamUrl", station.UrlResolved);
        command.Parameters.AddWithValue("@Genre", station.Tags ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Country", station.Country ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CountryCode", station.CountryCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Language", station.Language ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Bitrate", station.Bitrate);
        command.Parameters.AddWithValue("@Codec", station.Codec ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LogoUrl", station.Favicon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Homepage", station.Homepage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", station.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@DateAdded", station.DateAdded.ToString("o"));
    }

    private RadioStation MapReaderToStation(SqliteDataReader reader)
    {
        return new RadioStation
        {
            Id = reader.GetInt32(0),
            StationUuid = reader.GetString(1),
            Name = reader.GetString(2),
            UrlResolved = reader.GetString(3),
            Tags = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Country = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            CountryCode = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Language = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Bitrate = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            Codec = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Favicon = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            Homepage = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            IsActive = reader.GetInt32(12) == 1,
            DateAdded = DateTime.Parse(reader.GetString(13))
        };
    }

    #endregion
}
