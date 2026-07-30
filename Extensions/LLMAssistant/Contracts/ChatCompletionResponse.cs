using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a non-streaming LLM completion.</summary>
public class ChatCompletionResponse : LLMAssistantResponse
{
    /// <summary>Raw model output.</summary>
    [JsonProperty("response")]
    public string Response { get; set; } = string.Empty;
}
