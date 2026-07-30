using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for audio time stretch.</summary>
public class AudioTimeStretchResponse : AudioLabResponse
{
    /// <summary>Base64 encoded stretched audio.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Tempo multiplier that was applied.</summary>
    [JsonProperty("rate")]
    public double Rate { get; set; }

    /// <summary>Pitch shift in semitones that was applied.</summary>
    [JsonProperty("semitones")]
    public double Semitones { get; set; }
}
