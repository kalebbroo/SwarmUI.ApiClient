using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Endpoints.Backends;

/// <summary>Implements backend server management endpoints.</summary>
/// <remarks>Provides HTTP-based operations for listing, adding, toggling, and restarting GPU backends. See the SwarmUI Backends API documentation for detailed behavior and fields.</remarks>
public class BackendsEndpoint : IBackendsEndpoint
{
    /// <summary>Internal implementation data containing endpoint dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for backend API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>Session manager used indirectly via the HTTP client.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for backend endpoint operations.</summary>
        public ILogger<BackendsEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; typical usage should go through the public methods.</summary>
    public Impl Internal;

    /// <summary>Creates a new BackendsEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public BackendsEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<BackendsEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<BackendsEndpoint>.Instance;
    }

    /// <summary>Lists configured backend servers with their status and configuration.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of all backends with their current status and details.</returns>
    /// <remarks>Returns SwarmUI's ListBackends response; see <see cref="BackendsListResponse"/> for the response shape.</remarks>
    public async Task<BackendsListResponse> ListBackendsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing backend servers");
        BackendsListResponse response = await Internal.HttpClient.PostJsonAsync<BackendsListResponse>("ListBackends", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {BackendCount} backend servers", response.Backends?.Count ?? 0);
        return response;
    }

    /// <summary>Adds a new backend server to the SwarmUI backend pool.</summary>
    /// <param name="type">
    /// Backend type (e.g., "ComfyUI"). Must match a backend type supported by SwarmUI.
    /// Most installations use "ComfyUI" as the backend type.
    /// </param>
    /// <param name="address">
    /// Network address of the backend server (e.g., "http://localhost:7820").
    /// Must be reachable from the SwarmUI server. Can be local or remote.
    /// </param>
    /// <param name="name">
    /// Custom display name for this backend (e.g., "GPU 1 - RTX 4090").
    /// Used in UI and logs. Should be descriptive to help identify the backend.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming backend was added successfully.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if type or address is null or empty.
    /// </exception>
    /// <remarks>See the SwarmUI Backends API documentation for detailed behavior and fields.</remarks>
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
        Internal.Logger.LogDebug("Adding new backend: {Type} at {Address}", type, address);
        JObject payload = new()
        {
            ["type"] = type,
            ["address"] = address,
            ["name"] = name ?? string.Empty
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AddNewBackend", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Backend added successfully: {Name} ({Type})", name, type);
        return response;
    }

    /// <summary>Toggles a backend server on or off.</summary>
    /// This is reversible - toggling again re-enables the backend.
    /// <param name="backendId">
    /// Unique identifier of the backend to toggle.
    /// Get backend IDs from ListBackendsAsync response.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming the toggle operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if backendId is null or empty.
    /// </exception>
    /// <remarks>Disables a backend so it stops receiving new jobs while existing jobs complete, or re-enables it. Models remain loaded and configuration is preserved.</remarks>
    public async Task ToggleBackendAsync(string backendId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(backendId))
        {
            throw new ArgumentException("Backend ID cannot be null or empty", nameof(backendId));
        }
        Internal.Logger.LogDebug("Toggling backend: {BackendId}", backendId);
        JObject payload = new()
        {
            ["backend_id"] = backendId
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ToggleBackend", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Backend toggled successfully: {BackendId}", backendId);
    }

    /// <summary>Restarts backend servers to recover from errors or apply configuration changes.</summary>
    /// <param name="backendId">
    /// Optional backend ID to restart. If null or empty, restarts all backends.
    /// Get backend IDs from ListBackendsAsync response.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming the restart operation.</returns>
    /// <remarks>Restarting one or all backends may briefly interrupt generation capacity but is useful for recovery, memory cleanup, and applying configuration changes. See the SwarmUI Backends API documentation for lifecycle details.</remarks>
    public async Task RestartBackendsAsync(string? backendId = null, CancellationToken cancellationToken = default)
    {
        bool restartingAll = string.IsNullOrEmpty(backendId);
        Internal.Logger.LogDebug("Restarting backends {Scope}", restartingAll ? "(all)" : $": {backendId}");
        JObject payload = new();
        if (!restartingAll)
        {
            payload["backend_id"] = backendId;
        }
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("RestartBackends", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Backends restarted successfully {Scope}", restartingAll ? "(all)" : $": {backendId}");
    }
}
