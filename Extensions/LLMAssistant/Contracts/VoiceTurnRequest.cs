using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>One message in a <see cref="VoiceTurnRequest"/>'s conversation history.</summary>
/// <remarks>Mirrors the shape a <c>native_tool_call</c>/<c>tool_result</c> frame pair already uses on the way
/// out, so a replayed assistant message with tool calls round-trips the same shape either direction.</remarks>
public class VoiceTurnMessage
{
    /// <summary>"system", "user", "assistant", or "tool".</summary>
    [JsonProperty("role")]
    public string Role { get; set; } = "user";

    /// <summary>Message text.</summary>
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>The tool call this message answers. Required on a "tool" role message.</summary>
    [JsonProperty("toolCallId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ToolCallId { get; set; }

    /// <summary>Tool name, alongside <see cref="ToolCallId"/> on a "tool" role message.</summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    /// <summary>Tool calls this assistant message made, when replaying one that made any.</summary>
    [JsonProperty("toolCalls", NullValueHandling = NullValueHandling.Ignore)]
    public List<VoiceTurnToolCall>? ToolCalls { get; set; }
}

/// <summary>A tool call embedded in a replayed <see cref="VoiceTurnMessage"/>, or received live as part of a
/// <see cref="VoiceTurnNativeToolCall"/> frame.</summary>
public class VoiceTurnToolCall
{
    /// <summary>Identifier correlating this call with its later result.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Tool name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Call arguments as a nested JSON value (an object in practice), not a JSON-encoded string.</summary>
    [JsonProperty("arguments")]
    public JToken Arguments { get; set; } = new JObject();
}

/// <summary>Request contract for <see cref="ILLMAssistantEndpoint.StreamVoiceTurnAsync"/>.</summary>
/// <remarks>Unlike the thread-based streaming endpoints (<see cref="ChatStreamRequest"/>), this route is
/// stateless on the server: the caller sends the full conversation every turn and nothing is persisted to a
/// thread, matching how <c>HartsyInference.Voice</c>'s own turn loop (the route's one real caller today) keeps
/// history client-side rather than server-side.</remarks>
public class VoiceTurnRequest
{
    /// <summary>Full conversation so far, oldest first. Required and must be non-empty.</summary>
    [JsonProperty("messages")]
    public List<VoiceTurnMessage> Messages { get; set; } = [];

    /// <summary>Model to run. Falls back to the caller's preferred model when null.</summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>Assistant (tool registry, persona) to run under. Falls back to the default assistant when null.</summary>
    [JsonProperty("assistantId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantId { get; set; }

    /// <summary>Requests a thinking/reasoning pass before the reply, on models that support it. Server default
    /// when null.</summary>
    [JsonProperty("enableThinking", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableThinking { get; set; }

    /// <summary>Sampling temperature. Server-resolved default when null.</summary>
    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Temperature { get; set; }

    /// <summary>Maximum tokens to generate. Server-resolved default when null.</summary>
    [JsonProperty("maxTokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxTokens { get; set; }
}
