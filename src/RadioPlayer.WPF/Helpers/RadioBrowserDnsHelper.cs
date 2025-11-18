using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Helper for discovering Radio Browser API servers via DNS lookup
/// Implements best practice from Radio Browser API documentation
/// </summary>
public static class RadioBrowserDnsHelper
{
    private const string DnsLookupHost = "all.api.radio-browser.info";
    private const string DefaultServer = "https://de1.api.radio-browser.info";

    /// <summary>
    /// Get a random Radio Browser API server URL via DNS lookup
    /// Falls back to default server if DNS lookup fails
    /// </summary>
    /// <returns>Random server URL (e.g., "https://de1.api.radio-browser.info")</returns>
    public static async Task<string> GetRandomServerUrlAsync()
    {
        try
        {
            // DNS lookup for all.api.radio-browser.info
            var hostEntry = await Dns.GetHostEntryAsync(DnsLookupHost);
            var addresses = hostEntry.AddressList;

            if (addresses.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DNS lookup for {DnsLookupHost} returned no addresses, using default");
                return DefaultServer;
            }

            // Pick random server from available addresses
            var random = new Random();
            var randomAddress = addresses[random.Next(addresses.Length)];

            // Reverse DNS lookup to get hostname
            var reverseHost = await Dns.GetHostEntryAsync(randomAddress);
            var hostname = reverseHost.HostName;

            var serverUrl = $"https://{hostname}";
            System.Diagnostics.Debug.WriteLine($"Selected Radio Browser server: {serverUrl}");

            return serverUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to resolve Radio Browser servers via DNS: {ex.Message}. Using default server.");
            return DefaultServer;
        }
    }

    /// <summary>
    /// Get all available Radio Browser API server URLs
    /// </summary>
    /// <returns>List of all server URLs</returns>
    public static async Task<string[]> GetAllServerUrlsAsync()
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(DnsLookupHost);
            var addresses = hostEntry.AddressList;

            var servers = new string[addresses.Length];

            for (int i = 0; i < addresses.Length; i++)
            {
                try
                {
                    var reverseHost = await Dns.GetHostEntryAsync(addresses[i]);
                    servers[i] = $"https://{reverseHost.HostName}";
                }
                catch
                {
                    // If reverse DNS fails, use IP directly
                    servers[i] = $"https://{addresses[i]}";
                }
            }

            return servers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to get all Radio Browser servers: {ex.Message}");
            return new[] { DefaultServer };
        }
    }
}
