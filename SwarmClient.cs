using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Endpoints.Admin;
using SwarmUI.ApiClient.Endpoints.Backends;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Endpoints.User;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient;

/// <summary>Primary implementation of the SwarmUI API client.</summary>
/// <remarks>Intended lifetime is one instance per SwarmUI server for the life of the process (register as a singleton in DI). Sessions are pooled per key and refresh transparently when the server rejects them, so a SwarmUI restart never requires restarting the consuming application. Thread-safe for concurrent use.</remarks>
public class SwarmClient : ISwarmClient
{
    private readonly SwarmClientOptions _options;
    private readonly ILogger<SwarmClient> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Func<HttpClient> _httpClientProvider;
    private readonly HttpClient? _ownedHttpClient;
    private readonly SessionManager _sessionManager;
    private readonly SwarmHttpClient _swarmHttpClient;
    private readonly SwarmWebSocketClient _webSocketClient;
    private int _disposed;

    /// <summary>Access to text-to-image generation endpoints (default session).</summary>
    public IGenerationEndpoint Generation { get; }

    /// <summary>Access to model management endpoints (default session).</summary>
    public IModelsEndpoint Models { get; }

    /// <summary>Access to backend server management endpoints (default session).</summary>
    public IBackendsEndpoint Backends { get; }

    /// <summary>Access to preset management endpoints (default session).</summary>
    public IPresetsEndpoint Presets { get; }

    /// <summary>Access to user data and settings endpoints (default session).</summary>
    public IUserEndpoint User { get; }

    /// <summary>Access to administrative endpoints (default session).</summary>
    public IAdminEndpoint Admin { get; }

    /// <summary>Access to endpoints added by SwarmUI server extensions (default session).</summary>
    public ISwarmExtensions Extensions { get; }

    /// <inheritdoc />
    public ISessionManager Sessions => _sessionManager;

    /// <summary>Creates a standalone SwarmClient that owns its HTTP resources.</summary>
    /// <param name="options">Configuration options. Must not be null.</param>
    /// <param name="loggerFactory">Optional logger factory for client and endpoint loggers.</param>
    /// <remarks>The owned connection handler uses a pooled connection lifetime so DNS changes are picked up — safe to hold as a process-lifetime singleton.</remarks>
    public SwarmClient(SwarmClientOptions options, ILoggerFactory? loggerFactory = null)
        : this(options, httpClientProvider: null, loggerFactory)
    {
    }

    /// <summary>Creates a SwarmClient over an externally managed HttpClient supply (DI hosts).</summary>
    /// <param name="options">Configuration options. Must not be null.</param>
    /// <param name="httpClientProvider">Called per request to obtain an HttpClient; wire this to IHttpClientFactory.CreateClient so handler rotation works. When null, the client creates and owns its own HttpClient.</param>
    /// <param name="loggerFactory">Optional logger factory for client and endpoint loggers.</param>
    /// <remarks>The provider's clients must have <c>BaseAddress</c>, timeout, and auth configured — use <c>SwarmClientServiceCollectionExtensions.AddSwarmClient</c>, which does this automatically.</remarks>
    public SwarmClient(SwarmClientOptions options, Func<HttpClient>? httpClientProvider, ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<SwarmClient>() ?? NullLogger<SwarmClient>.Instance;
        if (httpClientProvider is null)
        {
            _ownedHttpClient = CreateOwnedHttpClient(options);
            _httpClientProvider = () => _ownedHttpClient;
        }
        else
        {
            _httpClientProvider = httpClientProvider;
        }
        SwarmHttpClient? swarmHttpClient = null;
        _sessionManager = new SessionManager(httpClientFactory: () => swarmHttpClient!, options, loggerFactory?.CreateLogger<SessionManager>());
        swarmHttpClient = new SwarmHttpClient(_httpClientProvider, _sessionManager, options, loggerFactory?.CreateLogger<SwarmHttpClient>());
        _swarmHttpClient = swarmHttpClient;
        _webSocketClient = new SwarmWebSocketClient(options, _sessionManager, loggerFactory?.CreateLogger<SwarmWebSocketClient>());
        Generation = new GenerationEndpoint(_swarmHttpClient, _webSocketClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<GenerationEndpoint>());
        Models = new ModelsEndpoint(_swarmHttpClient, _webSocketClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<ModelsEndpoint>());
        Backends = new BackendsEndpoint(_swarmHttpClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<BackendsEndpoint>());
        Presets = new PresetsEndpoint(_swarmHttpClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<PresetsEndpoint>());
        User = new UserEndpoint(_swarmHttpClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<UserEndpoint>());
        Admin = new AdminEndpoint(_swarmHttpClient, SwarmSessionKeys.Default, loggerFactory?.CreateLogger<AdminEndpoint>());
        Extensions = new SwarmExtensions(_swarmHttpClient, _webSocketClient, SwarmSessionKeys.Default, loggerFactory);
        _logger.LogInformation("SwarmClient initialized for {BaseUrl}", options.NormalizedBaseUrl);
    }

