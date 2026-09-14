using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;

/// <summary>Response from <c>HartsyInferenceGetSupportedArchs</c>.</summary>
/// <remarks>Answers what the engine will accept for a given architecture before a generation is attempted, so a UI
/// can hide controls the pipeline would refuse rather than surfacing the refusal after the user hits generate.</remarks>
public class SupportedArchsResponse
{
    /// <summary>True when the server answered successfully.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>Architecture ids the engine dispatches.</summary>
    [JsonProperty("supported")]
    public List<string> Supported { get; set; } = new List<string>();

    /// <summary>Architectures the engine knows but cannot run yet, mapped to the reason.</summary>
    [JsonProperty("pending")]
    public Dictionary<string, string> Pending { get; set; } = new Dictionary<string, string>();

    /// <summary>Composition features each architecture declares, keyed by architecture id.</summary>
    /// <remarks>Lowercase names such as <c>lora</c>, <c>refiner</c>, <c>img2img</c>, <c>inpaint</c>, <c>controlnet</c>,
    /// <c>ipadapter</c>, <c>regional</c>, <c>variationseed</c> and <c>seamlesstiling</c>. An empty list means the
    /// architecture declares none. Some families narrow this further per checkpoint file, which only the server can
    /// answer, so a feature listed here can still be refused for one specific model.</remarks>
    [JsonProperty("features")]
    public Dictionary<string, List<string>> Features { get; set; } = new Dictionary<string, List<string>>();

    /// <summary>Samplers and schedulers each architecture accepts, keyed by architecture id.</summary>
    [JsonProperty("sampling")]
    public Dictionary<string, ArchSamplingSupport> Sampling { get; set; } = new Dictionary<string, ArchSamplingSupport>();
}

/// <summary>The sampler and scheduler names one architecture accepts.</summary>
/// <remarks>Both lists empty means the family samples with its own solver and takes no selection at all, so a
/// sampler or scheduler sent for it would be refused.</remarks>
public class ArchSamplingSupport
{
    /// <summary>Accepted sampler names.</summary>
    [JsonProperty("samplers")]
    public List<string> Samplers { get; set; } = new List<string>();

    /// <summary>Accepted sigma-schedule names.</summary>
    [JsonProperty("schedulers")]
    public List<string> Schedulers { get; set; } = new List<string>();
}

/// <summary>Response from <c>HartsyInferenceProbeModel</c>: whether the engine will run one specific model, and how.</summary>
public class ProbeModelResponse
{
    /// <summary>True when the server answered successfully. This is not the same as the model being supported; read <see cref="State"/> for that.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>Model name as the server resolved it, file extension included.</summary>
    [JsonProperty("model_name")]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Architecture id the engine identified.</summary>
    [JsonProperty("arch_id")]
    public string? ArchId { get; set; }

    /// <summary>Compatibility class the engine identified.</summary>
    [JsonProperty("compat_class")]
    public string? CompatClass { get; set; }

    /// <summary>Whether the engine will run this model, for example <c>"supported"</c>.</summary>
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>Explanation of <see cref="State"/>, suitable for showing a user.</summary>
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}

/// <summary>Response from <c>HartsyInferenceListLoadedPipelines</c>.</summary>
public class LoadedPipelinesResponse
{
    /// <summary>True when the server answered successfully.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>One entry per HartsyInference backend.</summary>
    [JsonProperty("backends")]
    public List<PipelineBackendInfo> Backends { get; set; } = new List<PipelineBackendInfo>();
}

/// <summary>One HartsyInference backend and the model it currently holds.</summary>
public class PipelineBackendInfo : HartsyBackendPlacement
{
    /// <summary>Model this backend has loaded, empty when it holds none.</summary>
    [JsonProperty("current_model")]
    public string CurrentModel { get; set; } = string.Empty;

    /// <summary>How many generations this backend has served.</summary>
    [JsonProperty("usages")]
    public int Usages { get; set; }
}

/// <summary>Response from <c>HartsyInferenceGetDeviceInfo</c>.</summary>
public class DeviceInfoResponse
{
    /// <summary>True when the server answered successfully.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>One entry per HartsyInference backend, describing where each stage runs.</summary>
    [JsonProperty("devices")]
    public List<HartsyBackendPlacement> Devices { get; set; } = new List<HartsyBackendPlacement>();
}

/// <summary>Where one HartsyInference backend places each stage of a generation.</summary>
/// <remarks>An empty gpu id means that stage follows <see cref="GpuId"/> rather than being pinned separately.</remarks>
public class HartsyBackendPlacement
{
    /// <summary>SwarmUI's id for this backend.</summary>
    [JsonProperty("backend_id")]
    public int BackendId { get; set; }

    /// <summary>Backend status, for example <c>"RUNNING"</c>.</summary>
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Compute backend selection, for example <c>"auto"</c>.</summary>
    [JsonProperty("compute_backend")]
    public string ComputeBackend { get; set; } = string.Empty;

    /// <summary>Primary GPU this backend generates on.</summary>
    [JsonProperty("gpu_id")]
    public string GpuId { get; set; } = string.Empty;

    /// <summary>GPU pinned for text encoding.</summary>
    [JsonProperty("text_encoder_gpu_id")]
    public string TextEncoderGpuId { get; set; } = string.Empty;

    /// <summary>GPU pinned for VAE work.</summary>
    [JsonProperty("vae_gpu_id")]
    public string VaeGpuId { get; set; } = string.Empty;

    /// <summary>GPU pinned for the second CFG branch when guidance runs in parallel.</summary>
    [JsonProperty("cfg_parallel_gpu_id")]
    public string CfgParallelGpuId { get; set; } = string.Empty;

    /// <summary>GPU the diffusion transformer is sharded onto.</summary>
    [JsonProperty("dit_shard_gpu_id")]
    public string DitShardGpuId { get; set; } = string.Empty;

    /// <summary>GPU the language model is sharded onto.</summary>
    [JsonProperty("lm_shard_gpu_id")]
    public string LmShardGpuId { get; set; } = string.Empty;

    /// <summary>VRAM strategy, for example <c>"auto"</c>.</summary>
    [JsonProperty("vram_mode")]
    public string VramMode { get; set; } = string.Empty;
}

/// <summary>Response from <c>HartsyInferenceClearCache</c>.</summary>
public class ClearCacheResponse
{
    /// <summary>True when the server answered successfully.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>How many backends had their pipeline cache evicted.</summary>
    [JsonProperty("backends_cleared")]
    public int BackendsCleared { get; set; }
}
