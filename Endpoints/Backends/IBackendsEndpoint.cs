using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.Backends;

/// <summary>Provides access to SwarmUI backend server management endpoints.</summary>
/// <remarks>Backends are GPU workers (typically ComfyUI instances) that process generation requests. Use these endpoints to list, add, toggle, and restart backends. See the SwarmUI Backends API documentation for full details.</remarks>
public interface IBackendsEndpoint
{
    /// <summary>
    /// Lists configured backend servers with their status and basic configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of backends with their current status and configuration.</returns>
    Task<BackendsListResponse> ListBackendsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new backend server to the SwarmUI backend pool.</summary>
    /// <param name="type">
    /// Backend type (e.g., "ComfyUI"). Must match a backend type supported by SwarmUI.
    /// </param>
    /// <param name="address">
    /// Network address of the backend server (e.g., "http://localhost:7820").
    /// </param>
    /// <param name="name">
    /// Custom display name for this backend (e.g., "GPU 1 - RTX 4090").
    /// Used in UI and logs to identify this backend.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming backend was added successfully.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if type or address is null or empty.
    /// </exception>
    /// <remarks>Maps to SwarmUI's backend-add endpoint. The backend is validated, persisted in configuration, and will auto-start on server restart.</remarks>
    Task<JObject> AddNewBackendAsync(string type, string address, string name, CancellationToken cancellationToken = default);

    /// <summary>Toggles a backend server on or off.</summary>
    /// <param name="backendId">
    /// Unique identifier of the backend to toggle.
    /// Get backend IDs from ListBackendsAsync.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming the toggle operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if backendId is null or empty.
    /// </exception>
    /// <remarks>Temporarily disables or re-enables a backend without deleting its configuration. Disabled backends stop receiving new jobs while existing jobs complete and models remain loaded.</remarks>
    Task ToggleBackendAsync(string backendId, CancellationToken cancellationToken = default);

    /// <summary>Restarts backend servers to recover from errors or apply configuration changes.</summary>
    /// <param name="backendId">
    /// Optional backend ID to restart. If null, restarts all backends.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Response confirming the restart operation.</returns>
    /// <remarks>Restarting one or all backends may briefly interrupt generation capacity but is useful for recovery, memory cleanup, and applying configuration changes.</remarks>
    Task RestartBackendsAsync(string? backendId = null, CancellationToken cancellationToken = default);
}
