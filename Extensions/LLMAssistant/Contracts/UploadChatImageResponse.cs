using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a chat image upload.</summary>
public class UploadChatImageResponse : LLMAssistantResponse
{
    /// <summary>Served URL of the stored image, relative to the SwarmUI root.</summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>MIME type of the stored image.</summary>
    [JsonProperty("mediaType")]
    public string? MediaType { get; set; }

    /// <summary>Stored image width in pixels, after any downscale.</summary>
    [JsonProperty("width")]
    public int Width { get; set; }

    /// <summary>Stored image height in pixels, after any downscale.</summary>
    [JsonProperty("height")]
    public int Height { get; set; }

    /// <summary>Number of bytes written to disk.</summary>
    [JsonProperty("bytesWritten")]
    public long BytesWritten { get; set; }
}
