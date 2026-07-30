using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a thread export.</summary>
public class ThreadExportResponse : LLMAssistantResponse
{
    /// <summary>Exported thread content, either indented JSON or markdown.</summary>
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Suggested filename, derived from the thread title.</summary>
    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;
}
