using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for editing a stored message in place.</summary>
/// <remarks>This rewrites the existing message rather than branching. To edit and regenerate a reply as a new branch, stream the edit endpoint instead.</remarks>
public class EditMessageRequest
{
    /// <summary>Thread containing the message. Required.</summary>
    [JsonProperty("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>Message to rewrite. Required.</summary>
    [JsonProperty("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Replacement content. Required, though an empty string is accepted to blank a message.</summary>
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}
