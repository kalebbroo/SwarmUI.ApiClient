using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for converting audio between container formats.</summary>
/// <remarks>Requires ffmpeg on the SwarmUI host. The server accepts mp3, ogg, flac, wav, aac, and m4a.</remarks>
public class AudioFormatConversionRequest
{
    /// <summary>Base64 encoded source audio. Required.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Target format. One of mp3, ogg, flac, wav, aac, or m4a.</summary>
    [JsonProperty("format")]
    public string Format { get; set; } = "mp3";
}
