using System;
using Polly;

namespace SwarmUI.ApiClient;

/// <summary>How the client authenticates with the SwarmUI server.</summary>
public enum SwarmAuthMode
{
    /// <summary>Send the <see cref="SwarmClientOptions.Authorization"/> value in a request header named <see cref="SwarmClientOptions.AuthorizationHeaderName"/>. This is the mode for reverse-proxy or gateway auth in front of SwarmUI.</summary>
    Header,

    /// <summary>Send the <see cref="SwarmClientOptions.Authorization"/> value as a <c>swarm_token</c> cookie. This is SwarmUI's native account authentication for instances configured to require accounts (create tokens in the UI under User &gt; Swarm Auth Tokens).</summary>
    SwarmTokenCookie
}

/// <summary>Configuration options for the SwarmUI client.</summary>
/// <remarks>All properties have working defaults; the minimal configuration is <c>new SwarmClientOptions { BaseUrl = "http://localhost:7801" }</c>.</remarks>
public class SwarmClientOptions
{
    /// <summary>Base URL of the SwarmUI server instance.</summary>
    /// <remarks>Example: "http://localhost:7801". This should NOT include the /API/ path - that will be appended automatically. A trailing slash is tolerated and normalized away.</remarks>
    public string BaseUrl { get; set; } = "http://localhost:7801";

    /// <summary>Optional authorization value for authenticated requests. Interpreted per <see cref="AuthMode"/>: a header value, or a swarm_token cookie value.</summary>
    /// <remarks>Set to null or empty string if no authentication is required.</remarks>
    public string? Authorization { get; set; }

    /// <summary>HTTP header name used when <see cref="AuthMode"/> is <see cref="SwarmAuthMode.Header"/>.</summary>
    public string AuthorizationHeaderName { get; set; } = "Authorization";

    /// <summary>How <see cref="Authorization"/> is transmitted: as a request header (default) or as SwarmUI's native <c>swarm_token</c> cookie.</summary>
    public SwarmAuthMode AuthMode { get; set; } = SwarmAuthMode.Header;

    /// <summary>HTTP request timeout duration.</summary>
    /// <remarks>This applies to all HTTP requests (non-WebSocket operations). Default is 100 seconds to accommodate slow model loading.</remarks>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Maximum number of retry attempts for failed requests (HTTP requests and WebSocket connects).</summary>
    /// <remarks>Retried failures: invalid_session_id (after a transparent session refresh) always; transient transport errors when <see cref="RetryTransientHttpErrors"/> is enabled. Deterministic server-side errors are never retried.</remarks>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay for exponential backoff between retry attempts. Jitter is applied automatically.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Whether transient transport failures (connection errors, timeouts, HTTP 5xx) are retried in addition to session expiry.</summary>
    public bool RetryTransientHttpErrors { get; set; } = true;

    /// <summary>Optional fully custom resilience pipeline for HTTP requests. When set, it replaces the pipeline the client builds from <see cref="MaxRetryAttempts"/>/<see cref="RetryBaseDelay"/>/<see cref="RetryTransientHttpErrors"/> entirely.</summary>
    /// <remarks>The pipeline wraps the complete request attempt (session acquisition, POST, parse, error mapping). Throwing <c>SwarmSessionException</c> from an attempt indicates the session was invalidated and a retry will transparently acquire a fresh one.</remarks>
    public ResiliencePipeline? HttpResiliencePipeline { get; set; }

    /// <summary>Maximum number of transparent session-refresh cycles a single WebSocket stream will attempt when the server rejects the session id.</summary>
    /// <remarks>Matches the official SwarmUI JS client's retry cap. When exhausted, the stream throws <c>SwarmSessionException</c>.</remarks>
    public int SessionRefreshCap { get; set; } = 3;

    /// <summary>After a failed GetNewSession call, further session creation attempts for the same session key fail fast with the cached error for this duration.</summary>
    /// <remarks>Prevents a GetNewSession storm from thousands of concurrent callers while the server is down.</remarks>
    public TimeSpan SessionCreateFailureBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>When set, session slots that have not been used for this duration are eligible for client-side eviction. Null (default) means never evict.</summary>
    /// <remarks>Session slots are tiny (~100 bytes); eviction only matters for deployments keying sessions by very large user populations. SwarmUI expires sessions server-side after ~31 days regardless.</remarks>
    public TimeSpan? SessionIdleEviction { get; set; }

    /// <summary>WebSocket receive buffer size in bytes.</summary>
    /// <remarks>Buffers are pooled; this controls the per-read chunk size, not the maximum message size (see <see cref="MaxWebSocketMessageBytes"/>). Default is 16KB.</remarks>
    public int WebSocketBufferSize { get; set; } = 16384;

    /// <summary>Maximum size of a single WebSocket message before the stream is aborted as a protocol error.</summary>
    /// <remarks>Generation results can arrive as multi-megabyte base64 data URLs when the server is configured not to save files. Default is 256MB.</remarks>
    public long MaxWebSocketMessageBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>WebSocket keep-alive (protocol ping) interval.</summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long to wait for graceful WebSocket close handshake from server.</summary>
    public TimeSpan WebSocketCloseTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>Maximum time to wait between WebSocket messages before treating the connection as dead.</summary>
    /// <remarks>SwarmUI emits app-level keep_alive frames at most every 30 seconds during generation tails and status/progress frames continuously during active work, so a healthy stream never approaches this. Default is 3 minutes (the server's own per-send timeout is 2 minutes).</remarks>
    public TimeSpan WebSocketReceiveTimeout { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>Returns <see cref="BaseUrl"/> normalized for URL composition (no trailing slash).</summary>
    internal string NormalizedBaseUrl => BaseUrl.TrimEnd('/');
}
