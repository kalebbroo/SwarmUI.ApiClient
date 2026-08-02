using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>WebSocket communication layer for SwarmUI streaming endpoints.</summary>
/// <remarks>Implements the session contract from the official API docs: when the server rejects a session id (one <c>invalid_session_id</c> frame, then close), the pooled session is CAS-invalidated, refreshed, and the connection retried — bounded by <see cref="SwarmClientOptions.SessionRefreshCap"/>. The server closes every socket with status 1000 whether the operation succeeded or failed, so termination is judged from frames, never close codes.</remarks>
public class SwarmWebSocketClient : ISwarmWebSocketClient
{
    private readonly SwarmClientOptions _options;
    private readonly ISessionManager _sessionManager;
    private readonly IClientWebSocketFactory _socketFactory;
    private readonly ResiliencePipeline _connectPipeline;
    private readonly ILogger<SwarmWebSocketClient> _logger;
    private readonly ConcurrentDictionary<Guid, IClientWebSocket> _activeConnections = new();
    private readonly string _baseWsUrl;

    /// <summary>Creates a new SwarmWebSocketClient using real WebSockets.</summary>
    /// <param name="options">Client configuration options.</param>
    /// <param name="sessionManager">Session pool for session id acquisition and invalidation.</param>
    /// <param name="logger">Optional logger.</param>
    public SwarmWebSocketClient(SwarmClientOptions options, ISessionManager sessionManager, ILogger<SwarmWebSocketClient>? logger = null)
        : this(options, sessionManager, ClientWebSocketFactory.Instance, logger)
    {
    }

