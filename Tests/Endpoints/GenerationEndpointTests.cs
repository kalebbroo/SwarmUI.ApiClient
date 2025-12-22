using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints
{
    /// <summary>Unit tests for <see cref="GenerationEndpoint"/> verifying WebSocket payload shaping and streaming behavior.</summary>
    public class GenerationEndpointTests
    {
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
                for (int index = 0; index < MessagesToStream.Count; index++)
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

        /// <summary>Test HTTP client that returns new instances of the requested response type.</summary>
        private sealed class DummyHttpClient : ISwarmHttpClient
        {
            public TResponse NextResponse<TResponse>() where TResponse : class
            {
                return Activator.CreateInstance<TResponse>();
            }

            public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
            {
                return Task.FromResult(NextResponse<TResponse>());
            }

            public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            {
                return Task.FromResult(NextResponse<TResponse>());
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
        public async Task StreamGenerationAsync_UsesExpectedPayloadAndParser()
        {
            DummyHttpClient httpClient = new DummyHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            GenerationEndpoint endpoint = new GenerationEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            GenerationRequest request = new GenerationRequest
            {
                Prompt = "sunset over mountains",
                Model = "flux-dev",
                Width = 1024,
                Height = 768,
                Steps = 20,
                BatchSize = 2,
                Seed = "42",
                NegativePrompt = "low quality",
                StylePreset = "photographic",
                FluxGuidanceScale = "2.0f",
                Loras = new List<LoraModel>
                {
                    new LoraModel { Name = "anime", Weight = 0.8f },
                    new LoraModel { Name = "detail", Weight = 1.2f }
                }
            };

            JObject statusMessage = new JObject
            {
                ["status"] = new JObject
                {
                    ["waiting_gens"] = 0,
                    ["live_gens"] = 0,
                    ["loading_models"] = 0,
                    ["waiting_backends"] = 0
                }
            };

            JObject imageMessage1 = new JObject
            {
                ["image"] = "data:image/png;base64,AAA",
                ["batch_index"] = "0",
                ["metadata"] = "{}"
            };

            JObject imageMessage2 = new JObject
            {
                ["image"] = "data:image/png;base64,BBB",
                ["batch_index"] = "1",
                ["metadata"] = "{}"
            };

            webSocketClient.MessagesToStream.Add(imageMessage1);
            webSocketClient.MessagesToStream.Add(imageMessage2);
            webSocketClient.MessagesToStream.Add(statusMessage);

            List<GenerationUpdate> updates = new List<GenerationUpdate>();
            await foreach (GenerationUpdate update in endpoint.StreamGenerationAsync(request, CancellationToken.None))
            {
                updates.Add(update);
            }

            Assert.Equal("GenerateText2ImageWS", webSocketClient.LastEndpoint);
            Assert.NotNull(webSocketClient.LastPayload);
            Assert.Equal(2, webSocketClient.LastPayload!["images"]?.ToObject<int>());
            Assert.Equal(1, webSocketClient.LastPayload!["batchsize"]?.ToObject<int>());
            Assert.Equal("sunset over mountains", webSocketClient.LastPayload!["prompt"]?.ToString());
            Assert.Equal("flux-dev", webSocketClient.LastPayload!["model"]?.ToString());

            Assert.Equal(3, updates.Count);
            Assert.Equal("image", updates[0].Type);
            Assert.Equal("0", updates[0].Image!.BatchIndex);
            Assert.Equal("image", updates[1].Type);
            Assert.Equal("1", updates[1].Image!.BatchIndex);
            Assert.Equal("status", updates[2].Type);
        }

        [Fact]
        public void StreamGenerationAsync_ValidatesRequest()
        {
            DummyHttpClient httpClient = new DummyHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            GenerationEndpoint endpoint = new GenerationEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            GenerationRequest nullPromptRequest = new GenerationRequest
            {
                Prompt = string.Empty,
                Width = 1024,
                Height = 768,
                BatchSize = 1
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (GenerationUpdate _ in endpoint.StreamGenerationAsync(nullPromptRequest, CancellationToken.None))
                {
                }
            });

            GenerationRequest invalidSizeRequest = new GenerationRequest
            {
                Prompt = "x",
                Width = 0,
                Height = 0,
                BatchSize = 1
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (GenerationUpdate _ in endpoint.StreamGenerationAsync(invalidSizeRequest, CancellationToken.None))
                {
                }
            });
        }

        [Fact]
        public async Task GetCurrentStatusAsync_CallsHttpClient()
        {
            DummyHttpClient httpClient = new DummyHttpClient();
            FakeWebSocketClient webSocketClient = new FakeWebSocketClient();
            DummySessionManager sessionManager = new DummySessionManager();
            GenerationEndpoint endpoint = new GenerationEndpoint(httpClient, webSocketClient, sessionManager, logger: null);

            ServerStatusResponse response = await endpoint.GetCurrentStatusAsync(includeDebug: true, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(response);
        }
    }
}
