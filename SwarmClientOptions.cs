using System;

namespace SwarmUI.ApiClient;

/// <summary>Configuration options for the SwarmUI client.</summary>
/// <remarks>This class defines all configurable parameters that control client behavior, including connection settings, timeouts, retry policies, and authentication.</remarks>
public class SwarmClientOptions
{
    /// <summary>HTTP header name used for SwarmUI authentication.</summary>
    public string AuthorizationHeaderName { get; set; } = "Authorization";

    /// <summary>Base URL of the SwarmUI server instance.</summary>
    /// <remarks>Example: "http://localhost:7801" or "https://hartsy.ai". This should NOT include the /API/ path - that will be appended automatically.</remarks>
    public string BaseUrl { get; set; } = "http://localhost:7801";

    /// <summary>Optional authorization header value for authenticated requests.</summary>
    /// <remarks>SwarmUI uses simple header-based authentication when configured. Set to null or empty string if no authentication is required. Example: "your-api-key-here".</remarks>
    public string? Authorization { get; set; }

    /// <summary>HTTP request timeout duration.</summary>
    /// <remarks>This applies to all HTTP requests (non-WebSocket operations). Default is 100 seconds to accommodate slow model loading.</remarks>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>WebSocket buffer size in bytes for receiving messages.</summary>
    /// <remarks>Larger buffers can handle bigger preview images and metadata. Default is 16KB (16384 bytes).</remarks>
    public int WebSocketBufferSize { get; set; } = 16384;

    /// <summary>Maximum number of retry attempts for failed HTTP requests.</summary>
    /// <remarks>Used by the retry policy when transient errors occur. Default is 3 attempts.</remarks>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>WebSocket keep-alive interval.</summary>
    /// <remarks>Sends periodic pings to keep the connection alive during long operations. Default is 30 seconds.</remarks>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long to wait for graceful WebSocket close handshake from server.</summary>
    /// <remarks>After sending CloseOutput, we wait this long for the server's Close frame. Default is 1.5 seconds.</remarks>
    public TimeSpan WebSocketCloseTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

    // TODO: Add an IRetryPolicy field once Polly integration is in place.
    // This will allow callers to provide custom retry policies.
    // Example: public IAsyncPolicy<HttpResponseMessage>? RetryPolicy { get; set; }

    // TODO: Add logging configuration options.
    // Example: public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    // TODO: Consider adding session caching options.
    // Example: public bool EnableSessionCaching { get; set; } = true;
    // Example: public TimeSpan SessionCacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
}
