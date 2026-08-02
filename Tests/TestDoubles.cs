using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Tests;

/// <summary>Shared in-memory test doubles for the library's core interfaces.</summary>
public sealed class FakeSessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, string> _sessions = new();
    public int CreateCount;
    public int InvalidateCallCount;
    public readonly List<(string Key, string? Observed)> Invalidations = [];
    public Func<string, string>? SessionFactory;

    public Task<string> GetOrCreateSessionAsync(string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default)
    {
        string session = _sessions.GetOrAdd(sessionKey, key =>
        {
            Interlocked.Increment(ref CreateCount);
            return SessionFactory?.Invoke(key) ?? $"session-{key}-{CreateCount}";
        });
        return Task.FromResult(session);
    }

    public async Task<string> RefreshSessionAsync(string sessionKey, string? observedSessionId, CancellationToken cancellationToken = default)
    {
        InvalidateSession(sessionKey, observedSessionId);
        return await GetOrCreateSessionAsync(sessionKey, cancellationToken);
    }

    public void InvalidateSession(string sessionKey, string? observedSessionId)
    {
        Interlocked.Increment(ref InvalidateCallCount);
        lock (Invalidations)
        {
            Invalidations.Add((sessionKey, observedSessionId));
        }
        if (_sessions.TryGetValue(sessionKey, out string? current) && (observedSessionId is null || current == observedSessionId))
        {
            _sessions.TryRemove(new KeyValuePair<string, string>(sessionKey, current));
        }
    }

    public SwarmSessionInfo? GetCachedSession(string sessionKey = SwarmSessionKeys.Default)
        => _sessions.TryGetValue(sessionKey, out string? id) ? new SwarmSessionInfo(id, null, null, null, DateTimeOffset.UtcNow) : null;
}

/// <summary>Records requests and replays canned responses per endpoint.</summary>
public sealed class FakeSwarmHttpClient : ISwarmHttpClient
{
    public readonly List<(string Endpoint, JObject? Payload, string SessionKey)> Requests = [];
    public Func<string, JObject?, string, JObject>? Handler;

    public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default) where TResponse : class
    {
        JObject? payloadJson = payload switch
        {
            null => null,
            JObject jObject => jObject,
            _ => JObject.FromObject(payload)
        };
        lock (Requests)
        {
            Requests.Add((endpoint, payloadJson, sessionKey));
        }
        JObject response = Handler?.Invoke(endpoint, payloadJson, sessionKey) ?? [];
        if (typeof(TResponse) == typeof(JObject))
        {
            return Task.FromResult((TResponse)(object)response);
        }
        return Task.FromResult(response.ToObject<TResponse>()!);
    }
}

/// <summary>Replays a scripted list of frames as a WebSocket stream.</summary>
public sealed class FakeSwarmWebSocketClient : ISwarmWebSocketClient
{
    public readonly List<(string Endpoint, JObject Request, string SessionKey)> Streams = [];
    public Func<string, JObject, IEnumerable<JObject>>? FrameScript;

    public async IAsyncEnumerable<JObject> StreamFramesAsync(string endpoint, JObject request, string sessionKey = SwarmSessionKeys.Default, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (Streams)
        {
            Streams.Add((endpoint, request, sessionKey));
        }
        foreach (JObject frame in FrameScript?.Invoke(endpoint, request) ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return frame;
        }
    }

    public Task DisconnectAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>Scripted fake for the internal IClientWebSocket seam. Actions define what each successive ReceiveAsync returns.</summary>
internal sealed class FakeClientWebSocket : IClientWebSocket
{
    /// <summary>One scripted receive step: either a text payload (possibly split across reads), a close frame, an exception, or a delay.</summary>
    public abstract record Step;
    public sealed record TextFrame(string Json) : Step;
    public sealed record CloseFrame : Step;
    public sealed record ThrowStep(Exception Exception) : Step;
    public sealed record HangStep : Step;

    private readonly Queue<Step> _steps;
    private byte[]? _pending;
    private int _pendingOffset;
    public readonly List<string> SentMessages = [];
    public readonly Dictionary<string, string> Headers = [];
    public bool Disposed;
    public bool CloseOutputCalled;
    public Exception? ConnectException;

    public FakeClientWebSocket(IEnumerable<Step> steps)
    {
        _steps = new Queue<Step>(steps);
    }

    public WebSocketState State { get; set; } = WebSocketState.None;

    public void SetRequestHeader(string name, string value) => Headers[name] = value;

    public void SetKeepAliveInterval(TimeSpan interval)
    {
    }

    public void SetCookies(System.Net.CookieContainer cookies)
    {
    }

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (ConnectException is not null)
        {
            State = WebSocketState.Closed;
            throw ConnectException;
        }
        State = WebSocketState.Open;
        return Task.CompletedTask;
    }

    public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        SentMessages.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        return Task.CompletedTask;
    }

    public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        if (_pending is null)
        {
            if (_steps.Count == 0)
            {
                State = WebSocketState.Closed;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }
            Step step = _steps.Dequeue();
            switch (step)
            {
                case CloseFrame:
                    State = WebSocketState.CloseReceived;
                    return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                case ThrowStep t:
                    State = WebSocketState.Aborted;
                    throw t.Exception;
                case HangStep:
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    throw new OperationCanceledException(cancellationToken);
                case TextFrame text:
                    _pending = Encoding.UTF8.GetBytes(text.Json);
                    _pendingOffset = 0;
                    break;
            }
        }
        int remaining = _pending!.Length - _pendingOffset;
        int count = Math.Min(remaining, buffer.Count);
        Array.Copy(_pending, _pendingOffset, buffer.Array!, buffer.Offset, count);
        _pendingOffset += count;
        bool end = _pendingOffset >= _pending.Length;
        if (end)
        {
            _pending = null;
        }
        return new WebSocketReceiveResult(count, WebSocketMessageType.Text, end);
    }

    public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        CloseOutputCalled = true;
        State = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public void Dispose() => Disposed = true;
}

/// <summary>Factory returning a scripted sequence of fake sockets — one per connection attempt.</summary>
internal sealed class FakeClientWebSocketFactory : IClientWebSocketFactory
{
    private readonly Queue<IClientWebSocket> _sockets;
    public readonly List<IClientWebSocket> Created = [];

    public FakeClientWebSocketFactory(params IClientWebSocket[] sockets)
    {
        _sockets = new Queue<IClientWebSocket>(sockets);
    }

    public IClientWebSocket Create()
    {
        if (_sockets.Count == 0)
        {
            throw new InvalidOperationException("Test requested more sockets than were scripted");
        }
        IClientWebSocket socket = _sockets.Dequeue();
        Created.Add(socket);
        return socket;
    }
}
