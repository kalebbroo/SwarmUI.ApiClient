using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying a single chat thread.</summary>
/// <remarks>The thread is stored as a JSON blob whose schema is owned by the extension, so it is surfaced as a <see cref="JObject"/> rather than a fixed shape. Messages form a tree: each carries an id, role, and content, and branches are created by editing or regenerating.</remarks>
public class ThreadResponse : LLMAssistantResponse
{
    /// <summary>The thread blob, including its messages array, title, assistant, and parameters.</summary>
    [JsonProperty("thread")]
    public JObject? Thread { get; set; }
}
