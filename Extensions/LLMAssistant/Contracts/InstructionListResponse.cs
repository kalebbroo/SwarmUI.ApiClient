using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the instructions visible to the caller.</summary>
/// <remarks>Instructions are retained for text-to-image prompt tag compatibility; assistants are the primary way to configure system prompts.</remarks>
public class InstructionListResponse : LLMAssistantResponse
{
    /// <summary>Visible instructions, shared and personal combined.</summary>
    [JsonProperty("instructions")]
    public JArray Instructions { get; set; } = [];

    /// <summary>Whether the caller may create or edit shared instructions.</summary>
    [JsonProperty("canWriteShared")]
    public bool CanWriteShared { get; set; }
}
