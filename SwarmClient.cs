using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SwarmUI.ApiClient.Endpoints.Admin;
using SwarmUI.ApiClient.Endpoints.Backends;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Endpoints.User;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient;

/// <summary>Primary implementation of the SwarmUI API client, exposing organized endpoint groups and managing HTTP, WebSocket, and session infrastructure.</summary>
/// <remarks>The client is thread-safe for concurrent API requests and should be disposed when no longer needed. For usage patterns and implementation details, see the library documentation.</remarks>
public class SwarmClient : ISwarmClient
{
    /// <summary>Internal implementation data containing dependencies and infrastructure components using the Impl struct pattern.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests. May be owned by this instance or injected via DI.</summary>
        public HttpClient HttpClient;

        /// <summary>Indicates whether this instance owns the HttpClient and should dispose it.</summary>
        public bool DisposeHttpClient;

        /// <summary>Logger for client-level operations and health checks.</summary>
        public ILogger<SwarmClient> Logger;

        /// <summary>Session manager handling session lifecycle and caching across all endpoints.</summary>
        public ISessionManager SessionManager;

        /// <summary>HTTP communication layer wrapping HttpClient with SwarmUI-specific logic.</summary>
        public ISwarmHttpClient SwarmHttpClient;

        /// <summary>WebSocket communication layer for streaming operations.</summary>
        public ISwarmWebSocketClient WebSocketClient;
    }

    /// <summary>Internal implementation data. Do not use directly unless absolutely necessary.</summary>
    public Impl Internal;

    /// <summary>Access to text-to-image generation endpoints for streaming, status, and control operations.</summary>
    public IGenerationEndpoint Generation { get; }

    /// <summary>Access to model management endpoints for listing, editing, and managing models, LoRAs, and wildcards.</summary>
    public IModelsEndpoint Models { get; }

    /// <summary>Access to backend server management endpoints for listing, toggling, and restarting GPU backends.</summary>
    public IBackendsEndpoint Backends { get; }

    /// <summary>Access to preset management endpoints for creating, editing, duplicating, and deleting presets.</summary>
    public IPresetsEndpoint Presets { get; }

    /// <summary>Access to user data and settings endpoints for settings, API keys, and user-specific data.</summary>
    public IUserEndpoint User { get; }

    /// <summary>Access to administrative endpoints for user management, roles, server operations, and system administration.</summary>
    public IAdminEndpoint Admin { get; }

    /// <summary>Creates a new SwarmClient for standalone usage where the client owns its HttpClient instance.</summary>
    /// <param name="options">Configuration options for the client. Must not be null.</param>
    /// <param name="logger">Optional logger for client operations. Uses NullLogger if null.</param>
    /// <remarks>Creates and configures an internal HttpClient using SwarmClientOptions for simple, non-DI scenarios. See the README for usage examples.</remarks>
    public SwarmClient(SwarmClientOptions options, ILogger<SwarmClient>? logger = null) : this(CreateConfiguredHttpClient(options), options, logger, disposeHttpClient: true)
    {
    }

    /// <summary>Creates a new SwarmClient using an injected HttpClient from dependency injection.</summary>
    /// <param name="httpClient">
    /// HTTP client injected from DI container. Should be configured by IHttpClientFactory.
    /// Must not be null.
    /// </param>
    /// <param name="options">Configuration options for the client. Must not be null.</param>
    /// <param name="logger">Optional logger for client operations. Uses NullLogger if null.</param>
    /// <remarks>Designed for DI scenarios where HttpClient is managed by IHttpClientFactory and the DI container. The client configures the injected HttpClient with SwarmUI-specific settings but does not dispose it. See the README and CodingGuidelines.md for registration patterns.</remarks>
    public SwarmClient(HttpClient httpClient, SwarmClientOptions options, ILogger<SwarmClient>? logger = null) : this(httpClient, options, logger, disposeHttpClient: false)
    {
    }

    /// <summary>Private shared constructor implementing the initialization logic for HttpClient and infrastructure components.</summary>
    private SwarmClient(HttpClient httpClient, SwarmClientOptions options, ILogger<SwarmClient>? logger, bool disposeHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(options.BaseUrl);
        }
        httpClient.Timeout = options.HttpTimeout;
        ConfigureAuthorizationHeader(httpClient, options);
        Internal.HttpClient = httpClient;
        Internal.DisposeHttpClient = disposeHttpClient;
        Internal.Logger = logger ?? NullLogger<SwarmClient>.Instance;
        SwarmHttpClient? swarmHttpClient = null;
        Internal.SessionManager = new SessionManager(httpClientFactory: () => swarmHttpClient!, logger: null);
        swarmHttpClient = new SwarmHttpClient(httpClient, Internal.SessionManager, logger: null);
        Internal.SwarmHttpClient = swarmHttpClient;
        Internal.WebSocketClient = new SwarmWebSocketClient(options, Internal.SessionManager, logger: null);
        Generation = new GenerationEndpoint(Internal.SwarmHttpClient, Internal.WebSocketClient, Internal.SessionManager, logger: null);
        Models = new ModelsEndpoint(Internal.SwarmHttpClient, Internal.WebSocketClient, Internal.SessionManager, logger: null);
        Backends = new BackendsEndpoint(Internal.SwarmHttpClient, Internal.SessionManager, logger: null);
        Presets = new PresetsEndpoint(Internal.SwarmHttpClient, Internal.SessionManager, logger: null);
        User = new UserEndpoint(Internal.SwarmHttpClient, Internal.SessionManager, logger: null);
        Admin = new AdminEndpoint(Internal.SwarmHttpClient, Internal.SessionManager, logger: null);
        Internal.Logger.LogInformation("SwarmClient initialized for {BaseUrl}", options.BaseUrl);
    }

    /// <summary>Creates and configures an HttpClient for standalone usage.</summary>
    private static HttpClient CreateConfiguredHttpClient(SwarmClientOptions options, HttpMessageHandler? handler = null)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        HttpClient httpClient = handler != null ? new HttpClient(handler) : new HttpClient();
        httpClient.BaseAddress = new Uri(options.BaseUrl);
        httpClient.Timeout = options.HttpTimeout;
        ConfigureAuthorizationHeader(httpClient, options);
        return httpClient;
    }

    /// <summary>Configures the Authorization header on the provided HttpClient based on SwarmClientOptions.</summary>
    /// <param name="httpClient">The HTTP client to configure.</param>
    /// <param name="options">The options containing the authorization value.</param>
    internal static void ConfigureAuthorizationHeader(HttpClient httpClient, SwarmClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (!string.IsNullOrEmpty(options.Authorization))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(options.Authorization);
        }
    }

    /// <summary>Performs a health check on the SwarmUI server to verify connectivity and response time.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Health status information including reachability, response time, and error details.</returns>
    /// <remarks>Uses a lightweight session creation call to validate basic server health. See CodingGuidelines.md for behavior details.</remarks>
    public async Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            Internal.Logger.LogDebug("Performing health check");
            string sessionId = await Internal.SessionManager.GetOrCreateSessionAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            Internal.Logger.LogInformation("Health check successful - server is healthy (response time: {ResponseTime}ms)", stopwatch.ElapsedMilliseconds);
            return new HealthCheckResponse
            {
                IsHealthy = true,
                ResponseTime = stopwatch.Elapsed,
                Error = null,
                ServerVersion = null // TODO: Could be populated from API response
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Internal.Logger.LogWarning(ex, "Health check failed after {ResponseTime}ms: {Error}", stopwatch.ElapsedMilliseconds, ex.Message);
            return new HealthCheckResponse
            {
                IsHealthy = false,
                ResponseTime = stopwatch.Elapsed,
                Error = ex.Message,
                ServerVersion = null
            };
        }
    }

    /// <summary>Disposes of all resources used by this client.</summary>
    /// <remarks>Closes active WebSocket connections, disposes owned HttpClient instances, and releases session resources. Disposal is safe to call multiple times.</remarks>
    public async ValueTask DisposeAsync()
    {
        Internal.Logger.LogDebug("Disposing SwarmClient");
        try
        {
            if (Internal.WebSocketClient is not null)
            {
                await Internal.WebSocketClient.DisconnectAllAsync().ConfigureAwait(false);
            }
            if (Internal.SessionManager is IDisposable disposableSessionManager)
            {
                disposableSessionManager.Dispose();
            }
            if (Internal.DisposeHttpClient && Internal.HttpClient != null)
            {
                Internal.HttpClient.Dispose();
                Internal.Logger.LogDebug("Disposed owned HttpClient");
            }
            Internal.Logger.LogInformation("SwarmClient disposed successfully");
        }
        catch (Exception ex)
        {
            Internal.Logger.LogError(ex, "Error during SwarmClient disposal");
        }
    }
}
