using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for a chained AudioLab workflow.</summary>
/// <remarks>The server requires at least one step, and requires the first step to be "stt" when <see cref="InputType"/> is "audio".</remarks>
public class AudioWorkflowRequest
{
    /// <summary>Workflow label, for example "stt_to_tts" or "custom".</summary>
    [JsonProperty("workflow_type")]
    public string WorkflowType { get; set; } = "custom";

    /// <summary>Input payload, either text or base64 encoded audio depending on <see cref="InputType"/>.</summary>
    [JsonProperty("input_data")]
    public string InputData { get; set; } = string.Empty;

    /// <summary>Type of <see cref="InputData"/>, either "text" or "audio".</summary>
    [JsonProperty("input_type")]
    public string InputType { get; set; } = "text";

    /// <summary>Steps to execute, run in ascending <see cref="AudioWorkflowStep.Order"/>.</summary>
    [JsonProperty("steps")]
    public List<AudioWorkflowStep> Steps { get; set; } = [];
}

/// <summary>A single stage in an AudioLab workflow.</summary>
public class AudioWorkflowStep
{
    /// <summary>Step type. The server accepts "stt", "tts", "llm", or "custom".</summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Whether this step runs. Disabled steps are skipped.</summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>Execution order within the workflow.</summary>
    [JsonProperty("order")]
    public int Order { get; set; }

    /// <summary>Step specific configuration, such as voice, language, or volume.</summary>
    [JsonProperty("config")]
    public JObject Config { get; set; } = [];
}
