using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>Provides WebSocket communication with the SwarmUI API for streaming operations. Used by endpoint implementations to stream long-running operations such as image generation. See CodingGuidelines.md for detailed implementation notes.</summary>
public interface ISwarmWebSocketClient
{
    /// <summary>Streams messages from a SwarmUI WebSocket endpoint. Connects to the endpoint, sends the initial request, and yields parsed messages as they arrive.</summary>
    /// <typeparam name="TUpdate">The type of update messages to yield.</typeparam>
    /// <param name="endpoint">The WebSocket endpoint name (e.g., "GenerateText2ImageWS").</param>
    /// <param name="request">The initial request payload to send after connection.</param>
    /// <param name="messageParser">Function to parse raw JSON messages into update objects.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Async enumerable of update messages.</returns>
    IAsyncEnumerable<TUpdate> StreamMessagesAsync<TUpdate>(string endpoint, object request, Func<JObject, TUpdate> messageParser, CancellationToken cancellationToken = default);

    /// <summary>Gracefully closes a WebSocket connection. Sends CloseOutput and waits for server's Close frame before disposing.</summary>
    /// <param name="webSocket">The WebSocket to close.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task GracefulCloseAsync(ClientWebSocket webSocket, CancellationToken cancellationToken = default);

    /// <summary>Disconnects all active WebSocket connections. Should be called during client disposal to clean up resources.</summary>
    Task DisconnectAllAsync();
}
