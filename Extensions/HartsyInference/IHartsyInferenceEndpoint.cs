using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;

namespace SwarmUI.ApiClient.Extensions.HartsyInference;

/// <summary>Provides access to the endpoints added by the HartsyInference backend SwarmUI extension.</summary>
/// <remarks>Requires the SwarmUI-HartsyInference-Backend extension on the target server. None of these endpoints
/// exist in stock SwarmUI. The two admin endpoints need a separate, higher permission than the two capability ones.</remarks>
public interface IHartsyInferenceEndpoint : ISwarmExtensionEndpoint
{
    /// <summary>Lists the architectures the engine dispatches, the ones it cannot run yet and why, and per architecture the composition features and the samplers and schedulers it accepts.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Supported and pending architectures with their feature and sampling sets.</returns>
    /// <remarks>Calls <c>HartsyInferenceGetSupportedArchs</c>. Asked of the engine's registries at call time, so it cannot drift from what the pipeline will do.</remarks>
    Task<SupportedArchsResponse> GetSupportedArchitecturesAsync(CancellationToken cancellationToken = default);

    /// <summary>Asks whether the engine will run one specific model, and how it classified it.</summary>
    /// <param name="modelName">Model name as <c>ListModels</c> spells it.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The model's architecture, compatibility class, and support state with a reason.</returns>
    /// <remarks>Calls <c>HartsyInferenceProbeModel</c>. Answers the per-checkpoint narrowing that <see cref="GetSupportedArchitecturesAsync"/> cannot.</remarks>
    Task<ProbeModelResponse> ProbeModelAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>Lists each HartsyInference backend, the model it currently holds, and how many generations it has served.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>One entry per backend.</returns>
    /// <remarks>Calls <c>HartsyInferenceListLoadedPipelines</c>, which needs the extension's admin permission.</remarks>
    Task<LoadedPipelinesResponse> ListLoadedPipelinesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists where each HartsyInference backend places the stages of a generation across GPUs.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>One entry per backend.</returns>
    /// <remarks>Calls <c>HartsyInferenceGetDeviceInfo</c>, which needs the extension's admin permission.</remarks>
    Task<DeviceInfoResponse> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>Evicts the pipeline cache to free VRAM without restarting the server.</summary>
    /// <param name="backendId">Backend to clear, or -1 for every HartsyInference backend.</param>
    /// <param name="freeSystemRam">Also release the cached weights held in system memory.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>How many backends were cleared.</returns>
    /// <remarks>Calls <c>HartsyInferenceClearCache</c>, which needs the extension's admin permission. Unlike the
    /// other four this changes server state: a backend that had a model resident must reload it on its next
    /// generation.</remarks>
    Task<ClearCacheResponse> ClearCacheAsync(int backendId = -1, bool freeSystemRam = false, CancellationToken cancellationToken = default);
}
