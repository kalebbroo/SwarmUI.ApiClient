using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for uploading an image attached to a chat message.</summary>
/// <remarks>The server downscales the image per the caller's vision image size setting and stores it in their uploads directory, so threads reference a URL rather than embedded base64.</remarks>
public class UploadChatImageRequest
{
    /// <summary>Thread the image belongs to. Required.</summary>
    [JsonProperty("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>Message the image is attached to. Required.</summary>
    [JsonProperty("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Image as a base64 data URI. Required.</summary>
    [JsonProperty("imageData")]
    public string ImageData { get; set; } = string.Empty;
}
