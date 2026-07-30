using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract reporting real-time AudioLab installation progress.</summary>
public class AudioInstallationProgressResponse : AudioLabResponse
{
    /// <summary>Overall progress percentage between 0 and 100.</summary>
    [JsonProperty("progress")]
    public int Progress { get; set; }

    /// <summary>Description of the current installation step.</summary>
    [JsonProperty("current_step")]
    public string? CurrentStep { get; set; }

    /// <summary>Package currently being installed.</summary>
    [JsonProperty("current_package")]
    public string? CurrentPackage { get; set; }

    /// <summary>Packages installed so far.</summary>
    [JsonProperty("completed_packages")]
    public string[] CompletedPackages { get; set; } = [];

    /// <summary>Whether the installation has finished.</summary>
    [JsonProperty("is_complete")]
    public bool IsComplete { get; set; }

    /// <summary>Whether the installation failed.</summary>
    [JsonProperty("has_error")]
    public bool HasError { get; set; }

    /// <summary>Failure detail when <see cref="HasError"/> is set.</summary>
    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
}