    /// <summary>Test seam: create with a custom socket factory.</summary>
    internal SwarmWebSocketClient(SwarmClientOptions options, ISessionManager sessionManager, IClientWebSocketFactory socketFactory, ILogger<SwarmWebSocketClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
        _connectPipeline = SwarmResiliencePipelines.BuildWebSocketConnectPipeline(options);
        _logger = logger ?? NullLogger<SwarmWebSocketClient>.Instance;
        string baseUrl = options.NormalizedBaseUrl;
        if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _baseWsUrl = string.Concat("wss://", baseUrl.AsSpan(8));
        }
        else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            _baseWsUrl = string.Concat("ws://", baseUrl.AsSpan(7));
        }
        else
        {
            _baseWsUrl = "ws://" + baseUrl;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<JObject> StreamFramesAsync(string endpoint, JObject request, string sessionKey = SwarmSessionKeys.Default, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
        }
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sessionKey);
        Connection connection = await EstablishAsync(endpoint, request, sessionKey, cancellationToken).ConfigureAwait(false);
        Guid connectionId = Guid.NewGuid();
        _activeConnections.TryAdd(connectionId, connection.Socket);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_options.WebSocketBufferSize);
        try
        {
            JObject? frame = connection.FirstFrame;
            int consecutiveMalformed = 0;
            while (frame is not null || connection.Open)
            {
                if (frame is not null)
                {
                    if (frame.ContainsKey("keep_alive"))
                    {
                        _logger.LogDebug("Consumed keep_alive ping from {Endpoint}", endpoint);
                    }
                    else
                    {
                        yield return frame;
                    }
                }
                frame = await ReceiveFrameAsync(connection, endpoint, buffer, () => consecutiveMalformed = 0, () => ++consecutiveMalformed, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _activeConnections.TryRemove(connectionId, out _);
            await GracefulCloseAsync(connection.Socket, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Per-stream connection state.</summary>
    private sealed class Connection
    {
        public required IClientWebSocket Socket { get; init; }

        /// <summary>The first frame received after the request payload was sent (already checked for session rejection), or null when the stream opened without one.</summary>
        public JObject? FirstFrame { get; set; }

        /// <summary>False once the server has closed or the receive loop has ended.</summary>
        public bool Open { get; set; } = true;
    }

    /// <summary>Establishes a connection: connect (with transient retry), send the payload, read the first frame, and transparently refresh the session when the server rejects it — bounded by <see cref="SwarmClientOptions.SessionRefreshCap"/>.</summary>
    private async Task<Connection> EstablishAsync(string endpoint, JObject request, string sessionKey, CancellationToken cancellationToken)
    {
        Uri wsUri = new($"{_baseWsUrl}/API/{endpoint}");
        for (int refreshCycle = 0; ; refreshCycle++)
        {
            string sessionId = await _sessionManager.GetOrCreateSessionAsync(sessionKey, cancellationToken).ConfigureAwait(false);
            IClientWebSocket? socket = null;
            bool handedOff = false;
            try
            {
                // Each connect attempt needs a fresh socket — a ClientWebSocket can only be connected once.
                socket = await _connectPipeline.ExecuteAsync(
                    async (state, ct) =>
                    {
                        IClientWebSocket attempt = state.self._socketFactory.Create();
                        try
                        {
                            attempt.SetKeepAliveInterval(state.self._options.KeepAliveInterval);
                            state.self.ApplyAuth(attempt);
                            await attempt.ConnectAsync(state.wsUri, ct).ConfigureAwait(false);
                            return attempt;
                        }
                        catch
                        {
                            attempt.Dispose();
                            throw;
                        }
                    },
                    (self: this, wsUri),
                    cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("WebSocket connected: {Endpoint} (session key '{Key}')", endpoint, sessionKey);
                JObject payload = (JObject)request.DeepClone();
                payload["session_id"] = sessionId;
                byte[] requestBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
                await socket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
                // The server responds to a bad session with exactly one error frame, then a normal close.
                Connection connection = new() { Socket = socket };
                byte[] buffer = ArrayPool<byte>.Shared.Rent(_options.WebSocketBufferSize);
                JObject? firstFrame;
                try
                {
                    int malformed = 0;
                    firstFrame = await ReceiveFrameAsync(connection, endpoint, buffer, () => malformed = 0, () => ++malformed, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                if (firstFrame is not null && string.Equals(firstFrame["error_id"]?.ToString(), "invalid_session_id", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Server rejected session for key '{Key}' on {Endpoint} (cycle {Cycle}/{Cap})", sessionKey, endpoint, refreshCycle + 1, _options.SessionRefreshCap);
                    _sessionManager.InvalidateSession(sessionKey, sessionId);
                    if (refreshCycle + 1 >= _options.SessionRefreshCap)
                    {
                        throw new SwarmSessionException($"Session for key '{sessionKey}' was rejected {refreshCycle + 1} times in a row; giving up. {firstFrame["error"]}");
                    }
                    continue;
                }
                if (firstFrame is null && !connection.Open)
                {
                    // Server accepted the request and closed without frames — a legitimate (empty) stream, not an error.
                    _logger.LogDebug("Server closed the WebSocket for {Endpoint} without sending any frame", endpoint);
                }
                connection.FirstFrame = firstFrame;
                handedOff = true;
                return connection;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (SwarmException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish WebSocket connection to {Endpoint}", endpoint);
                throw new SwarmWebSocketException($"Failed to connect to WebSocket endpoint {endpoint}", socket?.State, ex);
            }
            finally
            {
                if (!handedOff)
                {
                    socket?.Dispose();
                }
            }
        }
    }

    /// <summary>Applies header or cookie authentication to a socket per <see cref="SwarmClientOptions.AuthMode"/>.</summary>
    private void ApplyAuth(IClientWebSocket socket)
    {
        if (string.IsNullOrEmpty(_options.Authorization))
        {
            return;
        }
        if (_options.AuthMode == SwarmAuthMode.SwarmTokenCookie)
        {
            CookieContainer cookies = new();
            cookies.Add(new Uri(_options.NormalizedBaseUrl), new Cookie("swarm_token", _options.Authorization));
            socket.SetCookies(cookies);
        }
        else
        {
            string headerName = string.IsNullOrWhiteSpace(_options.AuthorizationHeaderName) ? "Authorization" : _options.AuthorizationHeaderName;
            socket.SetRequestHeader(headerName, _options.Authorization);
        }
    }

    /// <summary>Receives and parses one complete JSON message. Returns null when the server closed the connection (also flips <see cref="Connection.Open"/>). Malformed frames are skipped with a warning; three in a row aborts the stream.</summary>
    private async Task<JObject?> ReceiveFrameAsync(Connection connection, string endpoint, byte[] buffer, Action resetMalformed, Func<int> bumpMalformed, CancellationToken cancellationToken)
    {
        while (connection.Open)
        {
            using MemoryStream message = new();
            while (true)
            {
                WebSocketReceiveResult result;
                using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_options.WebSocketReceiveTimeout);
                try
                {
                    result = await connection.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Caller cancellation must surface as cancellation, not as a completed stream.
                    _logger.LogDebug("WebSocket stream for {Endpoint} cancelled by caller", endpoint);
                    connection.Open = false;
                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    connection.Open = false;
                    throw new SwarmWebSocketException($"WebSocket receive from {endpoint} timed out after {_options.WebSocketReceiveTimeout}", connection.Socket.State, ex);
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode is WebSocketError.ConnectionClosedPrematurely)
                {
                    _logger.LogWarning("WebSocket connection closed prematurely for {Endpoint}", endpoint);
                    connection.Open = false;
                    return null;
                }
                catch (Exception ex)
                {
                    connection.Open = false;
                    throw new SwarmWebSocketException($"WebSocket receive error for {endpoint}", connection.Socket.State, ex);
                }
                if (result.MessageType is WebSocketMessageType.Close)
                {
                    _logger.LogDebug("Received Close frame from server for {Endpoint}", endpoint);
                    connection.Open = false;
                    return null;
                }
                if (message.Length + result.Count > _options.MaxWebSocketMessageBytes)
                {
                    connection.Open = false;
                    throw new SwarmWebSocketException($"WebSocket message from {endpoint} exceeded the {_options.MaxWebSocketMessageBytes} byte limit", connection.Socket.State);
                }
                message.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    break;
                }
            }
            if (message.Length == 0)
            {
                continue;
            }
            string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            try
            {
                JObject frame = JObject.Parse(text);
                resetMalformed();
                return frame;
            }
            catch (JsonException ex)
            {
                int malformedCount = bumpMalformed();
                _logger.LogWarning(ex, "Skipping malformed WebSocket frame from {Endpoint} ({Count} consecutive)", endpoint, malformedCount);
                if (malformedCount >= 3)
                {
                    connection.Open = false;
                    throw new SwarmWebSocketException($"Received {malformedCount} consecutive malformed frames from {endpoint}; aborting stream", connection.Socket.State, ex);
                }
            }
        }
        return null;
    }

    /// <summary>Best-effort RFC 6455 close handshake; swallows all cleanup errors.</summary>
    private async Task GracefulCloseAsync(IClientWebSocket socket, CancellationToken cancellationToken)
    {
        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None).ConfigureAwait(false);
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.WebSocketCloseTimeout);
                byte[] buffer = new byte[512];
                while (socket.State is WebSocketState.CloseSent && !cts.IsCancellationRequested)
                {
                    try
                    {
                        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
                        if (result.MessageType is WebSocketMessageType.Close)
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
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
            _logger.LogDebug(ex, "Exception during WebSocket graceful close (ignored)");
        }
        finally
        {
            try
            {
                socket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception disposing WebSocket (ignored)");
            }
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAllAsync(CancellationToken cancellationToken = default)
    {
        KeyValuePair<Guid, IClientWebSocket>[] connections = _activeConnections.ToArray();
        if (connections.Length == 0)
        {
            return;
        }
        _logger.LogInformation("Disconnecting {Count} active WebSocket connections", connections.Length);
        foreach (KeyValuePair<Guid, IClientWebSocket> connection in connections)
        {
            if (_activeConnections.TryRemove(connection.Key, out IClientWebSocket? socket))
            {
                await GracefulCloseAsync(socket, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
