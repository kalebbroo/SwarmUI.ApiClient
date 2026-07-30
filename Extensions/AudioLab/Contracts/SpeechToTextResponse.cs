using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for AudioLab speech-to-text transcription.</summary>
public class SpeechToTextResponse : AudioLabResponse
{
    /// <summary>Transcribed text.</summary>
    [JsonProperty("transcription")]
    public string Transcription { get; set; } = string.Empty;

    /// <summary>Transcription confidence between 0.0 and 1.0.</summary>
    [JsonProperty("confidence")]
    public float Confidence { get; set; }

    /// <summary>Language used for transcription.</summary>
    [JsonProperty("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>Alternative transcriptions, when requested.</summary>
    [JsonProperty("alternatives")]
    public string[] Alternatives { get; set; } = [];

    /// <summary>Session the transcription ran under.</summary>
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }

    /// <summary>Provider supplied metadata about the transcription.</summary>
    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
