using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Polly;
using Polly.Retry;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Models;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// Internet radio player using NAudio library
/// Implements two-thread architecture: download thread + playback thread
/// Features: MP3/AAC streaming, ICY metadata, buffer management, auto-reconnect
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

            System.Diagnostics.Debug.WriteLine(
                $"[RadioPlayer] Connected to {station.Name}, ICY metadata interval: {metadataInterval}");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (metadataInterval > 0)
            {
                await StreamWithMetadataAsync(stream, metadataInterval, cancellationToken);
            }
            else
            {
                await StreamWithoutMetadataAsync(stream, cancellationToken);
            }
        });
    }

    private async Task StreamWithMetadataAsync(
        Stream stream,
        int metadataInterval,
        CancellationToken cancellationToken)
    {
        var parser = new IcyMetadataParser(metadataInterval);
        var readBuffer = new byte[AppConstants.AudioBuffer.ChunkSize];

        IMp3FrameDecompressor? decompressor = null;
        var decompressBuffer = new byte[AppConstants.AudioBuffer.ChunkSize * 4];

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

            // Process audio data
            if (result.AudioData.Length > 0)
            {
                await ProcessAudioDataAsync(
                    result.AudioData,
                    ref decompressor,
                    decompressBuffer,
                    cancellationToken);
            }
        }

        decompressor?.Dispose();
    }

    private async Task StreamWithoutMetadataAsync(Stream stream, CancellationToken cancellationToken)
    {
        var readBuffer = new byte[AppConstants.AudioBuffer.ChunkSize];
        IMp3FrameDecompressor? decompressor = null;
        var decompressBuffer = new byte[AppConstants.AudioBuffer.ChunkSize * 4];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

            if (bytesRead == 0)
            {
                System.Diagnostics.Debug.WriteLine("[RadioPlayer] Stream ended");
                break;
            }

            await ProcessAudioDataAsync(
                readBuffer,
                ref decompressor,
                decompressBuffer,
                cancellationToken,
                bytesRead);
        }

        decompressor?.Dispose();
    }

    private async Task ProcessAudioDataAsync(
        byte[] audioData,
        ref IMp3FrameDecompressor? decompressor,
        byte[] decompressBuffer,
        CancellationToken cancellationToken,
        int? length = null)
    {
        int dataLength = length ?? audioData.Length;

        try
        {
            // Try to parse MP3 frame
            using var ms = new MemoryStream(audioData, 0, dataLength);

            Mp3Frame? frame;
            while ((frame = Mp3Frame.LoadFromStream(ms)) != null)
            {
                // Initialize decompressor on first frame
                if (decompressor == null)
                {
                    decompressor = new AcmMp3FrameDecompressor(new WaveFormat(
                        frame.SampleRate,
                        frame.ChannelMode == ChannelMode.Mono ? 1 : 2));

                    InitializePlayback(decompressor.OutputFormat);
                }

                // Decompress frame
                int decompressed = decompressor.DecompressFrame(frame, decompressBuffer, 0);

                // Add to buffer
                if (_bufferedWaveProvider != null && decompressed > 0)
                {
                    _bufferedWaveProvider.AddSamples(decompressBuffer, 0, decompressed);

                    // Report progress
                    ReportProgress();

                    // Handle buffering state
                    await HandleBufferingAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RadioPlayer] Audio processing error: {ex.Message}");
            // Continue streaming despite decoding errors
        }
    }

    private void InitializePlayback(WaveFormat waveFormat)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[RadioPlayer] Initializing playback: {waveFormat.SampleRate}Hz, {waveFormat.Channels}ch");

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
