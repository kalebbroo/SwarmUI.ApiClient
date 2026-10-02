using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Generation parameters registered by the LLMAssistant server extension's <c>&lt;llmprompt&gt;</c>
/// prompt-processing tag handler.</summary>
/// <remarks>
/// <para>Attached to a request via <see cref="SwarmUI.ApiClient.Extensions.GenerationRequestExtensionParams.LLMAssistant"/>
/// (<c>request.Extensions.LLMAssistant</c>). Registered by <c>SwarmUI-LLMAssistant</c>'s <c>PromptTagHandler</c>,
/// not by stock SwarmUI — a server without this extension installed does not recognize these wire names.</para>
/// </remarks>
public class LLMAssistantGenerationParams
{
    /// <summary>Cache LLM responses for identical prompts. ("LLM Use Cache").</summary>
    /// <remarks>Useful for batch generation. Server default: <c>true</c>.</remarks>
    [JsonProperty("llmusecache")]
    public bool? LlmUseCache { get; set; }

    /// <summary>Generate a consistent wildcard seed per batch for reproducible results. ("LLM Generate Wildcard Seed").</summary>
    /// <remarks>Server default: <c>false</c>.</remarks>
    [JsonProperty("llmgeneratewildcardseed")]
    public bool? LlmGenerateWildcardSeed { get; set; }

    /// <summary>Which LLM model to use for prompt processing. ("LLM Model ID").</summary>
    /// <remarks>Server default: <c>default</c>.</remarks>
    [JsonProperty("llmmodelid")]
    public string? LlmModelId { get; set; }

    /// <summary>Which instruction set to use for prompt processing. ("LLM Instructions").</summary>
    /// <remarks>Allowed values: <c>chat</c>, <c>vision</c>, <c>caption</c>, <c>prompt</c>, <c>randomprompt</c>, <c>instructiongen</c>, <c>companion</c>.</remarks>
    [JsonProperty("llminstructions")]
    public string? LlmInstructions { get; set; }

    /// <summary>Which assistant's instructions (and per-model variants) to use for &lt;llmprompt&gt; processing. ("LLM Assistant ID").</summary>
    /// <remarks>Default = active assistant. Allowed values: <c>default</c>, <c>jarvis</c>, <c>assistant-mu564gsu-pmesmbp</c>.</remarks>
    [JsonProperty("llmassistantid")]
    public string? LlmAssistantId { get; set; }
}
