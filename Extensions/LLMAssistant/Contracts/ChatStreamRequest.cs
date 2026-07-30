using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for the streaming chat endpoints.</summary>
/// <remarks>The thread is server-authoritative: the server loads it, appends the user message, builds the model input from stored history, streams the reply, and persists it. <see cref="MessageId"/>, <see cref="Content"/>, and <see cref="UserMessageId"/> apply only to the edit and regenerate variants.</remarks>
public class ChatStreamRequest
{
    /// <summary>Thread to send into. Required.</summary>
    [JsonProperty("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>Message text to send. Used when starting a new turn.</summary>
    [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; set; }

    /// <summary>Model to run. Falls back to the caller's preferred model when null.</summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>Instruction to use as the system prompt.</summary>
    [JsonProperty("instructionId", NullValueHandling = NullValueHandling.Ignore)]
    public string? InstructionId { get; set; }

    /// <summary>Sampling temperature. Negative one uses the resolved default.</summary>
    [JsonProperty("temperature")]
    public double Temperature { get; set; } = -1;

    /// <summary>Maximum tokens to generate. Negative one uses the resolved default.</summary>
    [JsonProperty("maxTokens")]
    public int MaxTokens { get; set; } = -1;

    /// <summary>Existing message to act on. Required when editing or regenerating.</summary>
    [JsonProperty("messageId", NullValueHandling = NullValueHandling.Ignore)]
    public string? MessageId { get; set; }

    /// <summary>Replacement content for the edited message. Required when editing.</summary>
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    /// <summary>Client assigned identifier for the edited user message, so the client can correlate the new branch.</summary>
    [JsonProperty("userMessageId", NullValueHandling = NullValueHandling.Ignore)]
    public string? UserMessageId { get; set; }

    /// <summary>Client assigned identifier for the assistant reply being produced.</summary>
    [JsonProperty("assistantMessageId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantMessageId { get; set; }
}
