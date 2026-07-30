using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for the per-thread tool-calling override.</summary>
public class ThreadToolsEnabledResponse : LLMAssistantResponse
{
    /// <summary>Thread the override applies to.</summary>
    [JsonProperty("threadId")]
    public string? ThreadId { get; set; }

    /// <summary>Override state. Null means the override was cleared and the thread inherits the assistant's default.</summary>
    [JsonProperty("toolsEnabled")]
    public bool? ToolsEnabled { get; set; }
}
