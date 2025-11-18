using System.Text.Json.Serialization;

namespace RadioPlayer.WPF.Services.DTOs;

/// <summary>
/// DTO for Radio Browser API language response
/// </summary>
public class LanguageDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("iso_639")]
    public string Iso639 { get; set; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public int StationCount { get; set; }
}
