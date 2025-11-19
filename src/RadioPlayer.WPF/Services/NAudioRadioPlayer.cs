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

    // Data accumulators for streaming
    private MemoryStream? _mp3Accumulator;
    private MemoryStream? _oggAccumulator;
    private MemoryStream? _aacAccumulator;
    private const int MinimumMp3DataSize = 131072; // 128KB minimum before processing MP3
    private const int MinimumOggDataSize = 65536; // 64KB minimum before processing
    private const int MinimumAacDataSize = 32768; // 32KB minimum before processing

    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
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
                });
    }

    public async Task PlayAsync(RadioStation station, CancellationToken cancellationToken = default)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Streaming error: {ex.Message}");
                State = PlaybackState.Error;
                ErrorOccurred?.Invoke(this, ex);
            }
        }, _cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    public void Stop()
    {
        State = PlaybackState.Stopped;

        // Cancel streaming
        _cancellationTokenSource?.Cancel();

        // Stop playback
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;

        _bufferedWaveProvider = null;
        _currentStation = null;

        // Clear accumulators
        _mp3Accumulator?.Dispose();
        _mp3Accumulator = null;
        _oggAccumulator?.Dispose();
        _oggAccumulator = null;
        _aacAccumulator?.Dispose();
        _aacAccumulator = null;

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
        var parser = new IcyMetadataParser(metadataInterval);
        var readBuffer = new byte[AppConstants.AudioBuffer.ChunkSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

            if (bytesRead == 0)
            {
                System.Diagnostics.Debug.WriteLine("[RadioPlayer] Stream ended");
                break;
            }

            // Parse ICY metadata
            var result = parser.ParseStream(readBuffer, 0, bytesRead);

            // Notify metadata
            if (result.Metadata != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RadioPlayer] Metadata: {result.Metadata.StreamTitle}");
                MetadataReceived?.Invoke(this, result.Metadata);
            }

            // Process audio data directly (no intermediate buffer for MP3)
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
        await ProcessAudioStreamAsync(stream, codec, cancellationToken);
    }

    private async Task ProcessRawAudioDataAsync(
        byte[] audioData,
        string codec,
        CancellationToken cancellationToken)
    {
        try
        {
            var codecUpper = codec.ToUpperInvariant();

            // All codecs use accumulation approach
            if (codecUpper == "MP3" || codecUpper == "MPEG")
            {
                await ProcessMp3AccumulatedDataAsync(audioData, cancellationToken);
            }
            else if (codecUpper == "OGG" || codecUpper == "VORBIS")
            {
                await ProcessOggAccumulatedDataAsync(audioData, cancellationToken);
            }
            else if (codecUpper == "AAC" || codecUpper == "MP4" || codecUpper == "AAC+")
            {
                await ProcessAacAccumulatedDataAsync(audioData, cancellationToken);
            }
            else
            {
                // Unknown codec - try MP3 as fallback
                await ProcessMp3AccumulatedDataAsync(audioData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Raw audio processing error: {ex.Message}");
        }
    }

    private async Task ProcessMp3AccumulatedDataAsync(byte[] mp3Data, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize accumulator if needed
            if (_mp3Accumulator == null)
            {
                _mp3Accumulator = new MemoryStream();
            }

            // Add new data to accumulator
            await _mp3Accumulator.WriteAsync(mp3Data, 0, mp3Data.Length, cancellationToken);

            // Process when we have enough data (128KB for smooth playback)
            if (_mp3Accumulator.Length >= MinimumMp3DataSize)
            {
                _mp3Accumulator.Position = 0;

                // Use MediaFoundationReader for MP3 (more stable than Mp3FileReader for streaming)
                try
                {
                    using var reader = new StreamMediaFoundationReader(_mp3Accumulator);

                    // Initialize playback on first successful read
                    if (_bufferedWaveProvider == null)
                    {
                        InitializePlayback(reader.WaveFormat);
                    }

                    // Read and buffer audio data
                    var buffer = new byte[AppConstants.AudioBuffer.ChunkSize];
                    int bytesRead;

                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        if (_bufferedWaveProvider != null)
                        {
                            _bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);
                        }
                    }

                    ReportProgress();
                    await HandleBufferingAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RadioPlayer] MP3 MediaFoundation error: {ex.Message}");
                }

                // Clear accumulator for next batch
                _mp3Accumulator.SetLength(0);
                _mp3Accumulator.Position = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] MP3 accumulation error: {ex.Message}");
            // Reset accumulator on error
            _mp3Accumulator?.Dispose();
            _mp3Accumulator = null;
        }
    }

    private async Task ProcessOggAccumulatedDataAsync(byte[] oggData, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize accumulator if needed
            if (_oggAccumulator == null)
            {
                _oggAccumulator = new MemoryStream();
            }

            // Add new data to accumulator
            await _oggAccumulator.WriteAsync(oggData, 0, oggData.Length, cancellationToken);

            // Process when we have enough data
            if (_oggAccumulator.Length >= MinimumOggDataSize)
            {
                _oggAccumulator.Position = 0;
                await ProcessAudioStreamAsync(_oggAccumulator, "OGG", cancellationToken);

                // Clear accumulator for next batch
                _oggAccumulator.SetLength(0);
                _oggAccumulator.Position = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] OGG accumulation error: {ex.Message}");
            // Reset accumulator on error
            _oggAccumulator?.Dispose();
            _oggAccumulator = null;
        }
    }

    private async Task ProcessAacAccumulatedDataAsync(byte[] aacData, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize accumulator if needed
            if (_aacAccumulator == null)
            {
                _aacAccumulator = new MemoryStream();
            }

            // Add new data to accumulator
            await _aacAccumulator.WriteAsync(aacData, 0, aacData.Length, cancellationToken);

            // Process when we have enough data
            if (_aacAccumulator.Length >= MinimumAacDataSize)
            {
                _aacAccumulator.Position = 0;
                await ProcessAudioStreamAsync(_aacAccumulator, "AAC", cancellationToken);

                // Clear accumulator for next batch
                _aacAccumulator.SetLength(0);
                _aacAccumulator.Position = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] AAC accumulation error: {ex.Message}");
            // Reset accumulator on error
            _aacAccumulator?.Dispose();
            _aacAccumulator = null;
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
                    reader = new Mp3FileReader(audioStream);
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
                    // Try MP3 as fallback
                    System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Unknown codec '{codec}', trying MP3");
                    reader = new Mp3FileReader(audioStream);
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

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
                {
                    if (_bufferedWaveProvider != null)
                    {
                        _bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);

                        // Report progress
                        ReportProgress();

                        // Handle buffering state
                        await HandleBufferingAsync(cancellationToken);
                    }
                }

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

            _waveOut.Play();
            State = PlaybackState.Playing;
        }

        await Task.CompletedTask;
    }

    private async Task MonitorBufferAsync(CancellationToken cancellationToken)
    {
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

                        _waveOut.Play();
                        State = PlaybackState.Playing;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Buffer monitoring error: {ex.Message}");
            }
        }
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
