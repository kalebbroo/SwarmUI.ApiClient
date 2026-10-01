using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace SwarmUI.ApiClient.WebSockets;

/// <summary>Shared connect/auth/close logic for the internal <see cref="IClientWebSocket"/> seam.</summary>
/// <remarks>Extracted from <see cref="SwarmWebSocketClient"/>'s own private helpers of the same shape so a second
/// WS-wrapping client (eg <see cref="SwarmUI.ApiClient.Extensions.AudioLab.AudioLabVoiceSessionClient"/>, whose wire protocol mixes binary and
/// JSON frames and so cannot go through <see cref="ISwarmWebSocketClient.StreamFramesAsync"/>) gets the same
/// connect retry, auth, and close behavior for free rather than a reimplementation that could drift out of sync.
/// </remarks>
internal static class WebSocketConnectionHelpers
{
    /// <summary>Applies header or cookie authentication to a socket per <see cref="SwarmClientOptions.AuthMode"/>.
    /// A no-op when <see cref="SwarmClientOptions.Authorization"/> is unset.</summary>
    public static void ApplyAuth(IClientWebSocket socket, SwarmClientOptions options)
    {
        if (string.IsNullOrEmpty(options.Authorization))
        {
            return;
        }
        if (options.AuthMode == SwarmAuthMode.SwarmTokenCookie)
        {
            CookieContainer cookies = new();
            cookies.Add(new Uri(options.NormalizedBaseUrl), new Cookie("swarm_token", options.Authorization));
            socket.SetCookies(cookies);
        }
        else
        {
            string headerName = string.IsNullOrWhiteSpace(options.AuthorizationHeaderName) ? "Authorization" : options.AuthorizationHeaderName;
            socket.SetRequestHeader(headerName, options.Authorization);
        }
    }

    /// <summary>Creates a socket from <paramref name="socketFactory"/>, applies keep-alive and auth, and connects
    /// to <paramref name="uri"/>, retrying transient failures per <paramref name="pipeline"/>. A fresh socket is
    /// created for each attempt -- a <see cref="IClientWebSocket"/> can only be connected once -- and disposed if
    /// that particular attempt fails before returning.</summary>
    public static async Task<IClientWebSocket> ConnectWithRetryAsync(
        IClientWebSocketFactory socketFactory, SwarmClientOptions options, ResiliencePipeline pipeline, Uri uri, CancellationToken cancellationToken)
    {
        return await pipeline.ExecuteAsync(
            static async (state, ct) =>
            {
                IClientWebSocket attempt = state.socketFactory.Create();
                try
                {
                    attempt.SetKeepAliveInterval(state.options.KeepAliveInterval);
                    ApplyAuth(attempt, state.options);
                    await attempt.ConnectAsync(state.uri, ct).ConfigureAwait(false);
                    return attempt;
                }
                catch
                {
                    attempt.Dispose();
                    throw;
                }
            },
            (socketFactory, options, uri),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Best-effort RFC 6455 close handshake: sends Close, waits (bounded by <paramref name="closeTimeout"/>)
    /// for the peer's Close in reply, then disposes the socket regardless of how that wait ended. Safe to call on
    /// a socket in any state -- only <see cref="WebSocketState.Open"/> or <see cref="WebSocketState.CloseReceived"/>
    /// (the "peer already closed first" case, where only the acknowledging Close is needed) actually send
    /// anything. Never throws; every cleanup failure is swallowed and logged at debug level, since the caller's
    /// own `using`/Dispose is the fallback for a connection that is already gone.</summary>
    public static async Task GracefulCloseAsync(IClientWebSocket socket, TimeSpan closeTimeout, ILogger? logger, CancellationToken cancellationToken)
    {
        logger ??= NullLogger.Instance;
        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None).ConfigureAwait(false);
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(closeTimeout);
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
            logger.LogDebug(ex, "Exception during WebSocket graceful close (ignored)");
        }
        finally
        {
            try
            {
                socket.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception disposing WebSocket (ignored)");
            }
        }
    }
}
