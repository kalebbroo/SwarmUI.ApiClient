using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>Minimal abstraction over <see cref="ClientWebSocket"/> so the WebSocket layer can be unit tested with scripted fakes.</summary>
internal interface IClientWebSocket : IDisposable
{
    WebSocketState State { get; }
    void SetRequestHeader(string name, string value);
    void SetKeepAliveInterval(TimeSpan interval);
    void SetCookies(CookieContainer cookies);
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
    Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}

/// <summary>Creates <see cref="IClientWebSocket"/> instances. The default factory produces real <see cref="ClientWebSocket"/> adapters; tests substitute scripted fakes.</summary>
internal interface IClientWebSocketFactory
{
    IClientWebSocket Create();
}

/// <summary>Production adapter wrapping <see cref="ClientWebSocket"/>.</summary>
internal sealed class ClientWebSocketAdapter : IClientWebSocket
{
    private readonly ClientWebSocket _socket = new();

    public WebSocketState State => _socket.State;

    public void SetRequestHeader(string name, string value) => _socket.Options.SetRequestHeader(name, value);

    public void SetKeepAliveInterval(TimeSpan interval) => _socket.Options.KeepAliveInterval = interval;

    public void SetCookies(CookieContainer cookies) => _socket.Options.Cookies = cookies;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _socket.ConnectAsync(uri, cancellationToken);

    public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        => _socket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        => _socket.ReceiveAsync(buffer, cancellationToken);

    public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        => _socket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

    public void Dispose() => _socket.Dispose();
}

/// <summary>Default factory producing real WebSocket adapters.</summary>
internal sealed class ClientWebSocketFactory : IClientWebSocketFactory
{
    public static readonly ClientWebSocketFactory Instance = new();

    public IClientWebSocket Create() => new ClientWebSocketAdapter();
}
