using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Vorbis;
using Polly;
using Polly.Retry;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Internet radio player using NAudio library
/// Implements two-thread architecture: download thread + playback thread
/// Features: MP3/AAC/OGG streaming, ICY metadata, buffer management, auto-reconnect
/// </summary>
public class NAudioRadioPlayer : IRadioPlayer
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


    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                var oldState = _state;
                _state = value;
                DebugLogger.Log("STATE", $"State changed: {oldState} -> {value}");
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
            if (codec.Contains("OGG") || codec.Contains("VORBIS"))
                return "OGG";
        }

        // Try content-type header
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("mpeg") || contentType.Contains("mp3"))
            return "MP3";
        if (contentType.Contains("aac") || contentType.Contains("mp4"))
            return "AAC";
        if (contentType.Contains("ogg") || contentType.Contains("vorbis"))
            return "OGG";

        // Default to MP3
        return "MP3";
    }

    private async Task StreamWithMetadataAsync(
        Stream stream,
        string codec,
        int metadataInterval,
        CancellationToken cancellationToken)
    {
        // Create IcyStream to filter metadata and provide continuous audio stream
        using var icyStream = new IcyStream(stream, metadataInterval);

        // Forward metadata events
        icyStream.MetadataReceived += (sender, metadata) =>
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Metadata: {metadata.StreamTitle}");
            DebugLogger.Log("METADATA", $"Title: {metadata.StreamTitle}");
            MetadataReceived?.Invoke(this, metadata);
        };

        // For MP3/AAC, wrap with ReadFullyStream for reliable MediaFoundation decoding
        var codecUpper = codec.ToUpperInvariant();
        if (codecUpper == "MP3" || codecUpper == "MPEG" || codecUpper == "AAC" || codecUpper == "MP4" || codecUpper == "AAC+")
        {
            DebugLogger.Log("STREAM", $"Creating continuous {codec} stream with ReadFullyStream wrapper");
            using var readFullyStream = new ReadFullyStream(icyStream);
            await ProcessAudioStreamAsync(readFullyStream, codec, cancellationToken);
        }
        else
        {
            // OGG and others can use IcyStream directly
            await ProcessAudioStreamAsync(icyStream, codec, cancellationToken);
        }
    }

    private async Task StreamWithoutMetadataAsync(
        Stream stream,
        string codec,
        CancellationToken cancellationToken)
    {
        // For streams without metadata, wrap with ReadFullyStream for reliable reading
        // This is especially important for MP3 and AAC with MediaFoundation
        var codecUpper = codec.ToUpperInvariant();

        if (codecUpper == "MP3" || codecUpper == "MPEG" || codecUpper == "AAC" || codecUpper == "MP4" || codecUpper == "AAC+")
        {
            DebugLogger.Log("STREAM", $"Wrapping {codec} stream with ReadFullyStream for reliable reading");
            using var readFullyStream = new ReadFullyStream(stream);
            await ProcessAudioStreamAsync(readFullyStream, codec, cancellationToken);
        }
        else
        {
            // OGG and other formats don't need ReadFullyStream wrapper
            await ProcessAudioStreamAsync(stream, codec, cancellationToken);
        }
    }


    private async Task ProcessAudioStreamAsync(
        Stream audioStream,
        string codec,
        CancellationToken cancellationToken)
    {
        try
        {
            WaveStream? reader = null;

            // Create appropriate reader based on codec
            switch (codec.ToUpperInvariant())
            {
                case "MP3":
                case "MPEG":
                    // Use Media Foundation for MP3 (more stable for streaming than Mp3FileReader)
                    reader = new StreamMediaFoundationReader(audioStream);
                    break;

                case "AAC":
                case "MP4":
                case "AAC+":
                    // Use Media Foundation for AAC (Windows 7+)
                    reader = new StreamMediaFoundationReader(audioStream);
                    break;

                case "OGG":
                case "VORBIS":
                    // Use NAudio.Vorbis for OGG
                    reader = new VorbisWaveReader(audioStream);
                    break;

                default:
                    // Try Media Foundation as fallback (works for most formats)
                    System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Unknown codec '{codec}', trying Media Foundation");
                    reader = new StreamMediaFoundationReader(audioStream);
                    break;
            }

            if (reader != null)
            {
                // Initialize playback on first successful read
                if (_bufferedWaveProvider == null)
                {
                    InitializePlayback(reader.WaveFormat);
                }

                // Read and buffer audio data
                var buffer = new byte[AppConstants.AudioBuffer.ChunkSize];
                int bytesRead;
                int chunkCount = 0;

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
                {
                    if (_bufferedWaveProvider != null)
                    {
                        _bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);
                        chunkCount++;

                        var bufferSeconds = _bufferedWaveProvider.BufferedDuration.TotalSeconds;
                        var bufferPercent = BufferFillPercentage * 100;

                        DebugLogger.Log("BUFFER", $"Chunk {chunkCount}: +{bytesRead} bytes, buffer: {bufferSeconds:F2}s ({bufferPercent:F0}%)");

                        // Report progress
                        ReportProgress();

                        // Handle buffering state
                        await HandleBufferingAsync(cancellationToken);
                    }
                }

                DebugLogger.Log("BUFFER", $"Stream read completed, total chunks: {chunkCount}");
                reader?.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Audio processing error: {ex.Message}");
            // Continue despite errors
        }
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
    }
}
