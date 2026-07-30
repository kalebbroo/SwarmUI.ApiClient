using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract listing every registered AudioLab provider.</summary>
public class AudioProvidersStatusResponse : AudioLabResponse
{
    /// <summary>Registered providers.</summary>
    [JsonProperty("providers")]
    public AudioProviderStatus[] Providers { get; set; } = [];

    /// <summary>Number of registered providers.</summary>
    [JsonProperty("total_count")]
    public int TotalCount { get; set; }
}

/// <summary>Summary of a single registered AudioLab provider.</summary>
public class AudioProviderStatus
{
    /// <summary>Provider identifier used by the processing endpoints.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the provider.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Provider category, for example TTS, STT, or AudioGen.</summary>
    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Number of models the provider exposes.</summary>
    [JsonProperty("model_count")]
    public int ModelCount { get; set; }
}
