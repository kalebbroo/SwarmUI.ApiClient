using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;

/// <summary>Response contract for MagicPrompt text enhancement.</summary>
public class MagicPromptResponse
{
    /// <summary>Indicates whether the enhancement was successful.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>The enhanced/rewritten text response from the LLM.</summary>
    [JsonProperty("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>Error message if the request failed.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>Error ID for categorizing failures.</summary>
    [JsonProperty("error_id")]
    public string? ErrorId { get; set; }
}
