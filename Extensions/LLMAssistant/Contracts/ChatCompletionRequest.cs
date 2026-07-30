using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for a non-streaming LLM completion.</summary>
/// <remarks>This path does not touch chat threads. It is intended for utility callers such as prompt enhancement. For conversational use, stream through a thread instead.</remarks>
public class ChatCompletionRequest
{
    /// <summary>Message to send to the model. Required.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Instruction to use as the system prompt. Falls back to the assistant's instruction when null.</summary>
    [JsonProperty("instructionId", NullValueHandling = NullValueHandling.Ignore)]
    public string? InstructionId { get; set; }

    /// <summary>Model to run. Falls back to the caller's preferred model when null.</summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>Sampling temperature. Negative one uses the resolved assistant or user default.</summary>
    [JsonProperty("temperature")]
    public double Temperature { get; set; } = -1;

    /// <summary>Maximum tokens to generate. Negative one uses the resolved assistant or user default.</summary>
    [JsonProperty("maxTokens")]
    public int MaxTokens { get; set; } = -1;

    /// <summary>Whether to bypass the server's process-lifetime completion cache.</summary>
    [JsonProperty("noCache")]
    public bool NoCache { get; set; }

    /// <summary>Assistant whose instruction and parameters should be resolved. Falls back to the caller's active assistant when null.</summary>
    [JsonProperty("assistantId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantId { get; set; }
}
