using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>Provides WebSocket communication with SwarmUI API for streaming operations, such as image generation and model-related workflows.</summary>
/// <remarks>Manages connection lifecycle, message streaming, retry logic, and graceful cleanup for SwarmUI WebSocket endpoints. For detailed behavior, error handling, and usage patterns, see CodingGuidelines.md (WebSockets section).</remarks>
public class SwarmWebSocketClient : ISwarmWebSocketClient
{
    /// <summary>Internal implementation data containing WebSocket configuration and dependencies. Uses the Impl struct pattern to organize fields per coding guidelines.</summary>
    public struct Impl
    {
        /// <summary>Configuration options controlling WebSocket behavior. Includes buffer sizes, timeouts, retry settings, and connection parameters.</summary>
        public SwarmClientOptions Options;

        /// <summary>Session manager for obtaining session IDs required by WebSocket requests. Every WebSocket request must include a session_id in the initial payload.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for debugging WebSocket lifecycle, messages, and errors. Uses NullLogger if none provided, so logging is always safe but optional.</summary>
        public ILogger<SwarmWebSocketClient> Logger;

        /// <summary>Tracks all active WebSocket connections for cleanup during disposal. Key is the session ID, value is the ClientWebSocket instance. Thread-safe for concurrent access from multiple generation operations.</summary>
        public ConcurrentDictionary<string, ClientWebSocket> ActiveConnections;

        /// <summary>WebSocket base URL derived from HTTP base URL. Converts http:// to ws:// and https:// to wss:// for WebSocket connections. Does not include the /API/ path - that's appended per endpoint.</summary>
        public string BaseWsUrl;
    }

    /// <summary>Internal implementation data. Do not use directly unless absolutely necessary.</summary>
    public Impl Internal;

