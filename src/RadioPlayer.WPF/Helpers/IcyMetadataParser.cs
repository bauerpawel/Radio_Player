using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Parser for ICY/Shoutcast metadata embedded in radio streams
/// Protocol: Every N bytes of audio data, metadata is inserted
/// Format: 1 byte length + (length × 16) bytes of metadata
/// </summary>
public class IcyMetadataParser
{
    private readonly int _metadataInterval;
    private int _bytesUntilMetadata;

    /// <summary>
    /// Create ICY metadata parser
    /// </summary>
    /// <param name="metadataInterval">Bytes between metadata blocks (from icy-metaint header)</param>
    public IcyMetadataParser(int metadataInterval)
    {
        _metadataInterval = metadataInterval;
        _bytesUntilMetadata = metadataInterval;
    }

    /// <summary>
    /// Parse stream buffer and extract audio data and metadata
    /// </summary>
    public IcyMetadataResult ParseStream(byte[] buffer, int offset, int count)
    {
        var audioData = new List<byte>();
        IcyMetadata? metadata = null;

        int position = offset;
        int remaining = count;

        while (remaining > 0)
        {
            if (_bytesUntilMetadata > 0)
            {
                // Extract audio data
                int audioBytes = Math.Min(_bytesUntilMetadata, remaining);

                for (int i = 0; i < audioBytes; i++)
                {
                    audioData.Add(buffer[position + i]);
                }

                position += audioBytes;
                remaining -= audioBytes;
                _bytesUntilMetadata -= audioBytes;
            }
            else
            {
                // Read metadata length byte (× 16)
                if (remaining < 1)
                    break;

                int metadataLength = buffer[position] * 16;
                position++;
                remaining--;

                if (metadataLength > 0)
                {
                    // Read metadata bytes
                    if (remaining < metadataLength)
                    {
                        // Not enough data, wait for more
                        _bytesUntilMetadata = 0;
                        break;
                    }

                    var metadataBytes = new byte[metadataLength];
                    Array.Copy(buffer, position, metadataBytes, 0, metadataLength);

                    metadata = ParseMetadata(metadataBytes);

                    position += metadataLength;
                    remaining -= metadataLength;
                }

                // Reset counter for next metadata block
                _bytesUntilMetadata = _metadataInterval;
            }
        }

        return new IcyMetadataResult
        {
            AudioData = audioData.ToArray(),
            Metadata = metadata
        };
    }

    /// <summary>
    /// Parse metadata bytes into IcyMetadata object
    /// Format: StreamTitle='Artist - Title';StreamUrl='http://...';
    /// </summary>
    private IcyMetadata ParseMetadata(byte[] metadataBytes)
    {
        // Convert to string (ASCII or UTF-8)
        var metadataString = Encoding.UTF8.GetString(metadataBytes).TrimEnd('\0');

        var metadata = new IcyMetadata();

        // Extract StreamTitle
        var streamTitleMatch = Regex.Match(metadataString, @"StreamTitle='([^']*)';?");
        if (streamTitleMatch.Success)
        {
            metadata.StreamTitle = streamTitleMatch.Groups[1].Value;
            metadata.ParseStreamTitle(); // Parse "Artist - Title"
        }

        // Extract StreamUrl
        var streamUrlMatch = Regex.Match(metadataString, @"StreamUrl='([^']*)';?");
        if (streamUrlMatch.Success)
        {
            metadata.StreamUrl = streamUrlMatch.Groups[1].Value;
        }

        metadata.ReceivedAt = DateTime.UtcNow;

        return metadata;
    }
}

/// <summary>
/// Result of parsing ICY metadata from stream
/// </summary>
public class IcyMetadataResult
{
    /// <summary>
    /// Extracted audio data (without metadata)
    /// </summary>
    public byte[] AudioData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Parsed metadata (null if no metadata in this chunk)
    /// </summary>
    public IcyMetadata? Metadata { get; set; }
}
