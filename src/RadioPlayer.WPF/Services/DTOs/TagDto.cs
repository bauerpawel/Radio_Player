using System.Text.Json.Serialization;

namespace RadioPlayer.WPF.Services.DTOs;

/// <summary>
/// DTO for Radio Browser API tag/genre response
/// </summary>
public class TagDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public int StationCount { get; set; }
}
