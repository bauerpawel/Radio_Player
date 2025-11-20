using System;
using System.IO;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Helper class for reading from a stream that may not return all requested bytes in a single Read call
/// This is essential for network streams and MP3 streaming to ensure complete frame reading
/// Based on NAudio's Mp3StreamingDemo implementation
/// </summary>
public class ReadFullyStream : Stream
{
    private readonly Stream sourceStream;
    private long pos; // pseudo-position
    private readonly byte[] readAheadBuffer;
    private int readAheadLength;
    private int readAheadOffset;

    public ReadFullyStream(Stream sourceStream)
    {
        this.sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
        readAheadBuffer = new byte[4096];
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override void Flush()
    {
        throw new InvalidOperationException("This stream does not support flushing");
    }

    public override long Length => pos;

    public override long Position
    {
        get => pos;
        set => throw new InvalidOperationException("This stream does not support seeking");
    }

    /// <summary>
    /// Reads from the stream, ensuring that the full requested number of bytes are read
    /// unless the end of stream is reached
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = 0;
        while (bytesRead < count)
        {
            int readAheadAvailableBytes = readAheadLength - readAheadOffset;
            int bytesRequired = count - bytesRead;

            if (readAheadAvailableBytes > 0)
            {
                int toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
                Array.Copy(readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
                bytesRead += toCopy;
                readAheadOffset += toCopy;
            }
            else
            {
                readAheadOffset = 0;
                readAheadLength = sourceStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                if (readAheadLength == 0)
                {
                    // End of stream reached
                    break;
                }
            }
        }

        pos += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new InvalidOperationException("This stream does not support seeking");
    }

    public override void SetLength(long value)
    {
        throw new InvalidOperationException("This stream does not support setting length");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("This stream does not support writing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            sourceStream?.Dispose();
        }
        base.Dispose(disposing);
    }
}
