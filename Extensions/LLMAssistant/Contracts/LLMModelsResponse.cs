using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing available LLM models across every registered provider.</summary>
/// <remarks>Providers are queried in parallel under a bounded timeout, so a single unreachable backend degrades to a warning instead of failing the call. Always check <see cref="Warnings"/> before treating the list as complete.</remarks>
public class LLMModelsResponse : LLMAssistantResponse
{
    /// <summary>Available models. Each entry carries the provider's model fields plus a "title" alias of "name".</summary>
    [JsonProperty("models")]
    public JArray Models { get; set; } = [];

    /// <summary>One message per provider that timed out or errored while listing.</summary>
    [JsonProperty("warnings")]
    public string[] Warnings { get; set; } = [];
}
