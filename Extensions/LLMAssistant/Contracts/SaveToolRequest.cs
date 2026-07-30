using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for creating or updating a tool definition.</summary>
public class SaveToolRequest
{
    /// <summary>Tool definition to store. Required.</summary>
    [JsonProperty("tool")]
    public JObject Tool { get; set; } = [];

    /// <summary>Target layer, either "personal" (default) or "shared". Shared requires the <c>llm_shared_write</c> permission.</summary>
    [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
    public string? Scope { get; set; }
}
