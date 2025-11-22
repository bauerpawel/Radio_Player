using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Concentus.Structs;

namespace RadioPlayer.WPF.Helpers;

/// <summary>
/// Opus decoder for streaming OGG/Opus HTTP streams
/// Uses custom OGG parser to handle non-seekable network streams
/// </summary>
public class OpusStreamDecoder : IDisposable
{
    private readonly OggStreamParser _oggParser;
    private OpusDecoder? _decoder;
    private int _sampleRate = 48000; // Opus standard
    private int _channels = 2; // Stereo by default
    private bool _headersParsed = false;
    private byte[] _packetBuffer = new byte[8192];

    public int SampleRate => _sampleRate;
    public int Channels => _channels;
    public bool IsInitialized => _headersParsed && _decoder != null;

    public OpusStreamDecoder()
    {
        _oggParser = new OggStreamParser();
    }

    /// <summary>
    /// Reads and decodes the next Opus packet from the stream
    /// Returns PCM samples as short[] or null if no more data
    /// </summary>
    public async Task<short[]?> DecodeNextPacketAsync(Stream stream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var page = await _oggParser.ReadNextPageAsync(stream, cancellationToken);
            if (page == null)
                return null; // End of stream

            // Process OGG page
            if (!_headersParsed)
            {
                // First pages are headers (OpusHead, OpusTags)
                if (page.IsBeginningOfStream)
                {
                    ParseOpusHeaders(page.PayloadData);
                    continue; // Skip to next page
                }

                // Second page is typically OpusTags (comments), skip it
                if (!_headersParsed && page.PayloadData.Length > 8)
                {
                    var magic = Encoding.ASCII.GetString(page.PayloadData, 0, Math.Min(8, page.PayloadData.Length));
                    if (magic.StartsWith("OpusTags"))
                    {
                        _headersParsed = true;
                        DebugLogger.Log("OPUS", $"Headers parsed: {_sampleRate}Hz, {_channels}ch");
                        continue;
                    }
                }

                _headersParsed = true;
            }

            // Decode audio packet
            if (page.PayloadData.Length > 0 && _decoder != null)
            {
                try
                {
                    // Decode Opus packet to PCM
                    int frameSize = OpusPacketInfo.GetNumSamples(page.PayloadData, 0, page.PayloadData.Length, _sampleRate);
                    var pcmOutput = new short[frameSize * _channels];

                    int samplesDecoded = _decoder.Decode(page.PayloadData, 0, page.PayloadData.Length, pcmOutput, 0, frameSize, false);

                    if (samplesDecoded > 0)
                    {
                        // Return actual decoded samples
                        var result = new short[samplesDecoded * _channels];
                        Array.Copy(pcmOutput, 0, result, 0, result.Length);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("OPUS_WARN", $"Packet decode error: {ex.Message}");
                    // Continue to next packet on error
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses Opus headers from the first OGG page
    /// </summary>
    private void ParseOpusHeaders(byte[] headerData)
    {
        // OpusHead structure:
        // 0-7: "OpusHead" magic signature
        // 8: Version (should be 1)
        // 9: Channel count
        // 10-11: Pre-skip (little endian)
        // 12-15: Input sample rate (little endian) - informational only
        // 16-17: Output gain (little endian)
        // 18: Channel mapping family

        if (headerData.Length < 19)
        {
            DebugLogger.Log("OPUS_WARN", "Invalid OpusHead header size");
            return;
        }

        var magic = Encoding.ASCII.GetString(headerData, 0, 8);
        if (magic != "OpusHead")
        {
            DebugLogger.Log("OPUS_WARN", $"Invalid OpusHead magic: {magic}");
            return;
        }

        byte version = headerData[8];
        _channels = headerData[9];
        ushort preSkip = BitConverter.ToUInt16(headerData, 10);
        uint inputSampleRate = BitConverter.ToUInt32(headerData, 12);

        DebugLogger.Log("OPUS", $"OpusHead: version={version}, channels={_channels}, preSkip={preSkip}, inputRate={inputSampleRate}");

        // Opus always decodes to 48kHz (can be resampled later if needed)
        _sampleRate = 48000;

        // Initialize Opus decoder
        #pragma warning disable CS0618
        _decoder = new OpusDecoder(_sampleRate, _channels);
        #pragma warning restore CS0618

        DebugLogger.Log("OPUS", $"Opus decoder initialized: {_sampleRate}Hz, {_channels}ch");
    }

    public void Dispose()
    {
        _decoder = null;
    }
}
