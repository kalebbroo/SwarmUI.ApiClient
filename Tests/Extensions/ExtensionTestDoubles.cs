using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Tests.Extensions
{
    /// <summary>HTTP client double that records the endpoint and payload of each call and deserializes a canned JSON response.</summary>
    /// <remarks>Shared by the extension endpoint tests so each suite only declares what it actually asserts on.</remarks>
    public sealed class RecordingExtensionHttpClient : ISwarmHttpClient
    {
        /// <summary>Endpoint name of the most recent call.</summary>
        public string? LastEndpoint { get; private set; }

        /// <summary>Serialized payload of the most recent call.</summary>
        public JObject? LastPayload { get; private set; }

        /// <summary>JSON the next call deserializes its response from.</summary>
        public JObject ResponseToReturn { get; set; } = new JObject { ["success"] = true };

        /// <inheritdoc />
        public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
        {
            LastEndpoint = endpoint;
            LastPayload = payload as JObject ?? (payload is not null ? JObject.FromObject(payload) : new JObject());
            return Task.FromResult(Deserialize<TResponse>());
        }

        /// <inheritdoc />
        public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
        {
            LastEndpoint = endpoint;
            LastPayload = JObject.FromObject(request);
            return Task.FromResult(Deserialize<TResponse>());
        }

        /// <summary>Deserializes <see cref="ResponseToReturn"/> into the requested response type.</summary>
        private TResponse Deserialize<TResponse>() where TResponse : class
        {
            TResponse? response = ResponseToReturn.ToObject<TResponse>();
            if (response is null)
            {
                throw new InvalidOperationException($"Could not deserialize canned response into {typeof(TResponse).Name}.");
            }
            return response;
        }
    }

    /// <summary>WebSocket client double that records the endpoint and payload, then replays canned frames.</summary>
    public sealed class RecordingExtensionWebSocketClient : ISwarmWebSocketClient
    {
        /// <summary>Endpoint name of the most recent stream.</summary>
        public string? LastEndpoint { get; private set; }

        /// <summary>Serialized initial payload of the most recent stream.</summary>
        public JObject? LastPayload { get; private set; }

        /// <summary>Frames replayed to the caller, in order.</summary>
        public List<JObject> FramesToReturn { get; set; } = [];

        /// <inheritdoc />
        public async IAsyncEnumerable<TUpdate> StreamMessagesAsync<TUpdate>(string endpoint, object request, Func<JObject, TUpdate> messageParser, CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            LastPayload = request as JObject ?? JObject.FromObject(request, JsonSerializer.CreateDefault());
            foreach (JObject frame in FramesToReturn)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return messageParser(frame);
            }
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task GracefulCloseAsync(ClientWebSocket webSocket, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DisconnectAllAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>Session manager double returning fixed session IDs.</summary>
    public sealed class StubSessionManager : ISessionManager
    {
        /// <inheritdoc />
        public Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("session-1");
        }

        /// <inheritdoc />
        public Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("session-2");
        }

        /// <inheritdoc />
        public void InvalidateSession()
        {
        }

        /// <inheritdoc />
        public string? CurrentSessionId => "session-1";
    }
}
