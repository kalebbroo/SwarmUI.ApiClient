using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Fields shared by every LLM Assistant response.</summary>
/// <remarks>The extension reports failures in-band rather than by HTTP status, so callers should check <see cref="Success"/> before reading operation specific fields.</remarks>
public class LLMAssistantResponse
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>Error message when the operation failed.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }
}
