using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for unloading resident LLM models.</summary>
public class UnloadModelsResponse : LLMAssistantResponse
{
    /// <summary>Number of providers that actually freed a loaded model.</summary>
    [JsonProperty("freed")]
    public int Freed { get; set; }

    /// <summary>Number of providers asked to unload. Remote providers are no-ops.</summary>
    [JsonProperty("providers")]
    public int Providers { get; set; }
}
