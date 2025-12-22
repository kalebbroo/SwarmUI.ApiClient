using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SwarmUI.ApiClient.Models.Common;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Strongly-typed representation of SwarmUI's GetCurrentStatus API response.</summary>
/// <remarks>Provides a snapshot of queue state, backend health, and supported features. The route is documented in <c>BasicAPIFeatures.md</c> and reuses <see cref="StatusInfo"/> and <see cref="BackendStatus"/> from <c>GenerationUpdate</c> for consistency.</remarks>
public class ServerStatusResponse
{
    /// <summary>Overall generation queue and activity status, including how many generations are waiting, loading, or actively running. This is the same structure used in WebSocket <c>status</c> updates from <c>GenerateText2ImageWS</c>.</summary>
    [JsonProperty("status")]
    public StatusInfo? Status { get; set; }

    /// <summary>Summary of backend GPU infrastructure state at the time of the status check. Indicates whether backends are running, loading models, or in error, and provides a human-readable message used by the web UI.</summary>
    [JsonProperty("backend_status")]
    public BackendStatus? BackendStatus { get; set; }

    /// <summary>List of feature flags supported by the current SwarmUI server instance. Feature IDs are used by the UI to enable or disable certain capabilities dynamically based on server configuration.</summary>
    [JsonProperty("supported_features")]
    public List<string>? SupportedFeatures { get; set; }

    /// <summary>Raw JSON data from the response that is not mapped to explicit properties. This preserves forward compatibility with future SwarmUI versions that add additional fields to GetCurrentStatus responses.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
