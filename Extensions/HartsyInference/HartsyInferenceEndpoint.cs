using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Extensions.HartsyInference;

/// <summary>Implements the endpoints added by the HartsyInference backend SwarmUI extension.</summary>
/// <remarks>Requires the SwarmUI-HartsyInference-Backend extension on the target server. None of these endpoints exist in stock SwarmUI.</remarks>
public class HartsyInferenceEndpoint : IHartsyInferenceEndpoint
{
    /// <summary>Metadata for the HartsyInference extension backing this endpoint group.</summary>
    public static readonly SwarmExtensionInfo ExtensionInfo = new()
    {
        Name = "HartsyInference",
        DisplayName = "HartsyInference Backend",
        RepositoryUrl = "https://github.com/HartsyAI/SwarmUI-HartsyInference-Backend",
        Endpoints = new string[]
        {
            "HartsyInferenceGetSupportedArchs",
            "HartsyInferenceProbeModel",
            "HartsyInferenceListLoadedPipelines",
            "HartsyInferenceGetDeviceInfo",
            "HartsyInferenceClearCache"
        }
    };

    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<HartsyInferenceEndpoint> _logger;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new HartsyInferenceEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public HartsyInferenceEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<HartsyInferenceEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<HartsyInferenceEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<SupportedArchsResponse> GetSupportedArchitecturesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing HartsyInference supported architectures");
        SupportedArchsResponse response = await _httpClient.PostJsonAsync<SupportedArchsResponse>("HartsyInferenceGetSupportedArchs", new JObject(), _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("HartsyInference reports {Supported} supported and {Pending} pending architectures", response.Supported.Count, response.Pending.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<ProbeModelResponse> ProbeModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        _logger.LogDebug("Probing HartsyInference support for model '{ModelName}'", modelName);
        ProbeModelResponse response = await _httpClient.PostJsonAsync<ProbeModelResponse>("HartsyInferenceProbeModel", new JObject { ["model_name"] = modelName }, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("HartsyInference reports '{ModelName}' as {State}", modelName, response.State);
        return response;
    }

    /// <inheritdoc />
    public async Task<LoadedPipelinesResponse> ListLoadedPipelinesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing loaded HartsyInference pipelines");
        return await _httpClient.PostJsonAsync<LoadedPipelinesResponse>("HartsyInferenceListLoadedPipelines", new JObject(), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeviceInfoResponse> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting HartsyInference device info");
        return await _httpClient.PostJsonAsync<DeviceInfoResponse>("HartsyInferenceGetDeviceInfo", new JObject(), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ClearCacheResponse> ClearCacheAsync(int backendId = -1, bool freeSystemRam = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing HartsyInference pipeline cache for backend {BackendId}", backendId);
        ClearCacheResponse response = await _httpClient.PostJsonAsync<ClearCacheResponse>("HartsyInferenceClearCache",
            new JObject { ["backend_id"] = backendId, ["free_system_ram"] = freeSystemRam }, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cleared the HartsyInference pipeline cache on {Count} backends", response.BackendsCleared);
        return response;
    }
}
