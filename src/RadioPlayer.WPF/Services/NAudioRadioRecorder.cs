using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using RadioPlayer.WPF.Models;
using RadioPlayer.WPF.Helpers;

namespace RadioPlayer.WPF.Services;

/// <summary>
/// NAudio-based implementation of radio stream recorder
/// </summary>
public class NAudioRadioRecorder : IRadioRecorder
{
    private readonly IRadioPlayer _radioPlayer;

    private bool _isRecording;
    private WaveFileWriter? _waveWriter;
    private Stopwatch? _recordingStopwatch;
    private RadioStation? _currentStation;
    private IcyMetadata? _latestMetadata;
    private string? _currentOutputPath;  // WAV file path (temporary or final)
    private string? _finalOutputPath;    // Final output path (MP3 or WAV)
    private RecordingFormat _currentFormat;
    private bool _disposed;

    public bool IsRecording => _isRecording;

    public TimeSpan RecordingDuration => _recordingStopwatch?.Elapsed ?? TimeSpan.Zero;

    public string? CurrentRecordingPath => _currentOutputPath;

    public event EventHandler<string>? RecordingStarted;
    public event EventHandler<RecordingResult>? RecordingStopped;
    public event EventHandler<Exception>? RecordingError;

    public NAudioRadioRecorder(IRadioPlayer radioPlayer)
    {
        _radioPlayer = radioPlayer ?? throw new ArgumentNullException(nameof(radioPlayer));
    }

    public async Task StartRecordingAsync(RadioStation station, string outputPath, RecordingFormat format)
    {
        if (_isRecording)
            throw new InvalidOperationException("Recording is already in progress");

        if (_radioPlayer.State != PlaybackState.Playing)
            throw new InvalidOperationException("Cannot start recording when not playing");

        try
        {
            _currentStation = station;
            _currentFormat = format;
            _finalOutputPath = outputPath;  // Save final output path

            // For MP3 format, record to temporary WAV file first
            if (format == RecordingFormat.Mp3)
            {
                // Create temp WAV file in same directory as final MP3
                var directory = Path.GetDirectoryName(outputPath);
                var tempFileName = Path.GetFileNameWithoutExtension(outputPath) + ".tmp.wav";
                _currentOutputPath = Path.Combine(directory ?? "", tempFileName);
            }
            else
            {
                _currentOutputPath = outputPath;
            }

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(_currentOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Subscribe to PCM data events
            _radioPlayer.PcmDataAvailable += OnPcmDataAvailable;

            // Initialize wave file writer (will be created when first data arrives)
            _recordingStopwatch = Stopwatch.StartNew();
            _isRecording = true;

            DebugLogger.Log("RECORDER", $"Recording started: {station.Name} -> {_currentOutputPath}");
            RecordingStarted?.Invoke(this, _currentOutputPath);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            DebugLogger.Log("RECORDER", $"Failed to start recording: {ex.Message}");
            _isRecording = false;
            RecordingError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task<RecordingResult> StopRecordingAsync()
    {
        if (!_isRecording)
            throw new InvalidOperationException("No recording in progress");

        try
        {
            _isRecording = false;
            _recordingStopwatch?.Stop();

            // Unsubscribe from events
            _radioPlayer.PcmDataAvailable -= OnPcmDataAvailable;

            // Close wave writer
            _waveWriter?.Flush();
            _waveWriter?.Dispose();
            _waveWriter = null;

            var duration = _recordingStopwatch?.Elapsed ?? TimeSpan.Zero;
            var wavFilePath = _currentOutputPath!;
            var finalFilePath = wavFilePath;
            long fileSize = 0;
            bool conversionSuccessful = true;

            if (File.Exists(wavFilePath))
            {
                fileSize = new FileInfo(wavFilePath).Length;

                // Convert to MP3 if requested
                if (_currentFormat == RecordingFormat.Mp3)
                {
                    try
                    {
                        finalFilePath = await ConvertWavToMp3Async(wavFilePath);
                        fileSize = new FileInfo(finalFilePath).Length;

                        // Delete temporary WAV file
                        File.Delete(wavFilePath);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log("RECORDER", $"MP3 conversion failed: {ex.Message}");
                        conversionSuccessful = false;
                        finalFilePath = wavFilePath; // Keep WAV file
                    }
                }
            }

            var result = new RecordingResult
            {
                FilePath = finalFilePath,
                Duration = duration,
                FileSizeBytes = fileSize,
                ConversionSuccessful = conversionSuccessful,
                Station = _currentStation,
                Metadata = _latestMetadata
            };

            DebugLogger.Log("RECORDER", $"Recording stopped: {duration.TotalSeconds:F1}s, {fileSize / 1024 / 1024:F2} MB");
            RecordingStopped?.Invoke(this, result);

            return result;
        }
        catch (Exception ex)
        {
            DebugLogger.Log("RECORDER", $"Failed to stop recording: {ex.Message}");
            RecordingError?.Invoke(this, ex);
            throw;
        }
        finally
        {
            _currentStation = null;
            _latestMetadata = null;
            _currentOutputPath = null;
            _finalOutputPath = null;
        }
    }

    public void UpdateMetadata(IcyMetadata metadata)
    {
        _latestMetadata = metadata;
    }

    private void OnPcmDataAvailable(object? sender, PcmDataEventArgs e)
    {
        if (!_isRecording)
            return;

        try
        {
            // Initialize wave writer on first data
            if (_waveWriter == null)
            {
                _waveWriter = new WaveFileWriter(_currentOutputPath!, e.WaveFormat);
                DebugLogger.Log("RECORDER", $"Wave writer initialized: {e.WaveFormat.SampleRate}Hz, {e.WaveFormat.Channels}ch, {e.WaveFormat.BitsPerSample}-bit");
            }

            // Write PCM data to file
            _waveWriter.Write(e.Data, e.Offset, e.Count);
        }
        catch (Exception ex)
        {
            DebugLogger.Log("RECORDER", $"Error writing PCM data: {ex.Message}");
            RecordingError?.Invoke(this, ex);
        }
    }

    private async Task<string> ConvertWavToMp3Async(string wavFilePath)
    {
        // Use the final output path that was specified by the user
        var mp3FilePath = _finalOutputPath!;

        await Task.Run(() =>
        {
            DebugLogger.Log("RECORDER", $"Converting WAV to MP3: {wavFilePath} -> {mp3FilePath}");

            using var reader = new WaveFileReader(wavFilePath);

            // Configure MediaFoundation encoder for MP3
            MediaFoundationApi.Startup();

            try
            {
                // Use MediaFoundation to encode to MP3 (128 kbps CBR)
                MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath, 128000);

                DebugLogger.Log("RECORDER", $"MP3 conversion successful: {mp3FilePath}");
            }
            finally
            {
                MediaFoundationApi.Shutdown();
            }
        });

        return mp3FilePath;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isRecording)
        {
            try
            {
                StopRecordingAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        _waveWriter?.Dispose();
        _disposed = true;
    }
}
