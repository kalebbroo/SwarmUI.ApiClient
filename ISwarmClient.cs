using System;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Endpoints.Admin;
using SwarmUI.ApiClient.Endpoints.Backends;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Endpoints.LLM;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Endpoints.User;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient;

/// <summary>Primary interface for interacting with the SwarmUI API and accessing organized endpoint groups.</summary>
/// <remarks>Implements IAsyncDisposable to clean up HTTP and WebSocket resources. For implementation details, see the library documentation.</remarks>
public interface ISwarmClient : IAsyncDisposable
{
    /// <summary>Access to text-to-image generation endpoints.</summary>
    /// <remarks>Provides streaming generation via WebSocket and status/control operations.</remarks>
    IGenerationEndpoint Generation { get; }

    /// <summary>Access to model management endpoints.</summary>
    /// <remarks>Provides listing, downloading, editing, and metadata operations for models, LoRAs, and wildcards.</remarks>
    IModelsEndpoint Models { get; }

    /// <summary>Access to backend server management endpoints.</summary>
    /// <remarks>Provides listing, adding, toggling, and restarting backend GPU servers.</remarks>
    IBackendsEndpoint Backends { get; }

    /// <summary>Access to preset management endpoints.</summary>
    /// <remarks>Provides creating, editing, duplicating, and deleting parameter presets.</remarks>
    IPresetsEndpoint Presets { get; }

    /// <summary>Access to user data and settings endpoints.</summary>
    /// <remarks>Provides getting/changing user settings, API keys, and user data.</remarks>
    IUserEndpoint User { get; }

    /// <summary>Access to administrative endpoints.</summary>
    /// <remarks>Provides user management, role management, server operations, and system management. Requires administrative permissions on the SwarmUI server.</remarks>
    IAdminEndpoint Admin { get; }

    /// <summary>Access to LLM endpoints for text processing and enhancement.</summary>
    /// <remarks>
    /// NOTE: LLM endpoints are Hartsy-specific extensions and not part of standard SwarmUI.
    /// Provides access to language model features like MagicPrompt for text enhancement.
    /// </remarks>
    ILLMEndpoint LLM { get; }

    /// <summary>Performs a health check on the SwarmUI server.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Health status information including connectivity, response time, and server details.</returns>
    /// <remarks>Verifies basic connectivity and returns summarized server health information.</remarks>
    Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    // TODO:
    // - Task<ServerInfo> GetServerInfoAsync() - Get server version, capabilities, etc.
    // - Task DisconnectAllAsync() - Explicitly disconnect all WebSocket connections
    // - IAsyncEnumerable<string> GetServerLogsAsync() - Stream server logs if API supports it
}
