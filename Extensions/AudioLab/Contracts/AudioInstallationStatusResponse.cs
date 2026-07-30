using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract describing which AudioLab providers are installed on the server.</summary>
/// <remarks>"Installed" means the provider's models are registered with the running audio backend.</remarks>
public class AudioInstallationStatusResponse : AudioLabResponse
{
    /// <summary>Whether the in-process audio engine bridge is available.</summary>
    [JsonProperty("engine_available")]
    public bool EngineAvailable { get; set; }

    /// <summary>Whether the audio engine is available and reporting ready.</summary>
    [JsonProperty("engine_ready")]
    public bool EngineReady { get; set; }

    /// <summary>Install state per provider, keyed by provider identifier.</summary>
    [JsonProperty("providers")]
    public Dictionary<string, bool> Providers { get; set; } = [];
}
