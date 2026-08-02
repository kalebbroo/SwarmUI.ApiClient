using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Endpoints.Backends;

/// <summary>Implements backend server management endpoints.</summary>
/// <remarks>Provides HTTP-based operations for listing, adding, toggling, and restarting GPU backends. Server-reported errors surface as <see cref="Exceptions.SwarmException"/> via the HTTP layer's centralized error mapping.</remarks>
public class BackendsEndpoint : IBackendsEndpoint
{
    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<BackendsEndpoint> _logger;

    /// <summary>Creates a new BackendsEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public BackendsEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<BackendsEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<BackendsEndpoint>.Instance;
    }

    /// <summary>Lists configured backend servers with their status and configuration.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of all backends with their current status and details.</returns>
    public async Task<BackendsListResponse> ListBackendsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing backend servers");
        BackendsListResponse response = await _httpClient.PostJsonAsync<BackendsListResponse>("ListBackends", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {BackendCount} backend servers", response.Backends?.Count ?? 0);
        return response;
    }

    /// <summary>Adds a new backend server to the SwarmUI backend pool.</summary>
    /// <param name="type">Backend type (e.g., "ComfyUI"). Must match a backend type supported by SwarmUI.</param>
    /// <param name="address">Network address of the backend server (e.g., "http://localhost:7820").</param>
    /// <param name="name">Custom display name for this backend (e.g., "GPU 1 - RTX 4090").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming backend was added successfully.</returns>
    /// <exception cref="ArgumentException">Thrown if type or address is null or empty.</exception>
    public async Task<JObject> AddNewBackendAsync(string type, string address, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(type))
        {
            throw new ArgumentException("Backend type cannot be null or empty", nameof(type));
        }
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException("Backend address cannot be null or empty", nameof(address));
        }
        _logger.LogDebug("Adding new backend: {Type} at {Address}", type, address);
        JObject payload = new()
        {
            ["type"] = type,
            ["address"] = address,
            ["name"] = name ?? string.Empty
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("AddNewBackend", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Backend added successfully: {Name} ({Type})", name, type);
        return response;
    }

    /// <summary>Toggles a backend server on or off. Reversible — toggling again re-enables the backend.</summary>
    /// <param name="backendId">Unique identifier of the backend to toggle (from ListBackendsAsync).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentException">Thrown if backendId is null or empty.</exception>
    public async Task ToggleBackendAsync(string backendId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(backendId))
        {
            throw new ArgumentException("Backend ID cannot be null or empty", nameof(backendId));
        }
        _logger.LogDebug("Toggling backend: {BackendId}", backendId);
        JObject payload = new()
        {
            ["backend_id"] = backendId
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("ToggleBackend", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Backend toggled successfully: {BackendId}", backendId);
    }

    /// <summary>Restarts backend servers to recover from errors or apply configuration changes.</summary>
    /// <param name="backendId">Optional backend ID to restart. If null or empty, restarts all backends.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task RestartBackendsAsync(string? backendId = null, CancellationToken cancellationToken = default)
    {
        bool restartingAll = string.IsNullOrEmpty(backendId);
        _logger.LogDebug("Restarting backends {Scope}", restartingAll ? "(all)" : $": {backendId}");
        JObject payload = new();
        if (!restartingAll)
        {
            payload["backend_id"] = backendId;
        }
        JObject _ = await _httpClient.PostJsonAsync<JObject>("RestartBackends", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Backends restarted successfully {Scope}", restartingAll ? "(all)" : $": {backendId}");
    }
}
