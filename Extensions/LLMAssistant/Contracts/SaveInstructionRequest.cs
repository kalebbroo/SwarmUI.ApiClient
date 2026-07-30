using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for creating or updating an instruction.</summary>
public class SaveInstructionRequest
{
    /// <summary>Instruction identifier. Omit to have the server assign one for a new instruction.</summary>
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    /// <summary>Display title.</summary>
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    /// <summary>Instruction text used as the system prompt. Required.</summary>
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Whether this records a built-in instruction override rather than a custom instruction.</summary>
    [JsonProperty("isBuiltIn")]
    public bool IsBuiltIn { get; set; }

    /// <summary>Tooltip shown in the interface.</summary>
    [JsonProperty("tooltip", NullValueHandling = NullValueHandling.Ignore)]
    public string? Tooltip { get; set; }

    /// <summary>Categories used to group the instruction.</summary>
    [JsonProperty("categories", NullValueHandling = NullValueHandling.Ignore)]
    public JArray? Categories { get; set; }

    /// <summary>Target layer, either "personal" (default) or "shared". Shared requires the <c>llm_shared_write</c> permission.</summary>
    [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
    public string? Scope { get; set; }
}
