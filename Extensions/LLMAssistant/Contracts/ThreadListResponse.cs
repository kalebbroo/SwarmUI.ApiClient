using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the caller's chat threads.</summary>
public class ThreadListResponse : LLMAssistantResponse
{
    /// <summary>Thread index entries. This is a summary index, not the full thread blobs.</summary>
    [JsonProperty("threads")]
    public JArray Threads { get; set; } = [];
}
