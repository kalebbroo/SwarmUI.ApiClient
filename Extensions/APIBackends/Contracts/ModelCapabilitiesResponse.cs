using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.APIBackends.Contracts;

/// <summary>Response from <c>APIBackendsListModelCapabilities</c>.</summary>
public class ModelCapabilitiesResponse
{
    /// <summary>What each API-backed model accepts, keyed by the same full model name generation parameters use.</summary>
    [JsonProperty("models")]
    public Dictionary<string, ApiModelCapability>? Models { get; set; }
}

/// <summary>What one API-backed model accepts, so a caller can shape a request before sending it rather than learning from a rejection.</summary>
public class ApiModelCapability
{
    /// <summary>Provider family, for example <c>"video.openai_sora"</c> or <c>"image.openai"</c>.</summary>
    [JsonProperty("family")]
    public string Family { get; set; } = string.Empty;

    /// <summary>What this model produces: <c>"image"</c>, <c>"video"</c>, and so on.</summary>
    [JsonProperty("modality")]
    public string Modality { get; set; } = string.Empty;

    /// <summary>True when the model accepts an init image.</summary>
    [JsonProperty("init_image")]
    public bool InitImage { get; set; }

    /// <summary>True when the model can produce more than one output per request.</summary>
    [JsonProperty("supports_batch")]
    public bool SupportsBatch { get; set; }

    /// <summary>Feature flags this model advertises, naming the parameter groups that apply to it.</summary>
    [JsonProperty("flags")]
    public List<string> Flags { get; set; } = new List<string>();
}
