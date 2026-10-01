using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions.AudioLab;

/// <summary>A duplex client for the AudioLab extension's <c>AudioLabVoiceSession</c> route: a real-time,
/// phone-call-style voice agent session over one WebSocket.</summary>
/// <remarks>The wire protocol mixes JSON (the <c>start</c> handshake, events, the client's <c>end</c>) with
/// binary audio (mono PCM16 both directions; server replies are additionally turn-tagged with a 4-byte
/// little-endian id), so it cannot go through <see cref="ISwarmWebSocketClient.StreamFramesAsync"/>, which only
/// ever UTF8-decodes every message. This class drives the raw <see cref="IClientWebSocket"/> seam directly,
/// sharing <see cref="SwarmWebSocketClient"/>'s own connect-retry, auth, and close-handshake logic through
/// <see cref="WebSocketConnectionHelpers"/> so both get the same behavior rather than a second implementation
/// that could drift. One instance is good for exactly one call: construct, <see cref="ConnectAsync"/>, use
/// <see cref="Events"/>/<see cref="ReplyAudio"/>/<see cref="SendAudioAsync"/>, then <see cref="EndAsync"/> or
/// dispose (either ends the call cleanly -- see <see cref="EndAsync"/>'s remarks on why that matters here too).
/// </remarks>
public sealed class AudioLabVoiceSessionClient : IAsyncDisposable
{
    private readonly SwarmClientOptions _options;
    private readonly ISessionManager _sessionManager;
    private readonly IClientWebSocketFactory _socketFactory;
    private readonly ResiliencePipeline _connectPipeline;
    private readonly ILogger<AudioLabVoiceSessionClient> _logger;
    private readonly string _baseWsUrl;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Channel<VoiceSessionEvent> _events = Channel.CreateUnbounded<VoiceSessionEvent>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private readonly Channel<VoiceSessionAudioFrame> _audio = Channel.CreateUnbounded<VoiceSessionAudioFrame>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private readonly CancellationTokenSource _pumpCancel = new();
    private IClientWebSocket? _socket;
    private Task? _pumpTask;
    private int _connected;
    private int _ended;

    /// <summary>Creates a new AudioLabVoiceSessionClient using real WebSockets.</summary>
    /// <param name="options">Client configuration options -- the same instance used for the rest of a
    /// <see cref="ISwarmClient"/>, so base URL and auth stay in sync.</param>
    /// <param name="sessionManager">Session pool for session id acquisition and invalidation (<see cref="ISwarmClient.Sessions"/>).</param>
    /// <param name="logger">Optional logger.</param>
    public AudioLabVoiceSessionClient(SwarmClientOptions options, ISessionManager sessionManager, ILogger<AudioLabVoiceSessionClient>? logger = null)
        : this(options, sessionManager, ClientWebSocketFactory.Instance, logger)
    {
    }

