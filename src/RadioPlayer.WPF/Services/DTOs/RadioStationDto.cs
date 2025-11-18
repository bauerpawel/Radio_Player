using System.Text.Json.Serialization;

namespace RadioPlayer.WPF.Services.DTOs;

/// <summary>
/// DTO for Radio Browser API station response
/// Maps JSON field names to C# properties
/// </summary>
public class RadioStationDto
{
    [JsonPropertyName("stationuuid")]
    public string StationUuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; set; } = string.Empty;

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("countrycode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("languagecodes")]
    public string LanguageCodes { get; set; } = string.Empty;

    [JsonPropertyName("votes")]
    public int Votes { get; set; }

    [JsonPropertyName("lastchangetime")]
    public string LastChangeTime { get; set; } = string.Empty;

    [JsonPropertyName("lastchangetime_iso8601")]
    public string LastChangeTimeIso8601 { get; set; } = string.Empty;

    [JsonPropertyName("codec")]
    public string Codec { get; set; } = string.Empty;

    [JsonPropertyName("bitrate")]
    public int Bitrate { get; set; }

    [JsonPropertyName("hls")]
    public int Hls { get; set; }

    [JsonPropertyName("lastcheckok")]
    public int LastCheckOk { get; set; }

    [JsonPropertyName("lastchecktime")]
    public string LastCheckTime { get; set; } = string.Empty;

    [JsonPropertyName("lastchecktime_iso8601")]
    public string LastCheckTimeIso8601 { get; set; } = string.Empty;

    [JsonPropertyName("lastcheckoktime")]
    public string LastCheckOkTime { get; set; } = string.Empty;

    [JsonPropertyName("lastcheckoktime_iso8601")]
    public string LastCheckOkTimeIso8601 { get; set; } = string.Empty;

    [JsonPropertyName("lastlocalchecktime")]
    public string LastLocalCheckTime { get; set; } = string.Empty;

    [JsonPropertyName("lastlocalchecktime_iso8601")]
    public string LastLocalCheckTimeIso8601 { get; set; } = string.Empty;

    [JsonPropertyName("clicktimestamp")]
    public string ClickTimestamp { get; set; } = string.Empty;

    [JsonPropertyName("clicktimestamp_iso8601")]
    public string ClickTimestampIso8601 { get; set; } = string.Empty;

    [JsonPropertyName("clickcount")]
    public int ClickCount { get; set; }

    [JsonPropertyName("clicktrend")]
    public int ClickTrend { get; set; }

    [JsonPropertyName("ssl_error")]
    public int SslError { get; set; }

    [JsonPropertyName("geo_lat")]
    public double? GeoLat { get; set; }

    [JsonPropertyName("geo_long")]
    public double? GeoLong { get; set; }

    [JsonPropertyName("has_extended_info")]
    public bool HasExtendedInfo { get; set; }
}
