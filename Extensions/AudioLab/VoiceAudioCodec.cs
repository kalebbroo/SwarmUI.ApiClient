using System;

namespace SwarmUI.ApiClient.Extensions.AudioLab;

/// <summary>Mono PCM16 little-endian &lt;-&gt; float32 ([-1, 1]) conversion, the wire format both directions of
/// <c>AudioLabVoiceSession</c> use for raw audio frames.</summary>
/// <remarks>Mirrors the server's own <c>Pcm16</c> (SwarmUI-AudioLab's <c>AudioServices/Voice/VoiceAudioCodec.cs</c>)
/// byte for byte, including its encode/decode asymmetry (divide by 32768 decoding, multiply by 32767 encoding --
/// the standard signed-16-bit convention), so round-tripping through this class and the server agree exactly.</remarks>
internal static class Pcm16
{
    /// <summary>Decodes little-endian PCM16 bytes to float samples in [-1, 1).</summary>
    public static float[] Decode(ReadOnlySpan<byte> bytes)
    {
        int count = bytes.Length / 2;
        float[] result = new float[count];
        for (int i = 0; i < count; i++)
        {
            short sample = (short)(bytes[i * 2] | (bytes[(i * 2) + 1] << 8));
            result[i] = sample / 32768f;
        }
        return result;
    }

    /// <summary>Encodes float samples (clamped to [-1, 1]) to little-endian PCM16 bytes.</summary>
    public static byte[] Encode(ReadOnlySpan<float> samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)Math.Clamp(MathF.Round(samples[i] * 32767f), short.MinValue, short.MaxValue);
            result[i * 2] = (byte)(sample & 0xFF);
            result[(i * 2) + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return result;
    }
}

/// <summary>The outbound reply-audio frame <c>AudioLabVoiceSession</c> sends: a 4-byte little-endian turn id,
/// then PCM16 mono at the session's outbound rate (24 kHz, Kokoro's own rate -- the server never resamples
/// outbound audio). Decode-only here: the client never produces this frame shape, only the server does.</summary>
internal static class VoiceOutboundFrame
{
    public const int HeaderBytes = 4;

    public static (int TurnId, float[] Samples) Decode(ReadOnlySpan<byte> frame)
    {
        int turnId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(frame);
        return (turnId, Pcm16.Decode(frame[HeaderBytes..]));
    }
}
