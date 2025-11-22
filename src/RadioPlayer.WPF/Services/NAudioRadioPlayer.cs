using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NVorbis; // For OGG Vorbis decoding (non-seekable HTTP streams)
using Concentus.Structs; // For Opus decoder (used by OpusStreamDecoder)
using Polly;
using Polly.Retry;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;
using System.Runtime.InteropServices; // Added for P/Invoke

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Internet radio player using NAudio library
/// Implements frame-by-frame MP3 decoding with ACM for reliable streaming
/// Features: MP3/AAC/OGG streaming, ICY metadata, buffer management, auto-reconnect
/// </summary>
public class NAudioRadioPlayer : IRadioPlayer, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;

    private IWavePlayer? _waveOut;
    private BufferedWaveProvider? _bufferedWaveProvider;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _streamingTask;
    private Task? _bufferMonitoringTask;

    private RadioStation? _currentStation;
    private PlaybackState _state = PlaybackState.Stopped;
    private float _volume = AppConstants.UI.DefaultVolume;
    private bool _isMuted;

    // MP3 frame-by-frame decoding with ACM
    private MemoryStream? _mp3InputBuffer;
    private AcmMp3FrameDecompressor? _mp3Decompressor;
    private readonly byte[] _pcmBuffer = new byte[65536];

    // Constant for OGG buffering to prevent decoder starvation
    private const int OggStreamBufferSize = 65536;

    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                var oldState = _state;
                _state = value;
                DebugLogger.Log("STATE", $"State changed: {oldState} → {value}");
                PlaybackStateChanged?.Invoke(this, value);
            }
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_waveOut != null)
            {
                _waveOut.Volume = _isMuted ? 0f : _volume;
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            if (_waveOut != null)
            {
                _waveOut.Volume = _isMuted ? 0f : _volume;
            }
        }
    }

    public RadioStation? CurrentStation => _currentStation;

    public TimeSpan BufferedDuration => _bufferedWaveProvider?.BufferedDuration ?? TimeSpan.Zero;

    public double BufferFillPercentage
    {
        get
        {
            if (_bufferedWaveProvider == null)
                return 0;

            var targetBuffer = AppConstants.AudioBuffer.BufferDuration;
            if (targetBuffer.TotalSeconds == 0)
                return 0;

            return Math.Clamp(_bufferedWaveProvider.BufferedDuration.TotalSeconds / targetBuffer.TotalSeconds, 0, 1);
        }
    }

    public int SampleRate => _bufferedWaveProvider?.WaveFormat?.SampleRate ?? 0;

    public int Channels => _bufferedWaveProvider?.WaveFormat?.Channels ?? 0;

    public event EventHandler<PlaybackState>? PlaybackStateChanged;
    public event EventHandler<IcyMetadata>? MetadataReceived;
    public event EventHandler<StreamProgress>? ProgressUpdated;
    public event EventHandler<Exception>? ErrorOccurred;

    public NAudioRadioPlayer()
        : this(new HttpClient())
    {
    }

    public NAudioRadioPlayer(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = AppConstants.Network.HttpTimeout;

        // Configure Polly retry policy
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: AppConstants.Network.RetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(AppConstants.Network.ExponentialBackoffBase, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[RadioPlayer] Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    DebugLogger.Log("RETRY", $"Attempt {retryCount} after {timeSpan.TotalSeconds}s - {exception.GetType().Name}: {exception.Message}");
                });
    }

    public async Task PlayAsync(RadioStation station, CancellationToken cancellationToken = default)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        DebugLogger.Log("PLAY", $"Starting playback: {station.Name} ({station.Codec})");

        // Stop current playback
        Stop();

        _currentStation = station;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        State = PlaybackState.Connecting;

        // Start streaming on background thread
        _streamingTask = Task.Run(async () =>
        {
            try
            {
                await StreamAudioAsync(station, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                System.Diagnostics.Debug.WriteLine("[RadioPlayer] Streaming cancelled");
                DebugLogger.Log("STREAM", "Streaming cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Streaming error: {ex.Message}");
                DebugLogger.Log("ERROR", $"Streaming error: {ex.GetType().Name} - {ex.Message}");
                State = PlaybackState.Error;
                ErrorOccurred?.Invoke(this, ex);
            }
        }, _cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    public void Stop()
    {
        DebugLogger.Log("STOP", "Stopping playback");

        State = PlaybackState.Stopped;

        // Cancel streaming
        _cancellationTokenSource?.Cancel();

        // Stop playback
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;

        _bufferedWaveProvider = null;
        _currentStation = null;

        // Clean up MP3 decoder
        _mp3Decompressor?.Dispose();
        _mp3Decompressor = null;
        _mp3InputBuffer?.Dispose();
        _mp3InputBuffer = null;

        // Wait for tasks to complete
        try
        {
            _streamingTask?.Wait(TimeSpan.FromSeconds(2));
            _bufferMonitoringTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore timeout
        }

        _streamingTask = null;
        _bufferMonitoringTask = null;
    }

    public void Pause()
    {
        if (_waveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
            _waveOut.Pause();
            State = PlaybackState.Paused;
        }
    }

    public void Resume()
    {
        if (_waveOut?.PlaybackState == NAudio.Wave.PlaybackState.Paused)
        {
            _waveOut.Play();
            State = PlaybackState.Playing;
        }
    }

    private async Task StreamAudioAsync(RadioStation station, CancellationToken cancellationToken)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, station.UrlResolved);

            // Request ICY metadata
            request.Headers.Add("Icy-MetaData", "1");
            request.Headers.Add("User-Agent", AppConstants.HttpHeaders.UserAgent);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            // Get ICY metadata interval
            int metadataInterval = 0;
            if (response.Headers.TryGetValues("icy-metaint", out var metaintValues))
            {
                foreach (var value in metaintValues)
                {
                    if (int.TryParse(value, out metadataInterval))
                        break;
                }
            }

            // Detect codec
            var codec = DetectCodec(station, response);
            System.Diagnostics.Debug.WriteLine(
                $"[RadioPlayer] Connected to {station.Name}, Codec: {codec}, ICY metadata interval: {metadataInterval}");
            DebugLogger.Log("CONNECT", $"Connected - Codec: {codec}, ICY interval: {metadataInterval}");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (metadataInterval > 0)
            {
                await StreamWithMetadataAsync(stream, codec, metadataInterval, cancellationToken);
            }
            else
            {
                await StreamWithoutMetadataAsync(stream, codec, cancellationToken);
            }
        });
    }

    private string DetectCodec(RadioStation station, HttpResponseMessage response)
    {
        // First try station codec
        if (!string.IsNullOrWhiteSpace(station.Codec))
        {
            var codec = station.Codec.ToUpperInvariant();
            if (codec.Contains("MP3") || codec.Contains("MPEG"))
                return "MP3";
            if (codec.Contains("AAC") || codec.Contains("MP4"))
                return "AAC";
            // Distinguish OPUS from Vorbis
            if (codec.Contains("OPUS"))
                return "OPUS";
            if (codec.Contains("OGG") || codec.Contains("VORBIS"))
                return "VORBIS";
            if (codec.Contains("FLAC"))
                return "FLAC";
        }

        // Try content-type header
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("mpeg") || contentType.Contains("mp3"))
            return "MP3";
        if (contentType.Contains("aac") || contentType.Contains("mp4"))
            return "AAC";
        if (contentType.Contains("opus"))
            return "OPUS";
        if (contentType.Contains("ogg") || contentType.Contains("vorbis"))
            return "VORBIS";

        // Default to MP3
        return "MP3";
    }

    private async Task StreamWithMetadataAsync(
        Stream stream,
        string codec,
        int metadataInterval,
        CancellationToken cancellationToken)
    {
        var codecUpper = codec.ToUpperInvariant();

        // OGG codecs (OPUS/Vorbis) require continuous stream - cannot work with chunks
        if (codecUpper == "OPUS" || codecUpper == "VORBIS" || codecUpper == "FLAC")
        {
            await StreamOggWithMetadataAsync(stream, codec, metadataInterval, cancellationToken);
            return;
        }

        // For MP3/AAC: chunk-based processing
        var parser = new IcyMetadataParser(metadataInterval);
        var readBuffer = new byte[AppConstants.AudioBuffer.ChunkSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

            if (bytesRead == 0)
            {
                System.Diagnostics.Debug.WriteLine("[RadioPlayer] Stream ended");
                DebugLogger.Log("STREAM", "Stream ended");
                break;
            }

            // Parse ICY metadata
            var result = parser.ParseStream(readBuffer, 0, bytesRead);

            // Notify metadata
            if (result.Metadata != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RadioPlayer] Metadata: {result.Metadata.StreamTitle}");
                DebugLogger.Log("METADATA", $"Title: {result.Metadata.StreamTitle}");
                MetadataReceived?.Invoke(this, result.Metadata);
            }

            // Process audio data
            if (result.AudioData.Length > 0)
            {
                await ProcessRawAudioDataAsync(result.AudioData, codec, cancellationToken);
            }
        }
    }

    private async Task StreamWithoutMetadataAsync(
        Stream stream,
        string codec,
        CancellationToken cancellationToken)
    {
        var codecUpper = codec.ToUpperInvariant();

        // OGG codecs (OPUS/Vorbis) require continuous stream
        if (codecUpper == "OPUS" || codecUpper == "VORBIS" || codecUpper == "FLAC")
            {
                // FIX: Wrap in BufferedStream for robust OGG/Opus decoding
                using var bufferedStream = new BufferedStream(stream, OggStreamBufferSize);
                await ProcessOggStreamAsync(bufferedStream, codec, cancellationToken);
                return;
            }

        // For MP3/AAC: chunk-based processing
        var buffer = new byte[AppConstants.AudioBuffer.ChunkSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead == 0)
            {
                System.Diagnostics.Debug.WriteLine("[RadioPlayer] Stream ended");
                DebugLogger.Log("STREAM", "Stream ended");
                break;
            }

            var audioChunk = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, audioChunk, 0, bytesRead);

            await ProcessRawAudioDataAsync(audioChunk, codec, cancellationToken);
        }
    }

    private async Task StreamOggWithMetadataAsync(
        Stream stream,
        string codec,
        int metadataInterval,
        CancellationToken cancellationToken)
    {
        DebugLogger.Log("OGG", $"Starting {codec} stream with ICY metadata (interval: {metadataInterval})");

        // Create filtering stream that removes ICY metadata in real-time
        using var icyFilter = new IcyFilterStream(stream, metadataInterval);

        // Forward metadata events
        icyFilter.MetadataReceived += (sender, metadata) =>
        {
            DebugLogger.Log("METADATA", $"{codec} metadata: {metadata.StreamTitle}");
            MetadataReceived?.Invoke(this, metadata);
        };

        // FIX: Wrap the IcyFilterStream in a BufferedStream.
        using var bufferedStream = new BufferedStream(icyFilter, OggStreamBufferSize);

        // Process clean OGG stream
        await ProcessOggStreamAsync(bufferedStream, codec, cancellationToken);
    }

    private async Task ProcessRawAudioDataAsync(
        byte[] audioData,
        string codec,
        CancellationToken cancellationToken)
    {
        try
        {
            var codecUpper = codec.ToUpperInvariant();

            if (codecUpper == "MP3" || codecUpper == "MPEG")
            {
                // Use frame-by-frame decoding with ACM for MP3
                ProcessMp3Frames(audioData);
            }
            else if (codecUpper == "AAC" || codecUpper == "MP4" || codecUpper == "AAC+")
            {
                // Use MediaFoundation for AAC
                await ProcessAacDataAsync(audioData, cancellationToken);
            }
            else
            {
                // Unknown codec - try MP3 as fallback
                ProcessMp3Frames(audioData);
            }

            ReportProgress();
            await HandleBufferingAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Raw audio processing error: {ex.Message}");
            DebugLogger.Log("ERROR", $"Raw audio processing error: {ex.Message}");
        }
    }

    private void ProcessMp3Frames(byte[] newData)
    {
        // Initialize MP3 input buffer if needed
        if (_mp3InputBuffer == null)
        {
            _mp3InputBuffer = new MemoryStream();
            DebugLogger.Log("MP3", "MP3 input buffer initialized");
        }

        // Append new data to buffer
        _mp3InputBuffer.Seek(0, SeekOrigin.End);
        _mp3InputBuffer.Write(newData, 0, newData.Length);
        _mp3InputBuffer.Position = 0;

        // Process frames from buffer
        while (_mp3InputBuffer.Position <= _mp3InputBuffer.Length - 4)
        {
            long startPosition = _mp3InputBuffer.Position;
            Mp3Frame? frame = null;

            try
            {
                frame = Mp3Frame.LoadFromStream(_mp3InputBuffer);
            }
            catch (Exception)
            {
                // Not enough data for complete frame, rewind and wait for more
                _mp3InputBuffer.Position = startPosition;
                break;
            }

            if (frame == null)
            {
                DebugLogger.Log("MP3", "No more frames in buffer");
                break;
            }

            // Initialize decompressor and playback on first frame
            if (_bufferedWaveProvider == null)
            {
                var mp3WaveFormat = new Mp3WaveFormat(
                    frame.SampleRate,
                    frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                    frame.FrameLength,
                    frame.BitRate);

                _mp3Decompressor = new AcmMp3FrameDecompressor(mp3WaveFormat);
                InitializePlayback(_mp3Decompressor.OutputFormat);

                DebugLogger.Log("MP3", $"Initialized MP3 decoder: {frame.SampleRate}Hz, {frame.ChannelMode}, {frame.BitRate} bps");
            }

            // Decompress frame to PCM
            int decodedBytes = _mp3Decompressor!.DecompressFrame(frame, _pcmBuffer, 0);

            if (decodedBytes > 0 && _bufferedWaveProvider != null)
            {
                _bufferedWaveProvider.AddSamples(_pcmBuffer, 0, decodedBytes);
            }
        }

        // Keep remaining data in buffer for next iteration
        long remainingBytes = _mp3InputBuffer.Length - _mp3InputBuffer.Position;
        if (remainingBytes > 0)
        {
            var leftoverData = new byte[remainingBytes];
            _mp3InputBuffer.Read(leftoverData, 0, leftoverData.Length);
            _mp3InputBuffer.SetLength(0);
            _mp3InputBuffer.Write(leftoverData, 0, leftoverData.Length);
        }
        else
        {
            _mp3InputBuffer.SetLength(0);
        }
    }

    private async Task ProcessOggStreamAsync(Stream oggStream, string codec, CancellationToken cancellationToken)
    {
        // Auto-detect actual codec from OGG stream content
        // OGG is just a container - it can contain Opus or Vorbis
        DebugLogger.Log("OGG", $"Detecting actual codec in OGG stream (hint: {codec})...");

        // Wrap stream in PeekableStream to allow detection without consuming data
        using var peekableStream = new PeekableStream(oggStream);

        var detectedCodec = await OggContentDetector.DetectOggCodecAsync(peekableStream, cancellationToken);

        if (detectedCodec == "UNKNOWN")
        {
            // Fallback to provided codec hint
            DebugLogger.Log("OGG", $"Could not detect codec from stream, using hint: {codec}");
            detectedCodec = codec.ToUpperInvariant();
        }
        else
        {
            DebugLogger.Log("OGG", $"Detected codec: {detectedCodec}");
        }

        // Stop peeking - now the stream will replay buffered data then continue from inner stream
        peekableStream.StopPeeking();

        if (detectedCodec == "FLAC")
        {
            await ProcessFlacStreamAsync(peekableStream, cancellationToken);
        }
        else if (detectedCodec == "OPUS")
        {
            await ProcessOpusStreamAsync(peekableStream, cancellationToken);
        }
        else // Vorbis (default)
        {
            await ProcessVorbisStreamAsync(peekableStream, cancellationToken);
        }
    }

    private async Task ProcessOpusStreamAsync(Stream opusStream, CancellationToken cancellationToken)
    {
        try
        {
            DebugLogger.Log("OPUS", "Creating streaming Opus decoder for OGG OPUS stream...");

            using var opusDecoder = new OpusStreamDecoder();

            int chunkCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Decode next packet from OGG stream
                    short[]? packet = await opusDecoder.DecodeNextPacketAsync(opusStream, cancellationToken);

                    if (packet == null)
                    {
                        DebugLogger.Log("OPUS", "End of stream reached");
                        break;
                    }

                    // Initialize playback on first packet (after headers are parsed)
                    if (_bufferedWaveProvider == null && opusDecoder.IsInitialized)
                    {
                        var waveFormat = new WaveFormat(opusDecoder.SampleRate, 16, opusDecoder.Channels);
                        InitializePlayback(waveFormat);
                        DebugLogger.Log("OPUS", $"Initialized OPUS playback: {opusDecoder.SampleRate}Hz, {opusDecoder.Channels}ch, 16-bit PCM");
                    }

                    if (packet.Length > 0 && _bufferedWaveProvider != null)
                    {
                        // Convert short[] to byte[] for BufferedWaveProvider
                        byte[] pcmBuffer = new byte[packet.Length * 2];
                        Buffer.BlockCopy(packet, 0, pcmBuffer, 0, pcmBuffer.Length);

                        _bufferedWaveProvider.AddSamples(pcmBuffer, 0, pcmBuffer.Length);
                        chunkCount++;

                        if (chunkCount % 50 == 0)
                        {
                            var bufferSeconds = _bufferedWaveProvider.BufferedDuration.TotalSeconds;
                            DebugLogger.Log("OPUS", $"Packet {chunkCount}: {packet.Length} samples, buffer: {bufferSeconds:F2}s");
                            ReportProgress();
                        }

                        await HandleBufferingAsync(cancellationToken);
                    }
                }
                catch (Exception packetEx)
                {
                    // If a single packet fails, try to continue
                    DebugLogger.Log("OPUS_WARN", $"Packet decode error: {packetEx.Message}");
                    // Brief delay to allow stream to stabilize if needed
                    await Task.Delay(10, cancellationToken);
                }
            }

            DebugLogger.Log("OPUS", $"OPUS stream processing completed, total packets: {chunkCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] OPUS stream processing error: {ex.Message}");
            DebugLogger.Log("OPUS", $"Processing error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessVorbisStreamAsync(Stream vorbisStream, CancellationToken cancellationToken)
    {
        try
        {
            DebugLogger.Log("VORBIS", "Creating NVorbis VorbisReader...");

            // NVorbis.VorbisReader supports non-seekable streams (HTTP streaming)
            // BufferedStream wrapper ensures headers are read correctly
            using var vorbisReader = new VorbisReader(vorbisStream, closeOnDispose: false);

            // Get audio format info
            var channels = vorbisReader.Channels;
            var sampleRate = vorbisReader.SampleRate;
            DebugLogger.Log("VORBIS", $"Vorbis stream info: {sampleRate}Hz, {channels} channels");

            // Initialize playback with IEEE float format (native NVorbis format)
            // This avoids unnecessary float→int16 conversion and preserves quality
            if (_bufferedWaveProvider == null)
            {
                var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                InitializePlayback(waveFormat);
                DebugLogger.Log("VORBIS", $"Initialized Vorbis playback: {sampleRate}Hz, {channels}ch, IEEE Float");
            }

            // Buffer for reading float samples from NVorbis (approx 100ms worth of audio)
            var floatBuffer = new float[channels * sampleRate / 10];

            // Buffer for byte conversion (4 bytes per float sample)
            var byteBuffer = new byte[floatBuffer.Length * sizeof(float)];

            int chunkCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                int samplesRead = 0;

                try
                {
                    // ReadSamples can throw if the stream has a slight corruption
                    samplesRead = vorbisReader.ReadSamples(floatBuffer, 0, floatBuffer.Length);
                }
                catch (Exception readEx)
                {
                    DebugLogger.Log("VORBIS_WARN", $"Read error (recovering): {readEx.Message}");
                    await Task.Delay(50, cancellationToken);
                    continue;
                }

                if (samplesRead == 0)
                {
                    if (vorbisReader.IsEndOfStream)
                    {
                        DebugLogger.Log("VORBIS", "End of Vorbis stream reached");
                        break;
                    }

                    // If not EOS but 0 read, wait briefly
                    await Task.Delay(20, cancellationToken);
                    continue;
                }

                // Convert float[] to byte[] using Buffer.BlockCopy (no quality loss)
                int bytesToAdd = samplesRead * sizeof(float);
                Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, bytesToAdd);

                if (_bufferedWaveProvider != null && bytesToAdd > 0)
                {
                    _bufferedWaveProvider.AddSamples(byteBuffer, 0, bytesToAdd);
                    chunkCount++;

                    if (chunkCount % 20 == 0) // Log every 20 chunks
                    {
                        ReportProgress();
                    }

                    await HandleBufferingAsync(cancellationToken);
                }
            }

            DebugLogger.Log("VORBIS", $"Vorbis stream processing completed, total chunks: {chunkCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Vorbis stream processing error: {ex.Message}");
            DebugLogger.Log("VORBIS", $"Processing error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessFlacStreamAsync(Stream flacStream, CancellationToken cancellationToken)
    {
        try
        {
            DebugLogger.Log("FLAC", "Creating libFLAC stream decoder for OGG FLAC stream...");

            Action<byte[], int> onPcmData = (pcm, length) =>
            {
                if (_bufferedWaveProvider != null)
                {
                    _bufferedWaveProvider.AddSamples(pcm, 0, length);
                }
            };

            using var decoder = new FlacStreamDecoder(flacStream, onPcmData);

            var initStatus = decoder.Initialize(true); // true for Ogg

            if (initStatus != FLAC__StreamDecoderInitStatus.FLAC__STREAM_DECODER_INIT_STATUS_OK)
            {
                throw new Exception("Failed to initialize FLAC decoder: " + initStatus);
            }

            var sampleRate = decoder.GetSampleRate();
            var channels = decoder.GetChannels();
            var bitsPerSample = decoder.GetBitsPerSample();

            if (_bufferedWaveProvider == null)
            {
                var waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);
                InitializePlayback(waveFormat);
                DebugLogger.Log("FLAC", $"Initialized FLAC playback: {sampleRate}Hz, {channels}ch, {bitsPerSample}-bit PCM");
            }

            int chunkCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (decoder.ProcessSingle())
                {
                    chunkCount++;

                    if (chunkCount % 20 == 0)
                    {
                        ReportProgress();
                    }

                    await HandleBufferingAsync(cancellationToken);
                }
                else
                {
                    var state = decoder.GetState();
                    if (state == FLAC__StreamDecoderState.FLAC__STREAM_DECODER_END_OF_STREAM)
                    {
                        DebugLogger.Log("FLAC", "End of FLAC stream reached");
                        break;
                    }
                    else
                    {
                        throw new Exception("FLAC decoding error: " + state);
                    }
                }
            }

            DebugLogger.Log("FLAC", $"FLAC stream processing completed, total chunks: {chunkCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] FLAC stream processing error: {ex.Message}");
            DebugLogger.Log("FLAC", $"Processing error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessAacDataAsync(byte[] aacData, CancellationToken cancellationToken)
    {
        try
        {
            using var dataStream = new MemoryStream(aacData, false);
            using var reader = new StreamMediaFoundationReader(dataStream);

            if (_bufferedWaveProvider == null)
            {
                InitializePlayback(reader.WaveFormat);
            }

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (_bufferedWaveProvider != null)
                {
                    _bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] AAC processing error: {ex.Message}");
            DebugLogger.Log("AAC", $"Processing error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private void InitializePlayback(WaveFormat waveFormat)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[RadioPlayer] Initializing playback: {waveFormat.SampleRate}Hz, {waveFormat.Channels}ch, {waveFormat.Encoding}");

        DebugLogger.Log("INIT", $"Initializing playback: {waveFormat.SampleRate}Hz, {waveFormat.Channels}ch, {waveFormat.Encoding}");
        DebugLogger.Log("INIT", $"Buffer config: {AppConstants.AudioBuffer.BufferDuration.TotalSeconds}s total, {AppConstants.AudioBuffer.PreBufferDuration.TotalSeconds}s pre-buffer");

        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = AppConstants.AudioBuffer.BufferDuration,
            ReadFully = true, // Return silence when buffer is empty
            DiscardOnBufferOverflow = true // Prevent memory issues
        };

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_bufferedWaveProvider);
        _waveOut.Volume = _isMuted ? 0f : _volume;

        // Start buffer monitoring
        _bufferMonitoringTask = Task.Run(() => MonitorBufferAsync(_cancellationTokenSource!.Token));

        State = PlaybackState.Buffering;
    }

    private async Task HandleBufferingAsync(CancellationToken cancellationToken)
    {
        if (_bufferedWaveProvider == null || _waveOut == null)
            return;

        var bufferedDuration = _bufferedWaveProvider.BufferedDuration;

        // Start playback when pre-buffer is filled
        if (State == PlaybackState.Buffering &&
            bufferedDuration >= AppConstants.AudioBuffer.PreBufferDuration)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[RadioPlayer] Pre-buffer filled ({bufferedDuration.TotalSeconds:F1}s), starting playback");

            DebugLogger.Log("BUFFERING", $"Pre-buffer filled ({bufferedDuration.TotalSeconds:F2}s), starting playback");

            _waveOut.Play();
            State = PlaybackState.Playing;
        }

        await Task.CompletedTask;
    }

    private async Task MonitorBufferAsync(CancellationToken cancellationToken)
    {
        DebugLogger.Log("MONITOR", "Buffer monitoring started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(AppConstants.AudioBuffer.BufferCheckInterval, cancellationToken);

                if (_bufferedWaveProvider == null || _waveOut == null)
                    break;

                var bufferedDuration = _bufferedWaveProvider.BufferedDuration;

                // Check for buffer underrun
                if (_waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    if (bufferedDuration < AppConstants.AudioBuffer.PreBufferDuration)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[RadioPlayer] Buffer underrun ({bufferedDuration.TotalSeconds:F1}s), pausing");

                        DebugLogger.Log("MONITOR", $"!!! BUFFER UNDERRUN !!! ({bufferedDuration.TotalSeconds:F2}s < {AppConstants.AudioBuffer.PreBufferDuration.TotalSeconds:F2}s), pausing playback");

                        _waveOut.Pause();
                        State = PlaybackState.Buffering;
                    }
                }
                else if (State == PlaybackState.Buffering)
                {
                    // Resume when buffer is filled
                    if (bufferedDuration >= AppConstants.AudioBuffer.BufferDuration)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[RadioPlayer] Buffer filled ({bufferedDuration.TotalSeconds:F1}s), resuming");

                        DebugLogger.Log("MONITOR", $"Buffer filled ({bufferedDuration.TotalSeconds:F2}s >= {AppConstants.AudioBuffer.BufferDuration.TotalSeconds:F2}s), resuming playback");

                        _waveOut.Play();
                        State = PlaybackState.Playing;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("MONITOR", "Buffer monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Buffer monitoring error: {ex.Message}");
                DebugLogger.Log("MONITOR", $"Buffer monitoring error: {ex.Message}");
            }
        }

        DebugLogger.Log("MONITOR", "Buffer monitoring stopped");
    }

    private void ReportProgress()
    {
        if (_bufferedWaveProvider == null)
            return;

        var progress = new StreamProgress
        {
            BufferDuration = _bufferedWaveProvider.BufferedDuration,
            IsBuffering = State == PlaybackState.Buffering,
            CurrentBitrate = _currentStation?.Bitrate ?? 0,
            StatusMessage = State.ToString()
        };

        ProgressUpdated?.Invoke(this, progress);
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _httpClient?.Dispose();
        _mp3Decompressor?.Dispose();
        _mp3InputBuffer?.Dispose();
    }

    // Added for OGG FLAC support using libFLAC P/Invoke
    // Note: Requires libflac.dll in the application directory or PATH
    private class FlacStreamDecoder : IDisposable
    {
        private IntPtr _decoder;
        private Stream _stream;
        private Action<byte[], int> _onPcmData;
        private ReadCallback _readDel;
        private WriteCallback _writeDel;
        private MetadataCallback _metadataDel;
        private ErrorCallback _errorDel;
        private GCHandle _clientDataHandle;
        private int _channels;
        private int _sampleRate;
        private int _bitsPerSample;

        public FlacStreamDecoder(Stream stream, Action<byte[], int> onPcmData)
        {
            _stream = stream;
            _onPcmData = onPcmData;
            _decoder = FLAC__stream_decoder_new();
            if (_decoder == IntPtr.Zero)
            {
                throw new Exception("Failed to create FLAC decoder");
            }

            _readDel = ReadCb;
            _writeDel = WriteCb;
            _metadataDel = MetadataCb;
            _errorDel = ErrorCb;

            _clientDataHandle = GCHandle.Alloc(this);
        }

        public FLAC__StreamDecoderInitStatus Initialize(bool isOgg)
        {
            return FLAC__stream_decoder_init_ogg_stream(_decoder, _readDel, null, null, null, null, _writeDel, _metadataDel, _errorDel, GCHandle.ToIntPtr(_clientDataHandle));
        }

        public bool ProcessSingle()
        {
            return FLAC__stream_decoder_process_single(_decoder) != 0;
        }

        public FLAC__StreamDecoderState GetState()
        {
            return FLAC__stream_decoder_get_state(_decoder);
        }

        public int GetChannels()
        {
            return (int)FLAC__stream_decoder_get_channels(_decoder);
        }

        public int GetSampleRate()
        {
            return (int)FLAC__stream_decoder_get_sample_rate(_decoder);
        }

        public int GetBitsPerSample()
        {
            return (int)FLAC__stream_decoder_get_bits_per_sample(_decoder);
        }

        private static FLAC__StreamDecoderReadStatus ReadCb(IntPtr decoder, IntPtr buffer, ref UIntPtr bytes, IntPtr client_data)
        {
            var handle = GCHandle.FromIntPtr(client_data);
            var self = (FlacStreamDecoder)handle.Target;

            var byteArr = new byte[bytes.ToUInt32()];

            var read = self._stream.Read(byteArr, 0, byteArr.Length);

            if (read == 0)
            {
                return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
            }

            Marshal.Copy(byteArr, 0, buffer, read);

            bytes = (UIntPtr)read;

            return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
        }

        private static FLAC__StreamDecoderWriteStatus WriteCb(IntPtr decoder, IntPtr frame, IntPtr[] buffer, IntPtr client_data)
        {
            var handle = GCHandle.FromIntPtr(client_data);
            var self = (FlacStreamDecoder)handle.Target;

            var blockSize = (int)FLAC__stream_decoder_get_blocksize(decoder);
            var channels = self.GetChannels();
            var bitsPerSample = self.GetBitsPerSample();

            var bytePerSample = (bitsPerSample + 7) / 8; // rounded up
            var pcmLength = blockSize * channels * bytePerSample;
            var pcm = new byte[pcmLength];

            int pos = 0;
            for (int s = 0; s < blockSize; s++)
            {
                for (int c = 0; c < channels; c++)
                {
                    int value = Marshal.ReadInt32(buffer[c], s * 4);
                    switch (bitsPerSample)
                    {
                        case 8:
                            pcm[pos++] = (byte)(value >> 24);
                            break;
                        case 16:
                            short v16 = (short)(value >> 16);
                            var b16 = BitConverter.GetBytes(v16);
                            pcm[pos++] = b16[0];
                            pcm[pos++] = b16[1];
                            break;
                        case 24:
                            pcm[pos++] = (byte)((value >> 8) & 0xff);
                            pcm[pos++] = (byte)((value >> 16) & 0xff);
                            pcm[pos++] = (byte)((value >> 24) & 0xff);
                            break;
                        case 32:
                            var b32 = BitConverter.GetBytes(value);
                            pcm[pos++] = b32[0];
                            pcm[pos++] = b32[1];
                            pcm[pos++] = b32[2];
                            pcm[pos++] = b32[3];
                            break;
                        default:
                            throw new NotSupportedException("Unsupported bits per sample: " + bitsPerSample);
                    }
                }
            }

            self._onPcmData(pcm, pos);

            return FLAC__StreamDecoderWriteStatus.FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
        }

        private static void MetadataCb(IntPtr decoder, IntPtr metadata, IntPtr client_data)
        {
            // Can extract more if needed, but we use get_ methods after init
        }

        private static void ErrorCb(IntPtr decoder, FLAC__StreamDecoderErrorStatus status, IntPtr client_data)
        {
            DebugLogger.Log("FLAC_ERROR", "Error: " + status);
        }

        public void Dispose()
        {
            if (_decoder != IntPtr.Zero)
            {
                FLAC__stream_decoder_finish(_decoder);
                FLAC__stream_decoder_delete(_decoder);
                _decoder = IntPtr.Zero;
            }

            _clientDataHandle.Free();
        }
    }

    // P/Invoke definitions for libFLAC
    private const string LibFlacDll = "libflac.dll"; // Adjust if necessary, e.g. "libflac-8.dll"

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FLAC__stream_decoder_new();

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FLAC__stream_decoder_delete(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern FLAC__StreamDecoderInitStatus FLAC__stream_decoder_init_ogg_stream(
        IntPtr decoder,
        ReadCallback read_callback,
        SeekCallback seek_callback,
        TellCallback tell_callback,
        LengthCallback length_callback,
        EofCallback eof_callback,
        WriteCallback write_callback,
        MetadataCallback metadata_callback,
        ErrorCallback error_callback,
        IntPtr client_data);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FLAC__stream_decoder_finish(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FLAC__stream_decoder_process_single(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern FLAC__StreamDecoderState FLAC__stream_decoder_get_state(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FLAC__stream_decoder_get_channels(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FLAC__stream_decoder_get_bits_per_sample(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FLAC__stream_decoder_get_sample_rate(IntPtr decoder);

    [DllImport(LibFlacDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FLAC__stream_decoder_get_blocksize(IntPtr decoder);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FLAC__StreamDecoderReadStatus ReadCallback(IntPtr decoder, IntPtr buffer, ref UIntPtr bytes, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FLAC__StreamDecoderWriteStatus WriteCallback(IntPtr decoder, IntPtr frame, IntPtr[] buffer, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MetadataCallback(IntPtr decoder, IntPtr metadata, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ErrorCallback(IntPtr decoder, FLAC__StreamDecoderErrorStatus status, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FLAC__StreamDecoderSeekStatus SeekCallback(IntPtr decoder, ulong absolute_byte_offset, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FLAC__StreamDecoderTellStatus TellCallback(IntPtr decoder, ref ulong absolute_byte_offset, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FLAC__StreamDecoderLengthStatus LengthCallback(IntPtr decoder, ref ulong stream_length, IntPtr client_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EofCallback(IntPtr decoder, IntPtr client_data);

    private enum FLAC__StreamDecoderInitStatus
    {
        FLAC__STREAM_DECODER_INIT_STATUS_OK,
        FLAC__STREAM_DECODER_INIT_STATUS_UNSUPPORTED_CONTAINER,
        FLAC__STREAM_DECODER_INIT_STATUS_INVALID_CALLBACKS,
        FLAC__STREAM_DECODER_INIT_STATUS_MEMORY_ALLOCATION_ERROR,
        FLAC__STREAM_DECODER_INIT_STATUS_ERROR_OPENING_FILE,
        FLAC__STREAM_DECODER_INIT_STATUS_ALREADY_INITIALIZED
    }

    private enum FLAC__StreamDecoderState
    {
        FLAC__STREAM_DECODER_SEARCH_FOR_METADATA,
        FLAC__STREAM_DECODER_READ_METADATA,
        FLAC__STREAM_DECODER_SEARCH_FOR_FRAME_SYNC,
        FLAC__STREAM_DECODER_READ_FRAME,
        FLAC__STREAM_DECODER_END_OF_STREAM,
        FLAC__STREAM_DECODER_OGG_ERROR,
        FLAC__STREAM_DECODER_SEEK_ERROR,
        FLAC__STREAM_DECODER_ABORTED,
        FLAC__STREAM_DECODER_MEMORY_ALLOCATION_ERROR,
        FLAC__STREAM_DECODER_UNINITIALIZED
    }

    private enum FLAC__StreamDecoderReadStatus
    {
        FLAC__STREAM_DECODER_READ_STATUS_CONTINUE,
        FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM,
        FLAC__STREAM_DECODER_READ_STATUS_ABORT
    }

    private enum FLAC__StreamDecoderWriteStatus
    {
        FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE,
        FLAC__STREAM_DECODER_WRITE_STATUS_ABORT
    }

    private enum FLAC__StreamDecoderErrorStatus
    {
        FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC,
        FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER,
        FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH,
        FLAC__STREAM_DECODER_ERROR_STATUS_UNPARSEABLE_STREAM
    }

    private enum FLAC__StreamDecoderSeekStatus
    {
        FLAC__STREAM_DECODER_SEEK_STATUS_OK,
        FLAC__STREAM_DECODER_SEEK_STATUS_ERROR,
        FLAC__STREAM_DECODER_SEEK_STATUS_UNSUPPORTED
    }

    private enum FLAC__StreamDecoderTellStatus
    {
        FLAC__STREAM_DECODER_TELL_STATUS_OK,
        FLAC__STREAM_DECODER_TELL_STATUS_ERROR,
        FLAC__STREAM_DECODER_TELL_STATUS_UNSUPPORTED
    }

    private enum FLAC__StreamDecoderLengthStatus
    {
        FLAC__STREAM_DECODER_LENGTH_STATUS_OK,
        FLAC__STREAM_DECODER_LENGTH_STATUS_ERROR,
        FLAC__STREAM_DECODER_LENGTH_STATUS_UNSUPPORTED
    }

}
