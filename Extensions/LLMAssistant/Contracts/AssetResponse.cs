using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying a single thread asset.</summary>
public class AssetResponse : LLMAssistantResponse
{
    /// <summary>The asset, including its content.</summary>
    [JsonProperty("asset")]
    public JObject? Asset { get; set; }
}
