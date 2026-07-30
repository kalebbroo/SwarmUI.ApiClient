using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying a single assistant definition.</summary>
public class AssistantResponse : LLMAssistantResponse
{
    /// <summary>The assistant definition.</summary>
    [JsonProperty("assistant")]
    public JObject? Assistant { get; set; }

    /// <summary>Identifier of the caller's active assistant. Set when resolving the active assistant.</summary>
    [JsonProperty("activeAssistantId")]
    public string? ActiveAssistantId { get; set; }
}
