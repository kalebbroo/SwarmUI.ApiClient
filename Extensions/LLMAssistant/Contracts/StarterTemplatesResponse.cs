using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the bundled starter assistant templates.</summary>
/// <remarks>Templates are baseline reference data read from disk once and cached for the process lifetime, intended as a starting point for cloning into a new assistant.</remarks>
public class StarterTemplatesResponse : LLMAssistantResponse
{
    /// <summary>Bundled assistant templates.</summary>
    [JsonProperty("templates")]
    public JArray Templates { get; set; } = [];
}
