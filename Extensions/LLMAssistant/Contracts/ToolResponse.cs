using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying a single tool definition.</summary>
public class ToolResponse : LLMAssistantResponse
{
    /// <summary>The tool definition.</summary>
    [JsonProperty("tool")]
    public JObject? Tool { get; set; }
}
