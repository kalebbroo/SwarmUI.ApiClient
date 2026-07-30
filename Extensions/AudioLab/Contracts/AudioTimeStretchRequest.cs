using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for tempo-preserving time stretch with optional pitch shift.</summary>
/// <remarks>Requires ffmpeg on the SwarmUI host.</remarks>
public class AudioTimeStretchRequest
{
    /// <summary>Base64 encoded source audio. Required.</summary>
    [JsonProperty("audio_data")]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>Output tempo multiplier between 0.25 and 4.0, where 2.0 plays twice as fast.</summary>
    [JsonProperty("rate")]
    public double Rate { get; set; } = 1.0;

    /// <summary>Pitch shift in semitones. Zero leaves pitch unchanged.</summary>
    [JsonProperty("semitones")]
    public double Semitones { get; set; }
}
