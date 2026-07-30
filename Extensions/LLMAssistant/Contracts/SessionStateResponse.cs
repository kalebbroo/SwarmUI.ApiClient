using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying the caller's LLM Assistant session state.</summary>
/// <remarks>Session state holds UI continuation data such as the active thread and last used model, letting a headless client resume where the interface left off.</remarks>
public class SessionStateResponse : LLMAssistantResponse
{
    /// <summary>The caller's session state blob.</summary>
    [JsonProperty("state")]
    public JObject? State { get; set; }
}
