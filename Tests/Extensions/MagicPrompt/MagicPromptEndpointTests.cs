using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Extensions.MagicPrompt;
using SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.MagicPrompt
{
    /// <summary>Unit tests for <see cref="MagicPromptEndpoint"/> verifying payload shaping and extension metadata.</summary>
    public class MagicPromptEndpointTests
    {
        /// <summary>Test HTTP client that records the last endpoint and payload and returns a configurable response.</summary>
        private sealed class RecordingHttpClient : ISwarmHttpClient
        {
            public string? LastEndpoint { get; private set; }
            public JObject? LastPayload { get; private set; }
            public MagicPromptResponse ResponseToReturn { get; set; } = new MagicPromptResponse { Success = true, Response = "enhanced" };

            public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = payload as JObject ?? (payload is not null ? JObject.FromObject(payload) : new JObject());
                throw new NotSupportedException("RecordingHttpClient only supports typed request overloads.");
            }

            public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = JObject.FromObject(request);
                return Task.FromResult((TResponse)(object)ResponseToReturn);
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
        public async Task EnhancePromptAsync_MapsRequestToExpectedPayload()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            DummySessionManager sessionManager = new DummySessionManager();
            MagicPromptEndpoint endpoint = new MagicPromptEndpoint(httpClient, sessionManager, logger: null);

            MagicPromptRequest request = new MagicPromptRequest
            {
                Content = new MessageContent { Text = "a cat" },
                ModelId = "claude-3-5-haiku-20241022",
                Seed = 42
            };

            MagicPromptResponse response = await endpoint.EnhancePromptAsync(request, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("MagicPromptPhoneHome", httpClient.LastEndpoint);
            Assert.NotNull(httpClient.LastPayload);
            Assert.Equal("a cat", httpClient.LastPayload!["messageContent"]?["text"]?.ToString());
            Assert.Equal("claude-3-5-haiku-20241022", httpClient.LastPayload!["modelId"]?.ToString());
            Assert.Equal("Text", httpClient.LastPayload!["messageType"]?.ToString());
            Assert.Equal("chat", httpClient.LastPayload!["action"]?.ToString());
            Assert.Equal(42, httpClient.LastPayload!["seed"]?.ToObject<long>());
            Assert.True(response.Success);
            Assert.Equal("enhanced", response.Response);
        }

        [Fact]
        public async Task EnhancePromptAsync_ThrowsWhenTextIsEmpty()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            DummySessionManager sessionManager = new DummySessionManager();
            MagicPromptEndpoint endpoint = new MagicPromptEndpoint(httpClient, sessionManager, logger: null);

            MagicPromptRequest request = new MagicPromptRequest
            {
                Content = new MessageContent { Text = "   " }
            };

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await endpoint.EnhancePromptAsync(request, CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public void Endpoint_ExposesExtensionMetadata()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            DummySessionManager sessionManager = new DummySessionManager();
            MagicPromptEndpoint endpoint = new MagicPromptEndpoint(httpClient, sessionManager, logger: null);

            ISwarmExtensionEndpoint extensionEndpoint = endpoint;

            Assert.Equal("MagicPrompt", extensionEndpoint.Extension.Name);
            Assert.Contains("MagicPromptPhoneHome", extensionEndpoint.Extension.Endpoints);
            Assert.False(string.IsNullOrWhiteSpace(extensionEndpoint.Extension.RepositoryUrl));
        }

        [Fact]
        public void SwarmExtensions_ReportsEverySupportedExtension()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            DummySessionManager sessionManager = new DummySessionManager();
            SwarmExtensions extensions = new SwarmExtensions(httpClient, sessionManager, loggerFactory: null);

            Assert.NotEmpty(extensions.All);
            Assert.Contains(extensions.All, info => info.Name == "MagicPrompt");
            Assert.Equal(MagicPromptEndpoint.ExtensionInfo, extensions.MagicPrompt.Extension);
        }

        [Fact]
        public void MagicPromptResponse_DeserializesServerErrorShape()
        {
            string json = "{\"success\":false,\"error\":\"no backend\",\"error_id\":\"llm_unavailable\"}";

            MagicPromptResponse? response = JsonConvert.DeserializeObject<MagicPromptResponse>(json);

            Assert.NotNull(response);
            Assert.False(response!.Success);
            Assert.Equal("no backend", response.Error);
            Assert.Equal("llm_unavailable", response.ErrorId);
        }
    }
}
