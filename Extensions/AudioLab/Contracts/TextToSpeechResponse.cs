using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for AudioLab text-to-speech synthesis.</summary>
public class TextToSpeechResponse : AudioLabResponse
{
    /// <summary>Base64 encoded synthesized audio.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Text that was synthesized.</summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Voice used for synthesis.</summary>
    [JsonProperty("voice")]
    public string Voice { get; set; } = string.Empty;

    /// <summary>Language used for synthesis.</summary>
    [JsonProperty("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>Volume level applied to the output.</summary>
    [JsonProperty("volume")]
    public float Volume { get; set; }

    /// <summary>Duration of the generated audio in seconds.</summary>
    [JsonProperty("duration")]
    public double Duration { get; set; }

    /// <summary>Session the synthesis ran under.</summary>
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }

    /// <summary>Provider supplied metadata about the synthesis.</summary>
    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