    /// <summary>Creates the owned HttpClient for standalone usage, with pooled connections so DNS changes are honored.</summary>
    private static HttpClient CreateOwnedHttpClient(SwarmClientOptions options)
    {
        SocketsHttpHandler handler = new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        HttpClient httpClient = new(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(options.NormalizedBaseUrl),
            Timeout = options.HttpTimeout
        };
        ConfigureAuth(httpClient, options);
        return httpClient;
    }

    /// <summary>Applies header or cookie authentication to an HttpClient per the options. Safe to call only before the client's first request.</summary>
    public static void ConfigureAuth(HttpClient httpClient, SwarmClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrEmpty(options.Authorization))
        {
            return;
        }
        if (options.AuthMode == SwarmAuthMode.SwarmTokenCookie)
        {
            httpClient.DefaultRequestHeaders.Remove("Cookie");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", $"swarm_token={options.Authorization}");
        }
        else
        {
            string headerName = string.IsNullOrWhiteSpace(options.AuthorizationHeaderName) ? "Authorization" : options.AuthorizationHeaderName;
            httpClient.DefaultRequestHeaders.Remove(headerName);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(headerName, options.Authorization);
        }
    }

    /// <inheritdoc />
    public ISwarmClient ForSession(string sessionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionKey);
        return new SessionScopedClient(this, sessionKey);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            // A real sessionless probe every call — never cached client state.
            JObject response = await _swarmHttpClient.PostJsonAsync<JObject>("GetNewSession", payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogDebug("Health check successful ({ResponseTime}ms)", stopwatch.ElapsedMilliseconds);
            return new HealthCheckResponse
            {
                IsHealthy = true,
                ResponseTime = stopwatch.Elapsed,
                Error = null,
                ServerVersion = response["version"]?.ToString(),
                ServerId = response["server_id"]?.ToString()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check failed after {ResponseTime}ms: {Error}", stopwatch.ElapsedMilliseconds, ex.Message);
            return new HealthCheckResponse
            {
                IsHealthy = false,
                ResponseTime = stopwatch.Elapsed,
                Error = ex.Message,
                ServerVersion = null,
                ServerId = null
            };
        }
    }

    /// <inheritdoc />
    public Task DisconnectAllAsync(CancellationToken cancellationToken = default) => _webSocketClient.DisconnectAllAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _logger.LogDebug("Disposing SwarmClient");
        try
        {
            await _webSocketClient.DisconnectAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disconnecting WebSockets during disposal");
        }
        _sessionManager.Dispose();
        _ownedHttpClient?.Dispose();
        GC.SuppressFinalize(this);
        _logger.LogInformation("SwarmClient disposed");
    }

    /// <summary>Lightweight per-session-key view over a root <see cref="SwarmClient"/>. Shares all connections and resources with the root; disposal is a no-op.</summary>
    private sealed class SessionScopedClient : ISwarmClient
    {
        private readonly SwarmClient _root;

        public IGenerationEndpoint Generation { get; }
        public IModelsEndpoint Models { get; }
        public IBackendsEndpoint Backends { get; }
        public IPresetsEndpoint Presets { get; }
        public IUserEndpoint User { get; }
        public IAdminEndpoint Admin { get; }
        public ISwarmExtensions Extensions { get; }
        public ISessionManager Sessions => _root.Sessions;

        public SessionScopedClient(SwarmClient root, string sessionKey)
        {
            _root = root;
            Generation = new GenerationEndpoint(root._swarmHttpClient, root._webSocketClient, sessionKey, root._loggerFactory?.CreateLogger<GenerationEndpoint>());
            Models = new ModelsEndpoint(root._swarmHttpClient, root._webSocketClient, sessionKey, root._loggerFactory?.CreateLogger<ModelsEndpoint>());
            Backends = new BackendsEndpoint(root._swarmHttpClient, sessionKey, root._loggerFactory?.CreateLogger<BackendsEndpoint>());
            Presets = new PresetsEndpoint(root._swarmHttpClient, sessionKey, root._loggerFactory?.CreateLogger<PresetsEndpoint>());
            User = new UserEndpoint(root._swarmHttpClient, sessionKey, root._loggerFactory?.CreateLogger<UserEndpoint>());
            Admin = new AdminEndpoint(root._swarmHttpClient, sessionKey, root._loggerFactory?.CreateLogger<AdminEndpoint>());
            Extensions = new SwarmExtensions(root._swarmHttpClient, root._webSocketClient, sessionKey, root._loggerFactory);
        }

        public ISwarmClient ForSession(string sessionKey) => _root.ForSession(sessionKey);

        public Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default) => _root.GetHealthAsync(cancellationToken);

        public Task DisconnectAllAsync(CancellationToken cancellationToken = default) => _root.DisconnectAllAsync(cancellationToken);

        /// <summary>No-op: session views do not own resources. Dispose the root client instead.</summary>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
