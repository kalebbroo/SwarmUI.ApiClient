using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Models.Common;

/// <summary>Generic update message used for model-related WebSocket operations such as
/// <c>DoModelDownloadWS</c> and <c>SelectModelWS</c>.
/// The SwarmUI docs do not currently define a strict schema for these messages,
/// so this type captures a few common fields and preserves all raw data via
/// <see cref="ExtensionData"/>.</summary>
public class ModelOperationUpdate
{
    /// <summary>Optional human-readable status text for the operation.</summary>
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>Optional numeric progress indicator (0.0-1.0 or percentage depending on server).</summary>
    [JsonProperty("progress")]
    public double? Progress { get; set; }

    /// <summary>Optional message or log line associated with this update.</summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    /// <summary>Optional error text if the operation encountered a problem.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>Captures any additional JSON properties sent by SwarmUI so that callers
    /// can inspect the raw payload even if this client library has not modeled specific fields yet.</summary>
    [JsonExtensionData]
    public IDictionary<string, JToken>? ExtensionData { get; set; }
}
