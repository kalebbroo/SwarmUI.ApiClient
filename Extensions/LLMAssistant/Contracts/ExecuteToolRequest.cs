using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for invoking a tool directly, bypassing the model.</summary>
/// <remarks>The per-tool permission gate still applies, so this is not a way around the <c>llm_tool_*</c> permissions.</remarks>
public class ExecuteToolRequest
{
    /// <summary>Tool to run. Required.</summary>
    [JsonProperty("toolId")]
    public string ToolId { get; set; } = string.Empty;

    /// <summary>Arguments to pass to the tool.</summary>
    [JsonProperty("arguments")]
    public JObject Arguments { get; set; } = [];

    /// <summary>Assistant context for the invocation.</summary>
    [JsonProperty("assistantId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantId { get; set; }

    /// <summary>Thread to record the invocation against. When set, the call is persisted to that thread.</summary>
    [JsonProperty("threadId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ThreadId { get; set; }

    /// <summary>Client assigned call identifier, echoed back on the response for correlation.</summary>
    [JsonProperty("callId", NullValueHandling = NullValueHandling.Ignore)]
    public string? CallId { get; set; }
}
