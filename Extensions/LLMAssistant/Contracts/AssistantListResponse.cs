using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the assistants visible to the caller.</summary>
/// <remarks>Entries merge the shared and personal layers, each tagged with a <c>_scope</c> marker identifying which layer it came from.</remarks>
public class AssistantListResponse : LLMAssistantResponse
{
    /// <summary>Visible assistants, shared and personal combined.</summary>
    [JsonProperty("assistants")]
    public JArray Assistants { get; set; } = [];

    /// <summary>Identifier of the caller's active assistant.</summary>
    [JsonProperty("activeAssistantId")]
    public string? ActiveAssistantId { get; set; }

    /// <summary>Whether the caller may create or edit shared assistants.</summary>
    [JsonProperty("canWriteShared")]
    public bool CanWriteShared { get; set; }
}
