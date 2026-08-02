using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>Streams raw JSON frames from SwarmUI WebSocket API endpoints.</summary>
/// <remarks>Handles connection retry, transparent session refresh when the server rejects the session id, receive timeouts, message framing, and cleanup. Yields every server frame except <c>keep_alive</c> liveness pings (consumed internally); parsing frames into typed updates is the endpoint layer's job so a single frame can produce multiple updates. Cancellation propagates as <see cref="System.OperationCanceledException"/> after a best-effort graceful close — note that SwarmUI does not stop generating when a socket closes; use InterruptAll on the same session to stop work.</remarks>
public interface ISwarmWebSocketClient
{
    /// <summary>Connects to a WS-suffixed SwarmUI API endpoint, sends the request payload (with the session id for <paramref name="sessionKey"/> injected), and yields each received JSON frame until the server closes the connection.</summary>
    /// <param name="endpoint">The WebSocket endpoint name without the /API/ prefix (e.g., "GenerateText2ImageWS").</param>
    /// <param name="request">The initial request payload. The caller's object is never mutated.</param>
    /// <param name="sessionKey">Which pooled session to authenticate with.</param>
    /// <param name="cancellationToken">Cancellation token for the streaming operation.</param>
    /// <exception cref="Exceptions.SwarmSessionException">The server rejected the session id and the refresh budget was exhausted.</exception>
    /// <exception cref="Exceptions.SwarmWebSocketException">Connection could not be established, the stream timed out, or a protocol error occurred.</exception>
    IAsyncEnumerable<JObject> StreamFramesAsync(string endpoint, JObject request, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default);

    /// <summary>Closes all WebSocket connections currently tracked by this client.</summary>
    Task DisconnectAllAsync(CancellationToken cancellationToken = default);
}
