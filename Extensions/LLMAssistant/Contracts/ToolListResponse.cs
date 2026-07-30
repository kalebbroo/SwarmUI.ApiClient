using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the tools visible to the caller.</summary>
public class ToolListResponse : LLMAssistantResponse
{
    /// <summary>Visible tool definitions, built-in and custom combined.</summary>
    [JsonProperty("tools")]
    public JArray Tools { get; set; } = [];

    /// <summary>Whether the caller may create or edit shared tools.</summary>
    [JsonProperty("canWriteShared")]
    public bool CanWriteShared { get; set; }
}
