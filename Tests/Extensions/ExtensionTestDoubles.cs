using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>Session key of the most recent call.</summary>
        public string? LastSessionKey { get; private set; }

        /// <summary>JSON the next call deserializes its response from.</summary>
        public JObject ResponseToReturn { get; set; } = new JObject { ["success"] = true };

        /// <inheritdoc />
        public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default) where TResponse : class
        {
            LastEndpoint = endpoint;
            LastPayload = payload as JObject ?? (payload is not null ? JObject.FromObject(payload) : new JObject());
            LastSessionKey = sessionKey;
            if (typeof(TResponse) == typeof(JObject))
            {
                return Task.FromResult((TResponse)(object)ResponseToReturn);
            }
            TResponse? response = ResponseToReturn.ToObject<TResponse>();
            if (response is null)
            {
                throw new InvalidOperationException($"Could not deserialize canned response into {typeof(TResponse).Name}.");
            }
            return Task.FromResult(response);
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
        public async IAsyncEnumerable<JObject> StreamFramesAsync(string endpoint, JObject request, string sessionKey = SwarmSessionKeys.Default, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            LastPayload = request;
            foreach (JObject frame in FramesToReturn)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return frame;
            }
        }

        /// <inheritdoc />
        public Task DisconnectAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