    /// <summary>Creates a new SwarmWebSocketClient instance with the specified dependencies. Automatically converts the HTTP base URL to WebSocket format for connections.</summary>
    /// <param name="options">Client configuration options. Must not be null.</param>
    /// <param name="sessionManager">Session manager for obtaining session IDs. Must not be null.</param>
    /// <param name="logger">Optional logger for WebSocket operations. Uses NullLogger if null.</param>
    /// <remarks>
    /// The WebSocket base URL is derived from options.BaseUrl by replacing http:// with ws://
    /// and https:// with wss://. This ensures secure connections when the HTTP client uses HTTPS.
    /// </remarks>
    public SwarmWebSocketClient(SwarmClientOptions options, ISessionManager sessionManager, ILogger<SwarmWebSocketClient>? logger = null)
    {
        Internal.Options = options ?? throw new ArgumentNullException(nameof(options));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<SwarmWebSocketClient>.Instance;
        Internal.ActiveConnections = new ConcurrentDictionary<string, ClientWebSocket>();
        string baseUrl = options.BaseUrl;
        if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Internal.BaseWsUrl = string.Concat("wss://", baseUrl.AsSpan(8));
        }
        else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Internal.BaseWsUrl = string.Concat("ws://", baseUrl.AsSpan(7));
        }
        else
        {
            // Assume non-secure if no protocol specified (e.g., localhost)
            Internal.BaseWsUrl = "ws://" + baseUrl;
        }
    }

    /// <summary>Streams messages from a SwarmUI WebSocket endpoint with automatic retry on connection failures.</summary>
    /// <typeparam name="TUpdate">The type of update messages to yield to the caller.</typeparam>
    /// <param name="endpoint">The WebSocket endpoint name without /API/ prefix (e.g., "GenerateText2ImageWS"). This should be the WS-suffixed version of the endpoint for streaming operations.</param>
    /// <param name="request">The initial request payload to send after connection. Must not be null. The session_id field will be automatically added to this payload before sending.</param>
    /// <param name="messageParser">Function to parse raw JSON messages into typed update objects. Called for each complete message received from the WebSocket. Should handle all expected message formats for the endpoint.</param>
    /// <param name="cancellationToken">Cancellation token for the streaming operation.</param>
    /// <returns>Async enumerable of parsed update messages. Use await foreach to process updates as they arrive.</returns>
    /// <exception cref="SwarmWebSocketException">Thrown when connection fails after all retry attempts, or when an unrecoverable error occurs during message streaming.</exception>
    /// <remarks>Handles session injection, buffering/chunking, retry with backoff, and graceful cleanup of connections. The caller provides a parser to turn raw JSON into typed updates. See CodingGuidelines.md (WebSockets section) for full behavior and error-handling details.</remarks>
    public async IAsyncEnumerable<TUpdate> StreamMessagesAsync<TUpdate>(string endpoint, object request, Func<JObject, TUpdate> messageParser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
        }
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(messageParser);
        ClientWebSocket? webSocket = null;
        string? sessionId = null;
        try
        {
            for (int retryCount = 0; retryCount <= Internal.Options.MaxRetryAttempts; retryCount++)
            {
                try
                {
                    sessionId = await Internal.SessionManager.GetOrCreateSessionAsync(cancellationToken).ConfigureAwait(false);
                    webSocket = new ClientWebSocket();
                    webSocket.Options.KeepAliveInterval = Internal.Options.KeepAliveInterval;
                    if (!string.IsNullOrEmpty(Internal.Options.Authorization))
                    {
                        webSocket.Options.SetRequestHeader("Authorization", Internal.Options.Authorization);
                    }
                    Uri wsUri = new Uri($"{Internal.BaseWsUrl}/API/{endpoint}");
                    Internal.Logger.LogDebug("Connecting to WebSocket: {Endpoint} (attempt {Attempt}/{MaxAttempts})", endpoint, retryCount + 1, Internal.Options.MaxRetryAttempts + 1);
                    await webSocket.ConnectAsync(wsUri, cancellationToken).ConfigureAwait(false);
                    Internal.Logger.LogInformation("WebSocket connected: {Endpoint}", endpoint);
                    Internal.ActiveConnections.TryAdd(sessionId, webSocket);
                    JObject payload = JObject.FromObject(request);
                    payload["session_id"] = sessionId;
                    string requestJson = JsonConvert.SerializeObject(payload);
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
                    await webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
                    Internal.Logger.LogDebug("Sent WebSocket request payload for {Endpoint}", endpoint);
                    break;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely && retryCount < Internal.Options.MaxRetryAttempts)
                {
                    int delayMs = 1000 * (retryCount + 1);
                    Internal.Logger.LogWarning("WebSocket connection failed (attempt {Attempt}/{MaxAttempts}): {Error}. Retrying in {Delay}ms...",
                        retryCount + 1, Internal.Options.MaxRetryAttempts + 1, ex.Message, delayMs);
                    if (webSocket is not null)
                    {
                        webSocket.Dispose();
                        webSocket = null;
                    }
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (Exception ex)
                {
                    Internal.Logger.LogError(ex, "Failed to establish WebSocket connection to {Endpoint}", endpoint);
                    throw new SwarmWebSocketException($"Failed to connect to WebSocket endpoint {endpoint} after {retryCount + 1} attempts");
                }
            }
            // If we get here without a valid WebSocket, all retries failed and something went very wrong. Throw.
            if (webSocket is null || webSocket.State is not WebSocketState.Open)
            {
                throw new SwarmWebSocketException($"Failed to establish WebSocket connection to {endpoint} after all retry attempts");
            }
            byte[] buffer = new byte[Internal.Options.WebSocketBufferSize];
            StringBuilder messageBuilder = new();
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Internal.Logger.LogDebug("WebSocket receive cancelled for {Endpoint}", endpoint);
                    break;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode is WebSocketError.ConnectionClosedPrematurely)
                {
                    Internal.Logger.LogWarning("WebSocket connection closed prematurely for {Endpoint}", endpoint);
                    break;
                }
                catch (Exception ex)
                {
                    Internal.Logger.LogError(ex, "Error receiving WebSocket message from {Endpoint}", endpoint);
                    throw new SwarmWebSocketException($"WebSocket receive error for {endpoint}");
                }
                if (result.MessageType is WebSocketMessageType.Close)
                {
                    Internal.Logger.LogInformation("Received Close frame from server for {Endpoint}", endpoint);
                    break;
                }
                if (result.MessageType is WebSocketMessageType.Text)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage)
                    {
                        string message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        JObject messageJson = JObject.Parse(message);
                        TUpdate update = messageParser(messageJson);
                        if (update is not null)
                        {
                            Internal.Logger.LogDebug("Received WebSocket message from {Endpoint}", endpoint);
                            yield return update;
                        }
                    }
                }
            }
        }
        finally
        {
            if (webSocket is not null)
            {
                Internal.Logger.LogDebug("Cleaning up WebSocket connection for {Endpoint}", endpoint);
                await GracefulCloseAsync(webSocket, CancellationToken.None).ConfigureAwait(false);
                if (sessionId != null)
                {
                    Internal.ActiveConnections.TryRemove(sessionId, out ClientWebSocket? _);
                }
            }
        }
    }

    /// <summary>Performs a best-effort graceful close handshake on a WebSocket connection.</summary>
    /// <param name="webSocket">The WebSocket connection to close. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation. Note that close operations use a separate timeout from options.WebSocketCloseTimeout, so this token is primarily for coordination with parent operations.</param>
    /// <remarks>Implements the RFC 6455 close sequence and swallows non-critical errors during cleanup. If the WebSocket is null or already closed/aborted, this method returns immediately. See CodingGuidelines.md (WebSockets section) for the full handshake flow.</remarks>
    public async Task GracefulCloseAsync(ClientWebSocket webSocket, CancellationToken cancellationToken = default)
    {
        if (webSocket is null)
        {
            return;
        }
        try
        {
            if (webSocket.State is WebSocketState.Open)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None).ConfigureAwait(false);
                using CancellationTokenSource cts = new(Internal.Options.WebSocketCloseTimeout);
                byte[] buffer = new byte[512];
                while (webSocket.State is WebSocketState.CloseSent && !cts.IsCancellationRequested)
                {
                    try
                    {
                        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
                        if (result.MessageType is WebSocketMessageType.Close)
                        {
                            Internal.Logger.LogDebug("Received server Close acknowledgment");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Internal.Logger.LogDebug("Timeout waiting for server Close acknowledgment");
                        break;
                    }
                    catch (WebSocketException)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Internal.Logger.LogDebug(ex, "Exception during WebSocket graceful close (ignored)");
        }
        finally
        {
            try
            {
                webSocket.Dispose();
            }
            catch (Exception ex)
            {
                Internal.Logger.LogDebug(ex, "Exception disposing WebSocket (ignored)");
            }
        }
    }

    /// <summary>Disconnects all active WebSocket connections tracked by this client.</summary>
    /// <remarks>Performs graceful close handshakes for each connection and clears the tracking dictionary. Typically called from SwarmClient disposal paths. See CodingGuidelines.md (WebSockets section) for sequencing and error-handling details.</remarks>
    public async Task DisconnectAllAsync()
    {
        KeyValuePair<string, ClientWebSocket>[] connections = Internal.ActiveConnections.ToArray();
        if (connections.Length is 0)
        {
            Internal.Logger.LogDebug("No active WebSocket connections to disconnect");
            return;
        }
        Internal.Logger.LogInformation("Disconnecting {Count} active WebSocket connections", connections.Length);
        Internal.ActiveConnections.Clear();
        foreach (KeyValuePair<string, ClientWebSocket> connection in connections)
        {
            try
            {
                await GracefulCloseAsync(connection.Value, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Internal.Logger.LogWarning(ex, "Error closing WebSocket connection for session {SessionId}",
                    connection.Key.Length > 8 ? string.Concat(connection.Key.AsSpan(0, 8), "...") : connection.Key);
            }
        }
        Internal.Logger.LogInformation("Disconnected all WebSocket connections");
    }
}
