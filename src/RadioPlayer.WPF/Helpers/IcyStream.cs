using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Stream wrapper that filters out ICY metadata from audio stream
/// Provides continuous audio data to decoders while parsing metadata in the background
/// Based on NAudio best practices for streaming with ICY metadata
/// </summary>
public class IcyStream : Stream
{
    private readonly Stream _sourceStream;
    private readonly int _metadataInterval;
    private readonly IcyMetadataParser _parser;
    private int _bytesUntilMetadata;
    private byte[]? _audioBuffer;
    private int _audioBufferPosition;
    private int _audioBufferLength;

    public event EventHandler<IcyMetadata>? MetadataReceived;

    public IcyStream(Stream sourceStream, int metadataInterval)
    {
        _sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
        _metadataInterval = metadataInterval;
        _parser = new IcyMetadataParser(metadataInterval);
        _bytesUntilMetadata = metadataInterval;
        _audioBuffer = new byte[16384]; // 16KB buffer for audio data
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            // First, use any buffered audio data
            if (_audioBufferLength > _audioBufferPosition)
            {
                int availableBytes = _audioBufferLength - _audioBufferPosition;
                int bytesToCopy = Math.Min(availableBytes, count - totalBytesRead);

                Array.Copy(_audioBuffer!, _audioBufferPosition, buffer, offset + totalBytesRead, bytesToCopy);
                _audioBufferPosition += bytesToCopy;
                totalBytesRead += bytesToCopy;
                continue;
            }

            // Need to read more data from source
            _audioBufferPosition = 0;
            _audioBufferLength = 0;

            // Read chunk from source stream
            byte[] readBuffer = new byte[8192]; // 8KB read chunks
            int bytesRead = _sourceStream.Read(readBuffer, 0, readBuffer.Length);

            if (bytesRead == 0)
            {
                // End of stream
                break;
            }

            // Parse the chunk to separate audio and metadata
            var result = _parser.ParseStream(readBuffer, 0, bytesRead);

            // Store audio data in buffer
            if (result.AudioData.Length > 0)
            {
                if (_audioBuffer!.Length < result.AudioData.Length)
                {
                    Array.Resize(ref _audioBuffer, result.AudioData.Length);
                }

                Array.Copy(result.AudioData, 0, _audioBuffer, 0, result.AudioData.Length);
                _audioBufferLength = result.AudioData.Length;
                _audioBufferPosition = 0;
            }

            // Raise metadata event if present
            if (result.Metadata != null)
            {
                MetadataReceived?.Invoke(this, result.Metadata);
            }
        }

        return totalBytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // For now, use synchronous read. Could be optimized later.
        return Read(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sourceStream?.Dispose();
        }
        base.Dispose(disposing);
    }
}
