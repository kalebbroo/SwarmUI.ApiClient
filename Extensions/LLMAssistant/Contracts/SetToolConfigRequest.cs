using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for replacing the caller's per-tool configuration.</summary>
public class SetToolConfigRequest
{
    /// <summary>Tool to configure. Required.</summary>
    [JsonProperty("toolId")]
    public string ToolId { get; set; } = string.Empty;

    /// <summary>Replacement configuration block.</summary>
    [JsonProperty("config")]
    public JObject Config { get; set; } = [];
}
