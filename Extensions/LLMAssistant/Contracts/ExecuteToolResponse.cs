using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a direct tool invocation.</summary>
public class ExecuteToolResponse : LLMAssistantResponse
{
    /// <summary>The tool's result. Shape is defined by the tool that ran.</summary>
    [JsonProperty("result")]
    public JToken? Result { get; set; }

    /// <summary>Call identifier echoed from the request.</summary>
    [JsonProperty("callId")]
    public string? CallId { get; set; }
}
