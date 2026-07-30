using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for patching LLM Assistant settings.</summary>
/// <remarks>The server strips the <c>assistants</c> and <c>tools</c> dictionaries from the payload, since those are managed through their own endpoints. This keeps a settings save from wiping one layer's records.</remarks>
public class SaveSettingsRequest
{
    /// <summary>Settings to merge into the target layer.</summary>
    [JsonProperty("settings")]
    public JObject Settings { get; set; } = [];

    /// <summary>Target layer, either "personal" (default) or "shared". Shared requires the <c>llm_shared_write</c> permission.</summary>
    [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
    public string? Scope { get; set; }
}
