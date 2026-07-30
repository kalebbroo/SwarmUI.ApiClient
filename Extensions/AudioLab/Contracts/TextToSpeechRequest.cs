using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for AudioLab text-to-speech synthesis.</summary>
/// <remarks>Server side validation rejects text longer than 1000 characters, volume outside 0.0-1.0, speed or pitch outside 0.1-3.0, and formats other than wav or mp3.</remarks>
public class TextToSpeechRequest
{
    /// <summary>Text to synthesize. Required, and limited to 1000 characters by the server.</summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Voice identifier for the target provider. Leave at "default" to use the provider default.</summary>
    [JsonProperty("voice")]
    public string Voice { get; set; } = "default";

    /// <summary>Language code for synthesis.</summary>
    [JsonProperty("language")]
    public string Language { get; set; } = "en-US";

    /// <summary>Output volume multiplier between 0.0 and 1.0.</summary>
    [JsonProperty("volume")]
    public float Volume { get; set; } = 0.8f;

    /// <summary>Provider to route to. When null or empty the server picks the first registered TTS provider.</summary>
    [JsonProperty("provider_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProviderId { get; set; }

    /// <summary>Base64 encoded WAV reference clip for zero-shot voice cloning providers such as F5 or VibeVoice.</summary>
    [JsonProperty("reference_audio", NullValueHandling = NullValueHandling.Ignore)]
    public string? ReferenceAudio { get; set; }

    /// <summary>Transcript of <see cref="ReferenceAudio"/>. Optional, but improves cloning quality.</summary>
    [JsonProperty("ref_text", NullValueHandling = NullValueHandling.Ignore)]
    public string? ReferenceText { get; set; }

    /// <summary>Additional synthesis options.</summary>
    [JsonProperty("options")]
    public TextToSpeechOptions Options { get; set; } = new();
}

/// <summary>Optional tuning knobs for a text-to-speech request.</summary>
public class TextToSpeechOptions
{
    /// <summary>Speech speed multiplier between 0.1 and 3.0.</summary>
    [JsonProperty("speed")]
    public float Speed { get; set; } = 1.0f;

    /// <summary>Voice pitch multiplier between 0.1 and 3.0.</summary>
    [JsonProperty("pitch")]
    public float Pitch { get; set; } = 1.0f;

    /// <summary>Output audio format. The server accepts "wav" or "mp3".</summary>
    [JsonProperty("format")]
    public string Format { get; set; } = "wav";
}
