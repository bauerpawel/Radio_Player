using System.Text.Json.Serialization;

namespace RadioPlayer.WPF.Services.DTOs;

/// <summary>
/// DTO for Radio Browser API country response
/// </summary>
public class CountryDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("iso_3166_1")]
    public string Iso31661 { get; set; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public int StationCount { get; set; }
}
