using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>A single streamed frame from a chat endpoint.</summary>
/// <remarks>Frame bodies are produced by the extension's stream helper and vary by model, tool activity, and compare mode, so the complete frame is preserved in <see cref="Raw"/>. The typed members cover the fields the extension guarantees.</remarks>
public class ChatStreamUpdate
{
    /// <summary>Failure detail when a stream or one compare lane fails.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>Compare-mode lane index this frame belongs to. Null on single-model streams.</summary>
    [JsonProperty("lane")]
    public int? Lane { get; set; }

    /// <summary>The complete frame as received, including any field without a typed member.</summary>
    [JsonIgnore]
    public JObject Raw { get; set; } = [];

    /// <summary>Whether this frame reports a failure.</summary>
    [JsonIgnore]
    public bool IsError => Error is not null;
}
