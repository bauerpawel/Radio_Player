using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Detects the actual codec inside an OGG container by examining stream headers
/// OGG is just a container format - it can contain Opus, Vorbis, or FLAC
/// </summary>
public static class OggContentDetector
{
    /// <summary>
    /// Detects whether an OGG stream contains Opus or Vorbis data
    /// Returns "OPUS", "VORBIS", or "UNKNOWN"
    /// </summary>
    public static async Task<string> DetectOggCodecAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // OGG page header is 27 bytes + segment table
        // We need to read the first page to see the identification header

        var headerBuffer = new byte[512]; // Enough for OGG page header + identification header

        try
        {
            // Try to read first 512 bytes
            int totalRead = 0;
            while (totalRead < headerBuffer.Length)
            {
                int bytesRead = await stream.ReadAsync(headerBuffer, totalRead, headerBuffer.Length - totalRead, cancellationToken);
                if (bytesRead == 0)
                    break; // End of stream
                totalRead += bytesRead;
            }

            if (totalRead < 64)
                return "UNKNOWN"; // Not enough data

            // Look for OggS marker
            int oggSIndex = FindOggSMarker(headerBuffer, totalRead);
            if (oggSIndex == -1)
                return "UNKNOWN";

            // Skip OGG page header (27 bytes from OggS)
            int pageHeaderStart = oggSIndex;
            if (pageHeaderStart + 27 > totalRead)
                return "UNKNOWN";

            byte numSegments = headerBuffer[pageHeaderStart + 26];
            int segmentTableStart = pageHeaderStart + 27;

            if (segmentTableStart + numSegments > totalRead)
                return "UNKNOWN";

            // Skip segment table to get to payload
            int payloadStart = segmentTableStart + numSegments;

            if (payloadStart + 8 > totalRead)
                return "UNKNOWN";

            // Check payload for codec signature
            var payload = Encoding.ASCII.GetString(headerBuffer, payloadStart, Math.Min(8, totalRead - payloadStart));

            if (payload.StartsWith("OpusHead"))
            {
                DebugLogger.Log("DETECT", "OGG stream contains OPUS codec");
                return "OPUS";
            }
            else if (payload.Contains("vorbis"))
            {
                DebugLogger.Log("DETECT", "OGG stream contains VORBIS codec");
                return "VORBIS";
            }
            else
            {
                DebugLogger.Log("DETECT", $"OGG stream contains unknown codec (signature: {payload})");
                return "UNKNOWN";
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log("DETECT", $"Error detecting OGG codec: {ex.Message}");
            return "UNKNOWN";
        }
    }

    private static int FindOggSMarker(byte[] buffer, int length)
    {
        for (int i = 0; i < length - 4; i++)
        {
            if (buffer[i] == 'O' && buffer[i + 1] == 'g' &&
                buffer[i + 2] == 'g' && buffer[i + 3] == 'S')
            {
                return i;
            }
        }
        return -1;
    }
}
