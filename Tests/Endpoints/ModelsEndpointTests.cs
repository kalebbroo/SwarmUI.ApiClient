using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints
{
    /// <summary>Unit tests for <see cref="ModelsEndpoint"/> verifying payload shaping, WebSocket streaming, and response parsing.</summary>
    public class ModelsEndpointTests
    {
        /// <summary>Test HTTP client that records the last endpoint and payload and returns a configurable response object.</summary>
        private sealed class RecordingHttpClient : ISwarmHttpClient
        {
            public string? LastEndpoint { get; private set; }
            public JObject? LastPayload { get; private set; }
            public object? ResponseObject { get; set; }

            public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = payload as JObject ?? (payload != null ? JObject.FromObject(payload) : new JObject());

                if (ResponseObject is TResponse typed)
                {
                    return Task.FromResult(typed);
                }

                if (typeof(TResponse) == typeof(JObject) && ResponseObject is JObject jObject)
                {
                    return Task.FromResult((TResponse)(object)jObject);
                }
                return Task.FromResult(Activator.CreateInstance<TResponse>());
            }

            public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = JObject.FromObject(request);

                if (ResponseObject is TResponse typed)
                {
                    return Task.FromResult(typed);
                }

                if (typeof(TResponse) == typeof(JObject) && ResponseObject is JObject jObject)
                {
                    return Task.FromResult((TResponse)(object)jObject);
                }
                return Task.FromResult(Activator.CreateInstance<TResponse>());
            }
        }

        /// <summary>Test WebSocket client that records the last endpoint and payload and streams a predefined sequence of messages.</summary>
        private sealed class FakeWebSocketClient : ISwarmWebSocketClient
        {
            public string? LastEndpoint { get; private set; }
            public JObject? LastPayload { get; private set; }
            public List<JObject> MessagesToStream { get; } = new List<JObject>();

            public IAsyncEnumerable<TUpdate> StreamMessagesAsync<TUpdate>(string endpoint, object request, Func<JObject, TUpdate> messageParser, CancellationToken cancellationToken = default)
            {
                LastEndpoint = endpoint;
                LastPayload = request as JObject ?? JObject.FromObject(request);
                return StreamCore(endpoint, messageParser, cancellationToken);
            }

            private async IAsyncEnumerable<TUpdate> StreamCore<TUpdate>(string endpoint, Func<JObject, TUpdate> messageParser, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index;
                for (index = 0; index < MessagesToStream.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    JObject message = MessagesToStream[index];
                    TUpdate update = messageParser(message);
                    yield return update;
                    await Task.Yield();
                }
            }

            public Task GracefulCloseAsync(System.Net.WebSockets.ClientWebSocket webSocket, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DisconnectAllAsync()
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>Test implementation of <see cref="ISessionManager"/> that returns fixed session IDs.</summary>
        private sealed class DummySessionManager : ISessionManager
        {
            public Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult("session-1");
            }

            public Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult("session-2");
            }

            public void InvalidateSession()
            {
            }

            public string? CurrentSessionId => "session-1";
        }

        [Fact]
        public async Task ListModelsAsync_ShapesPayloadCorrectly()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            ModelsEndpoint endpoint = new ModelsEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            ModelListResponse response = await endpoint.ListModelsAsync(modelType: "Stable-Diffusion", path: "SDXL", depth: 2, sortBy: "Name", allowRemote: false, sortReverse: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(response);
            Assert.Equal("ListModels", httpClient.LastEndpoint);
            Assert.NotNull(httpClient.LastPayload);
            Assert.Equal("SDXL", httpClient.LastPayload!["path"]?.ToString());
            Assert.Equal(2, httpClient.LastPayload!["depth"]?.ToObject<int>());
            Assert.Equal("Stable-Diffusion", httpClient.LastPayload!["subtype"]?.ToString());
            Assert.Equal("Name", httpClient.LastPayload!["sortBy"]?.ToString());
            Assert.False(httpClient.LastPayload!["allowRemote"]?.ToObject<bool>() ?? true);
            Assert.True(httpClient.LastPayload!["sortReverse"]?.ToObject<bool>() ?? false);
        }

        [Fact]
        public async Task StreamModelDownloadAsync_UsesWebSocketClient()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            ModelsEndpoint endpoint = new ModelsEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            JObject message1 = new JObject
            {
                ["status"] = "downloading",
                ["progress"] = 0.5
            };

            JObject message2 = new JObject
            {
                ["status"] = "complete",
                ["progress"] = 1.0
            };

            webSocketClient.MessagesToStream.Add(message1);
            webSocketClient.MessagesToStream.Add(message2);

            List<ModelOperationUpdate> updates = new List<ModelOperationUpdate>();

            await foreach (ModelOperationUpdate update in endpoint.StreamModelDownloadAsync(url: "https://example.com/model.safetensors", modelType: "Stable-Diffusion", name: "model.safetensors", metadata: null, cancellationToken: CancellationToken.None))
            {
                updates.Add(update);
            }

            Assert.Equal("DoModelDownloadWS", webSocketClient.LastEndpoint);
            Assert.NotNull(webSocketClient.LastPayload);
            Assert.Equal("https://example.com/model.safetensors", webSocketClient.LastPayload!["url"]?.ToString());
            Assert.Equal("Stable-Diffusion", webSocketClient.LastPayload!["type"]?.ToString());
            Assert.Equal("model.safetensors", webSocketClient.LastPayload!["name"]?.ToString());
            Assert.Equal(2, updates.Count);
            Assert.Equal("downloading", updates[0].Status);
            Assert.Equal(0.5, updates[0].Progress);
            Assert.Equal("complete", updates[1].Status);
            Assert.Equal(1.0, updates[1].Progress);
        }

        [Fact]
        public async Task DescribeModelAsync_ParsesModelFromWrapperObject()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            ModelsEndpoint endpoint = new ModelsEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            JObject response = new JObject
            {
                ["model"] = JObject.FromObject(new ModelDescription
                {
                    Name = "flux-dev",
                    Description = "Test model"
                })
            };

            httpClient.ResponseObject = response;

            ModelDescription description = await endpoint.DescribeModelAsync("flux-dev", "Stable-Diffusion", CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(description);
            Assert.Equal("flux-dev", description.Name);
            Assert.Equal("Test model", description.Description);
        }
    }
}
