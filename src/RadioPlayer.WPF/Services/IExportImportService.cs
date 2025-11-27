using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for exporting and importing application data (favorites, settings)
/// </summary>
public interface IExportImportService
{
    /// <summary>
    /// Exports favorites and settings to a JSON file
    /// </summary>
    /// <param name="filePath">Path where to save the backup file</param>
    /// <param name="includeCustomStations">Include custom stations in the backup</param>
    Task ExportToJsonAsync(string filePath, bool includeCustomStations = true);

    /// <summary>
    /// Imports favorites and settings from a JSON file
    /// </summary>
    /// <param name="filePath">Path to the backup file</param>
    /// <param name="mergeWithExisting">If true, merge with existing data; if false, replace all</param>
    Task ImportFromJsonAsync(string filePath, bool mergeWithExisting = true);

    /// <summary>
    /// Gets backup data as BackupData object (for preview)
    /// </summary>
    Task<BackupData> GetBackupDataAsync(bool includeCustomStations = true);

    /// <summary>
    /// Validates a backup file
    /// </summary>
    /// <param name="filePath">Path to the backup file</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateBackupFileAsync(string filePath);
}
