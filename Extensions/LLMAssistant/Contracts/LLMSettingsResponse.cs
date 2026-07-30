using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying LLM Assistant settings.</summary>
/// <remarks>Reads return the caller's merged view, with their personal overrides layered over the shared baseline.</remarks>
public class LLMSettingsResponse : LLMAssistantResponse
{
    /// <summary>The effective settings blob.</summary>
    [JsonProperty("settings")]
    public JObject? Settings { get; set; }

    /// <summary>Whether the caller holds the <c>llm_shared_write</c> permission and may write the shared layer.</summary>
    [JsonProperty("canWriteShared")]
    public bool CanWriteShared { get; set; }

    /// <summary>Layer a write landed in, either "personal" or "shared". Only set on writes.</summary>
    [JsonProperty("scope")]
    public string? Scope { get; set; }
}