    /// <summary>Test seam: create with a custom socket factory.</summary>
    internal AudioLabVoiceSessionClient(SwarmClientOptions options, ISessionManager sessionManager, IClientWebSocketFactory socketFactory, ILogger<AudioLabVoiceSessionClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
        _connectPipeline = SwarmResiliencePipelines.BuildWebSocketConnectPipeline(options);
        _logger = logger ?? NullLogger<AudioLabVoiceSessionClient>.Instance;
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

    /// <summary>JSON events, in arrival order. Completes (ending the enumeration) when the session ends, whether
    /// by <see cref="EndAsync"/>, the server closing first, or a connection failure (which the enumeration
    /// surfaces by rethrowing).</summary>
    public IAsyncEnumerable<VoiceSessionEvent> Events => _events.Reader.ReadAllAsync();

    /// <summary>Reply audio frames, in arrival order. A separate stream from <see cref="Events"/> fed by the
    /// same socket, so a consumer that only enumerates one of the two does not stall the other -- both channels
    /// are unbounded for exactly that reason.</summary>
    public IAsyncEnumerable<VoiceSessionAudioFrame> ReplyAudio => _audio.Reader.ReadAllAsync();

    /// <summary>Connects and sends the <c>start</c> handshake. Must be called exactly once, before
    /// <see cref="SendAudioAsync"/> or enumerating <see cref="Events"/>/<see cref="ReplyAudio"/>.</summary>
    /// <param name="start">The start handshake. <see cref="VoiceSessionStartOptions.Model"/> and
    /// <see cref="VoiceSessionStartOptions.InputRate"/> are required.</param>
    /// <param name="sessionKey">Which pooled session to authenticate with.</param>
    /// <param name="cancellationToken">Cancellation token for the connect operation (not the whole call --
    /// cancelling after this returns has no effect; use <see cref="EndAsync"/> to end an in-progress call).</param>
    /// <exception cref="ArgumentException"><paramref name="start"/> is missing a required field.</exception>
    /// <exception cref="InvalidOperationException"><see cref="ConnectAsync"/> was already called on this instance.</exception>
    /// <exception cref="SwarmSessionException">The server rejected the session id and the refresh budget was exhausted.</exception>
    /// <exception cref="SwarmWebSocketException">Connection could not be established, or the server rejected the
    /// start handshake itself (eg a bad <see cref="VoiceSessionStartOptions.InputRate"/>).</exception>
    public async Task ConnectAsync(VoiceSessionStartOptions start, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(start);
        if (string.IsNullOrWhiteSpace(start.Model))
        {
            throw new ArgumentException("Model is required", nameof(start));
        }
        if (Interlocked.Exchange(ref _connected, 1) != 0)
        {
            throw new InvalidOperationException("ConnectAsync was already called on this instance.");
        }
        Uri wsUri = new($"{_baseWsUrl}/API/AudioLabVoiceSession");
        for (int refreshCycle = 0; ; refreshCycle++)
        {
            string sessionId = await _sessionManager.GetOrCreateSessionAsync(sessionKey, cancellationToken).ConfigureAwait(false);
            IClientWebSocket? socket = null;
            bool handedOff = false;
            try
            {
                socket = await WebSocketConnectionHelpers.ConnectWithRetryAsync(_socketFactory, _options, _connectPipeline, wsUri, cancellationToken).ConfigureAwait(false);
                JObject payload = JObject.FromObject(start);
                payload["session_id"] = sessionId;
                byte[] requestBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
                await socket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
                // Blocking for one frame here is safe specifically for this route, not a general WS pattern: a
                // phone-call-style session that waits for the caller to speak could otherwise send nothing for
                // a long time, and this would hang until WebSocketReceiveTimeout for a perfectly healthy
                // connection. AudioLabVoiceSession's own handler always sends a {"state":"Warming"} event
                // synchronously right after validating the start frame, before any slow model-loading work
                // (see AudioAPI/VoiceSessionEndpoints.AudioLabVoiceSession), so something real arrives quickly
                // either way: that notice on success, or exactly one JSON error frame (a rejected session id or
                // a rejected start message) followed by a normal close on failure -- the same contract every
                // other SwarmUI WS route uses for the latter case.
                JObject? firstFrame = await ReceiveOneTextFrameAsync(socket, cancellationToken).ConfigureAwait(false);
                if (firstFrame is not null && string.Equals(firstFrame["error_id"]?.ToString(), "invalid_session_id", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Server rejected session for key '{Key}' on AudioLabVoiceSession (cycle {Cycle}/{Cap})", sessionKey, refreshCycle + 1, _options.SessionRefreshCap);
                    _sessionManager.InvalidateSession(sessionKey, sessionId);
                    if (refreshCycle + 1 >= _options.SessionRefreshCap)
                    {
                        throw new SwarmSessionException($"Session for key '{sessionKey}' was rejected {refreshCycle + 1} times in a row; giving up. {firstFrame["error"]}");
                    }
                    await WebSocketConnectionHelpers.GracefulCloseAsync(socket, _options.WebSocketCloseTimeout, _logger, CancellationToken.None).ConfigureAwait(false);
                    continue;
                }
                if (firstFrame is not null && firstFrame["error"] is JToken error)
                {
                    throw new SwarmWebSocketException($"The server rejected the start handshake: {error}", socket.State);
                }
                _socket = socket;
                handedOff = true;
                // A non-null, non-error first frame is already a live event (unusual but not disallowed by the
                // protocol) -- hand it to the pump's own channel instead of dropping it. _pumpCancel, not
                // CancellationToken.None: EndAsync must be able to force this loop's own ReceiveAsync to give up
                // even when nothing more is arriving, both so it cannot hang forever and so its receive loop is
                // guaranteed stopped before GracefulCloseAsync's own receive-for-the-peer's-close-reply runs --
                // two concurrent ReceiveAsync calls on one socket is a protocol violation most implementations
                // reject outright.
                _pumpTask = PumpAsync(socket, firstFrame, _pumpCancel.Token);
                return;
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
                _logger.LogError(ex, "Failed to establish the AudioLabVoiceSession connection");
                throw new SwarmWebSocketException("Failed to connect to the AudioLabVoiceSession endpoint", socket?.State, ex);
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

    /// <summary>Sends mono audio at the rate <see cref="ConnectAsync"/>'s <see cref="VoiceSessionStartOptions.InputRate"/>
    /// declared, as one binary frame. Serialized against concurrent calls (including <see cref="EndAsync"/>'s own
    /// send) so two overlapping sends can never interleave into one corrupted frame.</summary>
    public async Task SendAudioAsync(ReadOnlyMemory<float> samples, CancellationToken cancellationToken = default)
    {
        IClientWebSocket socket = RequireConnectedSocket();
        byte[] bytes = Pcm16.Encode(samples.Span);
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>Ends the call: sends <c>{"end":true}</c>, then completes the WebSocket close handshake.</summary>
    /// <remarks>LLMAssistant's own route (which a server-side <c>AudioLabVoiceSession</c> talks to on the
    /// caller's behalf) keeps one uncancelled background receive to detect a disconnect; completing this
    /// handshake rather than abandoning the connection is what lets its cleanup finish without logging what was
    /// actually a clean end as an error -- the same reasoning <c>RemoteTextService.CloseGracefullyAsync</c>
    /// documents server-side for that same route. Safe to call more than once, and safe to skip and go straight
    /// to <see cref="DisposeAsync"/> -- that does the same thing for a caller that forgot.</remarks>
    public async Task EndAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _ended, 1) != 0)
        {
            return;
        }
        IClientWebSocket? socket = _socket;
        if (socket is null)
        {
            return;
        }
        if (socket.State == WebSocketState.Open)
        {
            try
            {
                await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    byte[] endFrame = Encoding.UTF8.GetBytes("""{"end":true}""");
                    await socket.SendAsync(new ArraySegment<byte>(endFrame), WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Sending the end frame failed (socket likely already closing)");
            }
        }
        // The pump must be fully stopped -- not just asked to stop -- before the close handshake below issues
        // its own ReceiveAsync: two concurrent receives on one socket is a protocol violation most
        // implementations reject outright, and without a forced cancel here the pump's receive could otherwise
        // block forever on a quiet call (nothing more arriving is the normal case, not a failure).
        _pumpCancel.Cancel();
        if (_pumpTask is not null)
        {
            try
            {
                await _pumpTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "The receive pump ended with an exception while EndAsync was stopping it (expected)");
            }
        }
        await WebSocketConnectionHelpers.GracefulCloseAsync(socket, _options.WebSocketCloseTimeout, _logger, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await EndAsync(CancellationToken.None).ConfigureAwait(false);
        _sendLock.Dispose();
        _pumpCancel.Dispose();
    }

    private IClientWebSocket RequireConnectedSocket()
    {
        IClientWebSocket? socket = _socket;
        if (socket is null)
        {
            throw new InvalidOperationException("ConnectAsync must complete successfully before sending audio.");
        }
        return socket;
    }

    /// <summary>Reads exactly one complete text message for the connect-time handshake check. Returns null when
    /// the server closed without sending anything (a legitimate outcome: nothing to report as an error).</summary>
    private static async Task<JObject?> ReceiveOneTextFrameAsync(IClientWebSocket socket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[16 * 1024];
        using MemoryStream message = new();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);
        if (message.Length == 0)
        {
            return null;
        }
        string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
        return string.IsNullOrWhiteSpace(text) ? null : JObject.Parse(text);
    }

    /// <summary>Reads frames for the lifetime of the connection, demultiplexing JSON events and binary reply
    /// audio into their own channels. Ends (completing both channels) on a server close or <see cref="EndAsync"/>
    /// closing the socket; an unexpected failure instead faults both channels so an enumerator rethrows it,
    /// rather than silently ending the stream.</summary>
    private async Task PumpAsync(IClientWebSocket socket, JObject? alreadyReceivedFirstFrame, CancellationToken cancellationToken)
    {
        try
        {
            if (alreadyReceivedFirstFrame is not null)
            {
                await DispatchFrameAsync(alreadyReceivedFirstFrame, cancellationToken).ConfigureAwait(false);
            }
            byte[] receiveBuffer = new byte[_options.WebSocketBufferSize];
            while (true)
            {
                using MemoryStream message = new();
                WebSocketReceiveResult result;
                WebSocketMessageType? type = null;
                bool serverClosed = false;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        serverClosed = true;
                        break;
                    }
                    type ??= result.MessageType;
                    message.Write(receiveBuffer, 0, result.Count);
                }
                while (!result.EndOfMessage);
                if (serverClosed)
                {
                    break;
                }
                if (type == WebSocketMessageType.Binary)
                {
                    byte[] frame = message.ToArray();
                    if (frame.Length >= VoiceOutboundFrame.HeaderBytes)
                    {
                        (int turnId, float[] samples) = VoiceOutboundFrame.Decode(frame);
                        await _audio.Writer.WriteAsync(new VoiceSessionAudioFrame(turnId, samples), cancellationToken).ConfigureAwait(false);
                    }
                    continue;
                }
                if (message.Length == 0)
                {
                    continue;
                }
                string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }
                JObject jsonFrame;
                try
                {
                    jsonFrame = JObject.Parse(text);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping a malformed AudioLabVoiceSession frame");
                    continue;
                }
                await DispatchFrameAsync(jsonFrame, cancellationToken).ConfigureAwait(false);
            }
            _events.Writer.TryComplete();
            _audio.Writer.TryComplete();
        }
        catch (OperationCanceledException)
        {
            _events.Writer.TryComplete();
            _audio.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            _events.Writer.TryComplete(ex);
            _audio.Writer.TryComplete(ex);
        }
    }

    private async Task DispatchFrameAsync(JObject frame, CancellationToken cancellationToken)
    {
        if (frame.ContainsKey("keep_alive"))
        {
            return;
        }
        VoiceSessionEvent evt = frame.ToObject<VoiceSessionEvent>() ?? new VoiceSessionEvent();
        evt.Raw = frame;
        await _events.Writer.WriteAsync(evt, cancellationToken).ConfigureAwait(false);
    }
}
