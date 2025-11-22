using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Wrapper stream that allows peeking at data without consuming it
/// Buffers read data so it can be re-read later
/// </summary>
public class PeekableStream : Stream
{
    private readonly Stream _innerStream;
    private readonly MemoryStream _peekBuffer;
    private long _position = 0;
    private bool _peeking = true;

    public PeekableStream(Stream innerStream)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _peekBuffer = new MemoryStream();
    }

    /// <summary>
    /// Stops buffering and starts reading from the inner stream
    /// Previously peeked data will be replayed first
    /// </summary>
    public void StopPeeking()
    {
        _peeking = false;
        _position = 0; // Reset to replay buffered data
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // First, replay any buffered data
        if (_position < _peekBuffer.Length)
        {
            _peekBuffer.Position = _position;
            int bufferedBytesRead = await _peekBuffer.ReadAsync(buffer, offset, count, cancellationToken);
            _position += bufferedBytesRead;
            return bufferedBytesRead;
        }

        // Then read from inner stream
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        // If still peeking, buffer the data
        if (_peeking && bytesRead > 0)
        {
            _peekBuffer.Position = _peekBuffer.Length;
            await _peekBuffer.WriteAsync(buffer, offset, bytesRead, cancellationToken);
        }

        _position += bytesRead;
        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _peekBuffer?.Dispose();
            // Don't dispose _innerStream - let the caller manage it
        }
        base.Dispose(disposing);
    }
}
