using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's <c>GetModelHash</c> endpoint. Contains the tensor hash string for a specific model, which can be used to uniquely identify the model version across servers.</summary>
public class ModelHashResponse
{
    /// <summary>Hex-encoded tensor hash of the model, as returned by SwarmUI.</summary>
    [JsonProperty("hash")]
    public string Hash { get; set; } = string.Empty;
}
