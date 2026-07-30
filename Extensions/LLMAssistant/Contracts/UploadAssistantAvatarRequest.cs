using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for uploading an assistant avatar.</summary>
/// <remarks>The server accepts png, jpeg, webp, and gif data URIs up to 2 MB decoded, and stores them under the caller's output directory.</remarks>
public class UploadAssistantAvatarRequest
{
    /// <summary>Assistant the avatar belongs to. Required.</summary>
    [JsonProperty("assistantId")]
    public string AssistantId { get; set; } = string.Empty;

    /// <summary>Avatar as a base64 data URI. Required.</summary>
    [JsonProperty("imageData")]
    public string ImageData { get; set; } = string.Empty;
}
