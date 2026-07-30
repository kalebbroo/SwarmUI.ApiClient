using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for AudioLab engine uninstall and bulk weight removal.</summary>
public class AudioEngineOperationResponse : AudioLabResponse
{
    /// <summary>Provider the operation targeted.</summary>
    [JsonProperty("provider_id")]
    public string? ProviderId { get; set; }

    /// <summary>Model the operation targeted, when a single variant was removed.</summary>
    [JsonProperty("model_id")]
    public string? ModelId { get; set; }

    /// <summary>Whether weights were deleted from disk.</summary>
    [JsonProperty("deleted_weights")]
    public bool DeletedWeights { get; set; }

    /// <summary>Number of models whose weights were removed by a bulk removal.</summary>
    [JsonProperty("removed")]
    public int Removed { get; set; }

    /// <summary>Number of models a bulk removal attempted.</summary>
    [JsonProperty("total")]
    public int Total { get; set; }

    /// <summary>Identifiers of the models a bulk removal deleted.</summary>
    [JsonProperty("removed_ids")]
    public string[] RemovedIds { get; set; } = [];
}
