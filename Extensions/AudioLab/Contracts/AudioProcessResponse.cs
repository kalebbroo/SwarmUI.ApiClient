using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for provider routed audio processing.</summary>
/// <remarks>Provider results vary by engine, so any field beyond the standard envelope is captured in <see cref="AdditionalData"/> rather than being dropped.</remarks>
public class AudioProcessResponse : AudioLabResponse
{
    /// <summary>Base64 encoded audio output, when the provider produces audio.</summary>
    [JsonProperty("audio_data")]
    public string? AudioData { get; set; }

    /// <summary>Text output, when the provider produces text such as a transcription.</summary>
    [JsonProperty("text")]
    public string? Text { get; set; }

    /// <summary>Every provider specific field that is not part of the standard response envelope.</summary>
    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
}
