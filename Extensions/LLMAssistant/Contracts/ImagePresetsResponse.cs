using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract listing the caller's text-to-image presets as the assistant sees them.</summary>
public class ImagePresetsResponse : LLMAssistantResponse
{
    /// <summary>The caller's presets.</summary>
    [JsonProperty("presets")]
    public ImagePresetSummary[] Presets { get; set; } = [];
}

/// <summary>A text-to-image preset summarized for assistant and tool use.</summary>
public class ImagePresetSummary
{
    /// <summary>Preset title.</summary>
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Preset description.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>One-line breakdown of model, steps, sampler, dimensions, and LoRAs, matching what the model sees when selecting a preset.</summary>
    [JsonProperty("summary")]
    public string Summary { get; set; } = string.Empty;
}
