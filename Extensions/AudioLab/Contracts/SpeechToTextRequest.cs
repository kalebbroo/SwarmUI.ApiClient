using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for AudioLab speech-to-text transcription.</summary>
public class SpeechToTextRequest
{
    /// <summary>Base64 encoded audio to transcribe. Required, and validated as base64 by the server.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Language hint for transcription.</summary>
    [JsonProperty("language")]
    public string Language { get; set; } = "en-US";

    /// <summary>Provider to route to. When null or empty the server picks the first registered STT provider.</summary>
    [JsonProperty("provider_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProviderId { get; set; }

    /// <summary>Additional transcription options.</summary>
    [JsonProperty("options")]
    public SpeechToTextOptions Options { get; set; } = new();
}

/// <summary>Optional tuning knobs for a speech-to-text request.</summary>
public class SpeechToTextOptions
{
    /// <summary>Whether the response should include a confidence score.</summary>
    [JsonProperty("return_confidence")]
    public bool ReturnConfidence { get; set; } = true;

    /// <summary>Whether the response should include alternative transcriptions.</summary>
    [JsonProperty("return_alternatives")]
    public bool ReturnAlternatives { get; set; }

    /// <summary>Model optimization preference, either "accuracy" or "speed".</summary>
    [JsonProperty("model_preference")]
    public string ModelPreference { get; set; } = "accuracy";
}
