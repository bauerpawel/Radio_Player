using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Stream wrapper that unwraps FLAC data from OGG container
/// Reads OGG pages and extracts raw FLAC packets, presenting them as a continuous FLAC stream
/// Supports limited forward-only seeking for metadata parsing
/// </summary>
public class OggFlacUnwrappingStream : Stream
{
    private readonly OggStreamParser _oggParser;
    private readonly Stream _innerStream;
    private MemoryStream _currentPacketBuffer;
    private bool _headerWritten = false;
    private long _position = 0;
    private MemoryStream _metadataBuffer; // Buffer for metadata blocks to enable seeking

    public OggFlacUnwrappingStream(Stream innerStream)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _oggParser = new OggStreamParser();
        _currentPacketBuffer = new MemoryStream();
        _metadataBuffer = new MemoryStream();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true; // Enable seeking for metadata parsing (forward-only)
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        // Support only forward seeking (used by metadata parser and frame decoder)
        if (origin == SeekOrigin.Current && offset >= 0)
        {
            // Skip forward by reading and discarding bytes
            var skipBuffer = new byte[Math.Min(8192, offset)];
            long remaining = offset;

            while (remaining > 0)
            {
                int toRead = (int)Math.Min(skipBuffer.Length, remaining);
                int bytesRead = Read(skipBuffer, 0, toRead);
                if (bytesRead == 0)
                    break; // End of stream
                remaining -= bytesRead;
            }

            return _position;
        }
        else if (origin == SeekOrigin.Begin)
        {
            // Seek to absolute position
            if (offset == _position)
            {
                // Already at target position
                return _position;
            }
            else if (offset > _position)
            {
                // Forward seek - convert to relative seek from current position
                return Seek(offset - _position, SeekOrigin.Current);
            }
            else
            {
                // Backward seek - not supported for non-seekable streams
                throw new NotSupportedException($"Backward seeking (from {_position} to {offset}) is not supported for non-seekable streams");
            }
        }

        throw new NotSupportedException($"Seeking with origin={origin} and offset={offset} is not supported for non-seekable streams");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < count && !cancellationToken.IsCancellationRequested)
        {
            // First, try to read from current packet buffer
            if (_currentPacketBuffer.Position < _currentPacketBuffer.Length)
            {
                int bytesToRead = Math.Min(count - totalRead, (int)(_currentPacketBuffer.Length - _currentPacketBuffer.Position));
                int bytesRead = _currentPacketBuffer.Read(buffer, offset + totalRead, bytesToRead);
                totalRead += bytesRead;
                _position += bytesRead;

                if (totalRead >= count)
                    break;
            }

            // Need more data - read next OGG page
            var page = await _oggParser.ReadNextPageAsync(_innerStream, cancellationToken);
            if (page == null)
                break; // End of stream

            // For FLAC in OGG, we need to handle the special header packet
            if (!_headerWritten && page.IsBeginningOfStream)
            {
                // First packet contains FLAC header with "FLAC" signature (4 bytes) + metadata
                // We need to convert OGG FLAC header to native FLAC format
                if (page.PayloadData.Length >= 13 &&
                    page.PayloadData[0] == 0x7F && // OGG FLAC marker
                    page.PayloadData[1] == 'F' &&
                    page.PayloadData[2] == 'L' &&
                    page.PayloadData[3] == 'A' &&
                    page.PayloadData[4] == 'C')
                {
                    // Skip OGG FLAC header (13 bytes: 0x7F + "FLAC" + version info)
                    // Write native FLAC header "fLaC" + metadata blocks
                    _currentPacketBuffer = new MemoryStream();
                    _currentPacketBuffer.Write(new byte[] { (byte)'f', (byte)'L', (byte)'a', (byte)'C' }, 0, 4);

                    // Copy remaining metadata blocks (skip first 13 bytes of OGG FLAC header)
                    if (page.PayloadData.Length > 13)
                    {
                        _currentPacketBuffer.Write(page.PayloadData, 13, page.PayloadData.Length - 13);
                    }

                    _currentPacketBuffer.Position = 0;
                    _headerWritten = true;
                    continue;
                }
            }

            // Regular FLAC audio packets - just append payload data
            if (page.PayloadData.Length > 0)
            {
                _currentPacketBuffer = new MemoryStream();
                _currentPacketBuffer.Write(page.PayloadData, 0, page.PayloadData.Length);
                _currentPacketBuffer.Position = 0;
            }
        }

        return totalRead;
    }

    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentPacketBuffer?.Dispose();
            _metadataBuffer?.Dispose();
            // Don't dispose _innerStream - let the caller manage it
        }
        base.Dispose(disposing);
    }
}
