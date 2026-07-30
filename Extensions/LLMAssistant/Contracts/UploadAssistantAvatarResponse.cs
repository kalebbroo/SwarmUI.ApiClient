using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for an assistant avatar upload.</summary>
public class UploadAssistantAvatarResponse : LLMAssistantResponse
{
    /// <summary>Served URL of the stored avatar, to be written to the assistant's avatar field.</summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Number of bytes written to disk.</summary>
    [JsonProperty("bytesWritten")]
    public long BytesWritten { get; set; }
}
