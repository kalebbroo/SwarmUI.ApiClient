using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>A live <c>native_tool_call</c> frame: the model is calling a tool, before the result comes back.</summary>
public class VoiceTurnNativeToolCall
{
    /// <summary>Identifier the matching <see cref="VoiceTurnToolResult"/> will carry.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Tool name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Call arguments as a nested JSON value (an object in practice), not a JSON-encoded string.</summary>
    [JsonProperty("arguments")]
    public JToken? Arguments { get; set; }
}

/// <summary>A live <c>tool_result</c> frame: the result of a tool call this same stream already reported via a
/// <see cref="VoiceTurnNativeToolCall"/> frame.</summary>
public class VoiceTurnToolResult
{
    /// <summary>Matches the originating <see cref="VoiceTurnNativeToolCall.Id"/>.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Tool name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Result as a nested JSON value, not a JSON-encoded string.</summary>
    [JsonProperty("result")]
    public JToken? Result { get; set; }
}

/// <summary>A single streamed frame from <see cref="ILLMAssistantEndpoint.StreamVoiceTurnAsync"/>.</summary>
/// <remarks>Exactly one of <see cref="Chunk"/>, <see cref="NativeToolCall"/>, <see cref="ToolResult"/>,
/// <see cref="Notice"/>, <see cref="Done"/>, or <see cref="Error"/> is set per frame (<see cref="Done"/> is a bool
/// so check it rather than null-testing). <see cref="Raw"/> preserves the complete frame, same as
/// <see cref="ChatStreamUpdate.Raw"/>, for any field without a typed member.</remarks>
public class VoiceTurnUpdate
{
    /// <summary>Text chunk of the reply.</summary>
    [JsonProperty("chunk")]
    public string? Chunk { get; set; }

    /// <summary>The model is calling a tool. Dispatch and the resulting <see cref="ToolResult"/> frame are both
    /// the server's responsibility -- this is a live report, not a request for the caller to execute anything.</summary>
    [JsonProperty("native_tool_call")]
    public VoiceTurnNativeToolCall? NativeToolCall { get; set; }

    /// <summary>The result of a tool call this stream already reported.</summary>
    [JsonProperty("tool_result")]
    public VoiceTurnToolResult? ToolResult { get; set; }

    /// <summary>An informational notice (eg "tool calling is unavailable for this model") that is not itself a
    /// failure.</summary>
    [JsonProperty("notice")]
    public string? Notice { get; set; }

    /// <summary>Whether the turn has finished. The last frame of a successful turn.</summary>
    [JsonProperty("done")]
    public bool Done { get; set; }

    /// <summary>The complete reply text, set alongside <see cref="Done"/>.</summary>
    [JsonProperty("full_text")]
    public string? FullText { get; set; }

    /// <summary>Wire stop reason, set alongside <see cref="Done"/>: "length", "cancelled", "error", "tool_call", or
    /// null for a normal finish. "tool_call" means the server's tool loop hit its round limit while the model still
    /// wanted another call; that last call was never dispatched, so treat the reply as cut short.</summary>
    [JsonProperty("stopReason")]
    public string? StopReason { get; set; }

    /// <summary>Failure detail when the turn (or the connection) failed.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>The complete frame as received, including any field without a typed member.</summary>
    [JsonIgnore]
    public JObject Raw { get; set; } = [];

    /// <summary>Whether this frame reports a failure.</summary>
    [JsonIgnore]
    public bool IsError => Error is not null;
}
