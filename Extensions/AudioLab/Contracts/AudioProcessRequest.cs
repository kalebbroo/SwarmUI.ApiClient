using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Request contract for routing audio processing to a specific AudioLab provider.</summary>
/// <remarks>The generic entry point: <see cref="Arguments"/> is forwarded verbatim to the provider, so the accepted keys depend on the provider selected by <see cref="ProviderId"/>.</remarks>
public class AudioProcessRequest
{
    /// <summary>Identifier of the provider to run. Required.</summary>
    [JsonProperty("provider_id")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>Provider specific arguments forwarded untouched to the engine.</summary>
    [JsonProperty("args")]
    public Dictionary<string, object> Arguments { get; set; } = [];
}
