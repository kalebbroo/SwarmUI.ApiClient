using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for audio format conversion.</summary>
public class AudioFormatConversionResponse : AudioLabResponse
{
    /// <summary>Base64 encoded converted audio.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Format of the converted audio.</summary>
    [JsonProperty("format")]
    public string Format { get; set; } = string.Empty;

    /// <summary>MIME type matching <see cref="Format"/>.</summary>
    [JsonProperty("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Size of the converted audio in bytes.</summary>
    [JsonProperty("size")]
    public long Size { get; set; }
}
