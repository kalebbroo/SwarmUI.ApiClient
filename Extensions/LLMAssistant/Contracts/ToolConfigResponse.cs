using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying the caller's per-tool configuration.</summary>
public class ToolConfigResponse : LLMAssistantResponse
{
    /// <summary>Tool the configuration belongs to.</summary>
    [JsonProperty("toolId")]
    public string? ToolId { get; set; }

    /// <summary>The tool's configuration block for this user.</summary>
    [JsonProperty("config")]
    public JObject? Config { get; set; }
}
