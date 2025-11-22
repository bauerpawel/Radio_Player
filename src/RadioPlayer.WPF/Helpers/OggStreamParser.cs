using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Simple OGG container parser for streaming (non-seekable) HTTP streams
/// Parses OGG page structure to extract Opus/Vorbis packets
/// </summary>
public class OggStreamParser
{
    private const int OGG_PAGE_HEADER_SIZE = 27;
    private readonly byte[] _pageHeaderBuffer = new byte[OGG_PAGE_HEADER_SIZE];
    private readonly byte[] _segmentTableBuffer = new byte[255];
    private long _pageSequence = 0;

    public async Task<OggPage?> ReadNextPageAsync(Stream stream, CancellationToken cancellationToken)
    {
        // Read OGG page header (27 bytes)
        int bytesRead = await ReadExactAsync(stream, _pageHeaderBuffer, 0, OGG_PAGE_HEADER_SIZE, cancellationToken);
        if (bytesRead < OGG_PAGE_HEADER_SIZE)
            return null; // End of stream

        // Verify OGG signature "OggS"
        if (_pageHeaderBuffer[0] != 'O' || _pageHeaderBuffer[1] != 'g' ||
            _pageHeaderBuffer[2] != 'g' || _pageHeaderBuffer[3] != 'S')
        {
            // Try to resync by finding next OggS marker
            if (!await ResyncToOggPageAsync(stream, cancellationToken))
                return null;

            // After resync, _pageHeaderBuffer[0-3] contains "OggS"
            // Read remaining 23 bytes of header
            bytesRead = await ReadExactAsync(stream, _pageHeaderBuffer, 4, OGG_PAGE_HEADER_SIZE - 4, cancellationToken);
            if (bytesRead < OGG_PAGE_HEADER_SIZE - 4)
                return null;
        }

        // Parse header fields
        byte streamStructureVersion = _pageHeaderBuffer[4]; // Should be 0
        byte headerType = _pageHeaderBuffer[5]; // Flags: 0x01=continuation, 0x02=BOS, 0x04=EOS
        long granulePosition = BitConverter.ToInt64(_pageHeaderBuffer, 6);
        int streamSerialNumber = BitConverter.ToInt32(_pageHeaderBuffer, 14);
        int pageSequenceNumber = BitConverter.ToInt32(_pageHeaderBuffer, 18);
        uint checksum = BitConverter.ToUInt32(_pageHeaderBuffer, 22);
        byte numSegments = _pageHeaderBuffer[26];

        // Read segment table
        bytesRead = await ReadExactAsync(stream, _segmentTableBuffer, 0, numSegments, cancellationToken);
        if (bytesRead < numSegments)
            return null;

        // Calculate total payload size
        int totalPayloadSize = 0;
        for (int i = 0; i < numSegments; i++)
            totalPayloadSize += _segmentTableBuffer[i];

        // Read payload data
        var payloadData = new byte[totalPayloadSize];
        bytesRead = await ReadExactAsync(stream, payloadData, 0, totalPayloadSize, cancellationToken);
        if (bytesRead < totalPayloadSize)
            return null;

        _pageSequence++;

        return new OggPage
        {
            HeaderType = headerType,
            GranulePosition = granulePosition,
            StreamSerialNumber = streamSerialNumber,
            PageSequenceNumber = pageSequenceNumber,
            PayloadData = payloadData,
            IsBeginningOfStream = (headerType & 0x02) != 0,
            IsEndOfStream = (headerType & 0x04) != 0,
            IsContinuation = (headerType & 0x01) != 0
        };
    }

    /// <summary>
    /// Reads exact number of bytes from stream, handling partial reads
    /// </summary>
    private async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
            if (bytesRead == 0)
                return totalRead; // End of stream

            totalRead += bytesRead;
        }
        return totalRead;
    }

    /// <summary>
    /// Attempts to resync to next OggS marker after stream corruption
    /// </summary>
    private async Task<bool> ResyncToOggPageAsync(Stream stream, CancellationToken cancellationToken)
    {
        const int maxSearchBytes = 8192;
        int bytesSearched = 0;
        int markerIndex = 0;
        byte[] oggMarker = Encoding.ASCII.GetBytes("OggS");

        while (bytesSearched < maxSearchBytes && !cancellationToken.IsCancellationRequested)
        {
            int b = stream.ReadByte();
            if (b == -1)
                return false; // End of stream

            bytesSearched++;

            if (b == oggMarker[markerIndex])
            {
                markerIndex++;
                if (markerIndex == 4)
                {
                    // Found OggS marker - we've already consumed 4 bytes
                    // Put them back in the header buffer for next read
                    _pageHeaderBuffer[0] = (byte)'O';
                    _pageHeaderBuffer[1] = (byte)'g';
                    _pageHeaderBuffer[2] = (byte)'g';
                    _pageHeaderBuffer[3] = (byte)'S';
                    return true;
                }
            }
            else
            {
                markerIndex = (b == oggMarker[0]) ? 1 : 0;
            }
        }

        return false;
    }
}

/// <summary>
/// Represents a single OGG page
/// </summary>
public class OggPage
{
    public byte HeaderType { get; set; }
    public long GranulePosition { get; set; }
    public int StreamSerialNumber { get; set; }
    public int PageSequenceNumber { get; set; }
    public byte[] PayloadData { get; set; } = Array.Empty<byte>();
    public bool IsBeginningOfStream { get; set; }
    public bool IsEndOfStream { get; set; }
    public bool IsContinuation { get; set; }
}
