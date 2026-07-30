using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract listing every AudioLab engine with install state, metadata, and models.</summary>
public class AudioEnginesResponse : AudioLabResponse
{
    /// <summary>Available engines.</summary>
    [JsonProperty("engines")]
    public AudioEngineInfo[] Engines { get; set; } = [];

    /// <summary>Status of the audio backend, or "NOT_FOUND" when no audio backend is registered.</summary>
    [JsonProperty("backend_status")]
    public string BackendStatus { get; set; } = string.Empty;
}

/// <summary>An AudioLab engine and its install state.</summary>
public class AudioEngineInfo
{
    /// <summary>Engine identifier, equal to the provider identifier.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the engine.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Engine category, for example TTS, STT, or AudioGen.</summary>
    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Group of engines that share an implementation.</summary>
    [JsonProperty("engine_group")]
    public string? EngineGroup { get; set; }

    /// <summary>Whether the engine is a legacy Docker based engine.</summary>
    [JsonProperty("requires_docker")]
    public bool RequiresDocker { get; set; }

    /// <summary>Whether the engine calls a remote API rather than running weights locally.</summary>
    [JsonProperty("is_api_provider")]
    public bool IsApiProvider { get; set; }

    /// <summary>Server settings key holding the API key for an API backed engine.</summary>
    [JsonProperty("api_key_settings_id")]
    public string? ApiKeySettingsId { get; set; }

    /// <summary>Whether the engine can run in the current build.</summary>
    [JsonProperty("platform_compatible")]
    public bool PlatformCompatible { get; set; }

    /// <summary>Explanation when <see cref="PlatformCompatible"/> is false.</summary>
    [JsonProperty("platform_note")]
    public string? PlatformNote { get; set; }

    /// <summary>Whether the engine is registered with the running audio backend.</summary>
    [JsonProperty("installed")]
    public bool Installed { get; set; }

    /// <summary>Whether the engine is installed but its weights are absent from disk.</summary>
    [JsonProperty("weights_missing")]
    public bool WeightsMissing { get; set; }

    /// <summary>Whether the engine runs in-process rather than as an external service.</summary>
    [JsonProperty("in_process")]
    public bool InProcess { get; set; }

    /// <summary>Whether the engine downloads its own weights on first use, making per-model installs meaningless.</summary>
    [JsonProperty("self_managed")]
    public bool SelfManaged { get; set; }

    /// <summary>Capability flags the engine supports, for example "tts_voice_ref".</summary>
    [JsonProperty("features")]
    public string[] Features { get; set; } = [];

    /// <summary>Models the engine exposes.</summary>
    [JsonProperty("models")]
    public AudioEngineModelInfo[] Models { get; set; } = [];
}

/// <summary>A single model variant offered by an AudioLab engine.</summary>
public class AudioEngineModelInfo
{
    /// <summary>Model identifier, used to install or remove one specific variant.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the model.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the model.</summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>Where the weights are downloaded from.</summary>
    [JsonProperty("source_url")]
    public string? SourceUrl { get; set; }

    /// <summary>License the weights are distributed under.</summary>
    [JsonProperty("license")]
    public string? License { get; set; }

    /// <summary>Approximate download size.</summary>
    [JsonProperty("estimated_size")]
    public string? EstimatedSize { get; set; }

    /// <summary>Approximate VRAM required to run the model.</summary>
    [JsonProperty("estimated_vram")]
    public string? EstimatedVram { get; set; }

    /// <summary>Name of this model in SwarmUI's model registry, for generating through the core generation pipeline.</summary>
    [JsonProperty("swarm_model")]
    public string? SwarmModel { get; set; }

    /// <summary>Whether this variant's weights are present on disk. API backed and self managed engines always report true.</summary>
    [JsonProperty("installed")]
    public bool Installed { get; set; }
}
