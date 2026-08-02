using System;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Endpoints.Admin;
using SwarmUI.ApiClient.Endpoints.Backends;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Endpoints.User;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient;

/// <summary>Primary interface for interacting with the SwarmUI API and accessing organized endpoint groups.</summary>
/// <remarks>Every endpoint group exposed directly on this interface is part of stock SwarmUI. Endpoints that depend on a SwarmUI server extension live under <see cref="Extensions"/> instead.
/// SwarmUI scopes generation queues, status counters, and interrupts to a session; multi-tenant hosts should route each logical user through <see cref="ForSession"/> so users get independent sessions. Single-user consumers can ignore sessions entirely — everything defaults to one pooled session.</remarks>
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

    /// <summary>Access to endpoints added by SwarmUI server extensions, grouped one property per extension.</summary>
    /// <remarks>Nothing under this property is part of stock SwarmUI. Each extension must be installed and configured on the target server before its endpoints will respond. See <c>Extensions/README.md</c> for the supported extension list.</remarks>
    ISwarmExtensions Extensions { get; }

    /// <summary>The session pool backing this client — inspect, refresh, or invalidate pooled sessions.</summary>
    ISessionManager Sessions { get; }

    /// <summary>Returns a view of this client whose calls all authenticate with the pooled session for <paramref name="sessionKey"/> (e.g. an application user id).</summary>
    /// <remarks>Because SwarmUI scopes InterruptAll and generation queues per session, per-user keys give per-user interrupt and status isolation. The returned view shares this client's connections and resources; disposing it is a no-op — dispose the root client instead.</remarks>
    ISwarmClient ForSession(string sessionKey);

    /// <summary>Performs a real health check against the SwarmUI server (never returns cached state).</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Health status including connectivity, response time, server version, and server id.</returns>
    Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes all WebSocket connections currently tracked by this client (all session views included).</summary>
    Task DisconnectAllAsync(CancellationToken cancellationToken = default);
}
