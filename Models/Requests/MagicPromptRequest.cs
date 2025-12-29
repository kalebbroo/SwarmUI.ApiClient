using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Requests;

/// <summary>Request model for MagicPrompt text enhancement via LLM.</summary>
/// <remarks>
/// NOTE: This is a Hartsy-specific extension endpoint. The MagicPrompt functionality is not part
/// of the standard SwarmUI distribution and requires the Hartsy MagicPrompt extension to be installed
/// and configured on the SwarmUI server. Without this extension, calls to this endpoint will fail.
/// </remarks>
public class MagicPromptRequest
{
    /// <summary>The text content to enhance via MagicPrompt.</summary>
    [JsonProperty("messageContent")]
    public MessageContent Content { get; set; } = new();

    /// <summary>The LLM model ID to use for enhancement (e.g., "claude-3-5-haiku-20241022").</summary>
    [JsonProperty("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Message type - typically "Text" for prompt enhancement.</summary>
    [JsonProperty("messageType")]
    public string MessageType { get; set; } = "Text";

    /// <summary>Action type - typically "chat" for conversational enhancement.</summary>
    [JsonProperty("action")]
    public string Action { get; set; } = "chat";

    /// <summary>Optional seed for reproducible results. Use -1 for random.</summary>
    [JsonProperty("seed")]
    public long Seed { get; set; } = -1;
}

/// <summary>Message content wrapper for MagicPrompt requests.</summary>
public class MessageContent
{
    /// <summary>The text to enhance.</summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Keep-alive setting for streaming connections.</summary>
    [JsonProperty("KeepAlive")]
    public object? KeepAlive { get; set; } = null;
}
