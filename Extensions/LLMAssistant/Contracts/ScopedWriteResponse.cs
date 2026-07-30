using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a write that targets either the personal or shared settings layer.</summary>
/// <remarks>Shared writes require the <c>llm_shared_write</c> permission; without it the server rejects the request rather than silently writing to the personal layer.</remarks>
public class ScopedWriteResponse : LLMAssistantResponse
{
    /// <summary>Identifier of the created or updated record.</summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>Layer the write landed in, either "personal" or "shared".</summary>
    [JsonProperty("scope")]
    public string? Scope { get; set; }
}
