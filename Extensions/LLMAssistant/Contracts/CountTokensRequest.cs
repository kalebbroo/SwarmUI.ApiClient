using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for counting tokens.</summary>
/// <remarks>Supply either <see cref="Text"/> or <see cref="Messages"/>. When both are absent the server counts an empty string.</remarks>
public class CountTokensRequest
{
    /// <summary>Raw text to count.</summary>
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    /// <summary>Messages to count. Each entry is flattened as "role: content" before counting.</summary>
    [JsonProperty("messages", NullValueHandling = NullValueHandling.Ignore)]
    public JArray? Messages { get; set; }
}
