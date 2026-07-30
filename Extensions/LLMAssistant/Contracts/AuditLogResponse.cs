using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for the shared-write audit log.</summary>
/// <remarks>Admin only. The log records other users' shared writes and tool invocations, so it requires the <c>llm_shared_write</c> permission.</remarks>
public class AuditLogResponse : LLMAssistantResponse
{
    /// <summary>Whether audit logging is currently enabled.</summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Most recent log entries, newest last.</summary>
    [JsonProperty("entries")]
    public JArray Entries { get; set; } = [];
}
