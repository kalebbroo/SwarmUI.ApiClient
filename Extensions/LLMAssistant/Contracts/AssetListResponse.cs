using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing a thread's assets.</summary>
/// <remarks>Assets are artifacts promoted out of messages or tool results and stored inside the owning thread's blob.</remarks>
public class AssetListResponse : LLMAssistantResponse
{
    /// <summary>Thread the assets belong to.</summary>
    [JsonProperty("threadId")]
    public string? ThreadId { get; set; }

    /// <summary>The thread's assets, each with full content.</summary>
    [JsonProperty("assets")]
    public JArray Assets { get; set; } = [];
}
