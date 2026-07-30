using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for a chained AudioLab workflow.</summary>
public class AudioWorkflowResponse : AudioLabResponse
{
    /// <summary>Raw result of each executed step, keyed by step type.</summary>
    [JsonProperty("workflow_results")]
    public JObject? WorkflowResults { get; set; }

    /// <summary>Step types that ran, in execution order.</summary>
    [JsonProperty("executed_steps")]
    public string[] ExecutedSteps { get; set; } = [];

    /// <summary>Combined processing duration of every step, in seconds.</summary>
    [JsonProperty("total_processing_time")]
    public double TotalProcessingTime { get; set; }

    /// <summary>Session the workflow ran under.</summary>
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }
}
