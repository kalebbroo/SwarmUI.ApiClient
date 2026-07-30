using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>A single streamed frame from an AudioLab engine install operation.</summary>
/// <remarks>Install endpoints emit progress frames carrying <see cref="Info"/>, per-model completion frames carrying <see cref="ModelId"/> together with <see cref="ModelDone"/> or <see cref="ModelFailed"/>, and one terminal frame carrying <see cref="Success"/> or <see cref="Error"/>.</remarks>
public class AudioEngineInstallUpdate
{
    /// <summary>Progress text for the current install step.</summary>
    [JsonProperty("info")]
    public string? Info { get; set; }

    /// <summary>Set on the terminal frame when the operation finished successfully.</summary>
    [JsonProperty("success")]
    public bool? Success { get; set; }

    /// <summary>Failure detail. Present instead of <see cref="Success"/> when the operation failed.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>Human readable summary on the terminal frame.</summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    /// <summary>Provider the operation targeted.</summary>
    [JsonProperty("provider_id")]
    public string? ProviderId { get; set; }

    /// <summary>Model this frame refers to, on per-model frames.</summary>
    [JsonProperty("model_id")]
    public string? ModelId { get; set; }

    /// <summary>Set when the model named by <see cref="ModelId"/> installed successfully.</summary>
    [JsonProperty("model_done")]
    public bool? ModelDone { get; set; }

    /// <summary>Set when the model named by <see cref="ModelId"/> failed to install.</summary>
    [JsonProperty("model_failed")]
    public bool? ModelFailed { get; set; }

    /// <summary>Number of models installed, on the terminal frame of a bulk install.</summary>
    [JsonProperty("installed")]
    public int? Installed { get; set; }

    /// <summary>Number of models the bulk install attempted.</summary>
    [JsonProperty("total")]
    public int? Total { get; set; }

    /// <summary>Whether this frame is the terminal frame of the operation.</summary>
    [JsonIgnore]
    public bool IsTerminal => Success.HasValue || Error is not null;
}
