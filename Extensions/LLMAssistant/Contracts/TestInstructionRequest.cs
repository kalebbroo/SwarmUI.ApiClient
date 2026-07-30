using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Request contract for test-running an unsaved instruction.</summary>
/// <remarks>Nothing is persisted: no thread is created and no memory is written. Standard prompt variables are substituted so the preview matches real chat.</remarks>
public class TestInstructionRequest
{
    /// <summary>Instruction text to test as the system prompt. Required.</summary>
    [JsonProperty("instructionText")]
    public string InstructionText { get; set; } = string.Empty;

    /// <summary>Sample user message to run against the instruction. Required.</summary>
    [JsonProperty("sampleInput")]
    public string SampleInput { get; set; } = string.Empty;

    /// <summary>Model to run. Falls back to the caller's preferred model when null.</summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>Assistant name to substitute into the instruction. Defaults to "Test Assistant" server side.</summary>
    [JsonProperty("assistantName", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantName { get; set; }
}
