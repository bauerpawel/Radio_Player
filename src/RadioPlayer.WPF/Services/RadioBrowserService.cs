using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Polly;
using Polly.Retry;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Services.DTOs;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Service for interacting with Radio Browser API
/// Implements resilient HTTP calls with Polly retry policy
/// </summary>
public class RadioBrowserService : IRadioBrowserService
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly string _baseUrl;

    public RadioBrowserService()
        : this(new HttpClient(), null)
    {
    }

    public RadioBrowserService(HttpClient httpClient)
        : this(httpClient, null)
    {
    }

    public RadioBrowserService(HttpClient httpClient, string? baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl ?? AppConstants.RadioBrowserApiBaseUrl;

        // Configure HttpClient
        _httpClient.Timeout = AppConstants.Network.HttpTimeout;

        // Only add User-Agent if not already present
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", AppConstants.HttpHeaders.UserAgent);
        }

        // Configure Polly retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: AppConstants.Network.RetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(AppConstants.Network.ExponentialBackoffBase, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    // TODO: Add logging
                    System.Diagnostics.Debug.WriteLine(
                        $"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    /// <summary>
    /// Create RadioBrowserService with automatic DNS-based server discovery
    /// Recommended approach - discovers available servers via all.api.radio-browser.info
    /// </summary>
    public static async Task<RadioBrowserService> CreateWithDnsLookupAsync()
    {
        var serverUrl = await RadioBrowserDnsHelper.GetRandomServerUrlAsync();
        return new RadioBrowserService(new HttpClient(), serverUrl);
    }

    /// <summary>
    /// Create RadioBrowserService with automatic DNS-based server discovery using provided HttpClient
    /// </summary>
    public static async Task<RadioBrowserService> CreateWithDnsLookupAsync(HttpClient httpClient)
    {
        var serverUrl = await RadioBrowserDnsHelper.GetRandomServerUrlAsync();
        return new RadioBrowserService(httpClient, serverUrl);
    }

    public async Task<List<RadioStation>> SearchStationsAsync(
        string? searchTerm = null,
        string? country = null,
        string? language = null,
        string? tag = null,
        string? codec = null,
        int? bitrateMin = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["hidebroken"] = "true",
            ["order"] = "votes",
            ["reverse"] = "true"
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
            queryParams["name"] = searchTerm;

        if (!string.IsNullOrWhiteSpace(country))
            queryParams["country"] = country;

        if (!string.IsNullOrWhiteSpace(language))
            queryParams["language"] = language;

        if (!string.IsNullOrWhiteSpace(tag))
            queryParams["tag"] = tag;

        if (!string.IsNullOrWhiteSpace(codec))
            queryParams["codec"] = codec;

        if (bitrateMin.HasValue)
            queryParams["bitrateMin"] = bitrateMin.Value.ToString();

        var url = BuildUrl("/json/stations/search", queryParams);

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RadioStationDto>>(url, cancellationToken);
            return dtos?.Select(MapToRadioStation).ToList() ?? new List<RadioStation>();
        });
    }

    public async Task<List<RadioStation>> GetStationsByCountryAsync(
        string country,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["hidebroken"] = "true",
            ["order"] = "votes",
            ["reverse"] = "true"
        };

        var encodedCountry = Uri.EscapeDataString(country);
        var url = BuildUrl($"/json/stations/bycountry/{encodedCountry}", queryParams);

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RadioStationDto>>(url, cancellationToken);
            return dtos?.Select(MapToRadioStation).ToList() ?? new List<RadioStation>();
        });
    }

    public async Task<List<RadioStation>> GetStationsByTagAsync(
        string tag,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["hidebroken"] = "true",
            ["order"] = "votes",
            ["reverse"] = "true"
        };

        var encodedTag = Uri.EscapeDataString(tag);
        var url = BuildUrl($"/json/stations/bytag/{encodedTag}", queryParams);

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RadioStationDto>>(url, cancellationToken);
            return dtos?.Select(MapToRadioStation).ToList() ?? new List<RadioStation>();
        });
    }

    public async Task<List<RadioStation>> GetTopVotedStationsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["hidebroken"] = "true"
        };

        var url = BuildUrl("/json/stations/topvote", queryParams);

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RadioStationDto>>(url, cancellationToken);
            return dtos?.Select(MapToRadioStation).ToList() ?? new List<RadioStation>();
        });
    }

    public async Task<List<RadioStation>> GetTopClickedStationsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["hidebroken"] = "true"
        };

        var url = BuildUrl("/json/stations/topclick", queryParams);

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RadioStationDto>>(url, cancellationToken);
            return dtos?.Select(MapToRadioStation).ToList() ?? new List<RadioStation>();
        });
    }

    public async Task RegisterClickAsync(
        string stationUuid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationUuid))
            return;

        var url = $"{_baseUrl}/json/url/{Uri.EscapeDataString(stationUuid)}";

        await ExecuteWithRetryAsync(async () =>
        {
            // Register click - counted once per day per IP per station
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        });
    }

    public async Task<List<string>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/json/countries", new Dictionary<string, string>
        {
            ["order"] = "name"
        });

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<CountryDto>>(url, cancellationToken);
            return dtos?
                .Where(c => c.StationCount > 0)
                .Select(c => c.Name)
                .OrderBy(n => n)
                .ToList() ?? new List<string>();
        });
    }

    public async Task<List<string>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/json/languages", new Dictionary<string, string>
        {
            ["order"] = "name"
        });

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<LanguageDto>>(url, cancellationToken);
            return dtos?
                .Where(l => l.StationCount > 0)
                .Select(l => l.Name)
                .OrderBy(n => n)
                .ToList() ?? new List<string>();
        });
    }

    public async Task<List<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/json/tags", new Dictionary<string, string>
        {
            ["order"] = "name"
        });

        return await ExecuteWithRetryAsync(async () =>
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<TagDto>>(url, cancellationToken);
            return dtos?
                .Where(t => t.StationCount > 0)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList() ?? new List<string>();
        });
    }

    /// <summary>
    /// Execute action with Polly retry policy
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        return await _retryPolicy.ExecuteAsync(action);
    }

    /// <summary>
    /// Build complete URL with query parameters
    /// </summary>
    private string BuildUrl(string path, Dictionary<string, string> queryParams)
    {
        var query = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{_baseUrl}{path}?{query}";
    }

    /// <summary>
    /// Map DTO to domain model
    /// </summary>
    private RadioStation MapToRadioStation(RadioStationDto dto)
    {
        return new RadioStation
        {
            StationUuid = dto.StationUuid,
            Name = dto.Name,
            UrlResolved = !string.IsNullOrWhiteSpace(dto.UrlResolved) ? dto.UrlResolved : dto.Url,
            Codec = dto.Codec,
            Bitrate = dto.Bitrate,
            Country = dto.Country,
            CountryCode = dto.CountryCode,
            Language = dto.Language,
            Tags = dto.Tags,
            Favicon = dto.Favicon,
            Homepage = dto.Homepage,
            LastCheckOk = dto.LastCheckOk == 1,
            Votes = dto.Votes,
            ClickCount = dto.ClickCount,
            IsActive = true,
            DateAdded = DateTime.UtcNow
        };
    }
}
