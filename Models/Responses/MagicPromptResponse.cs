using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from MagicPrompt LLM enhancement API.</summary>
/// <remarks>
/// NOTE: This is a Hartsy-specific extension endpoint. The MagicPrompt functionality is not part
/// of the standard SwarmUI distribution and requires the Hartsy MagicPrompt extension to be installed
/// and configured on the SwarmUI server. Without this extension, calls to this endpoint will fail.
/// </remarks>
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
