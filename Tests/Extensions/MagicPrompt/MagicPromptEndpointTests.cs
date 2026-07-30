using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Extensions.MagicPrompt;
using SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.MagicPrompt
{
    /// <summary>Unit tests for <see cref="MagicPromptEndpoint"/> verifying payload shaping and extension metadata.</summary>
    public class MagicPromptEndpointTests
    {
        /// <summary>Builds an endpoint over recording doubles.</summary>
        private static MagicPromptEndpoint CreateEndpoint(RecordingExtensionHttpClient httpClient)
        {
            return new MagicPromptEndpoint(httpClient, new StubSessionManager(), logger: null);
        }

        [Fact]
        public async Task EnhancePromptAsync_MapsRequestToExpectedPayload()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject { ["success"] = true, ["response"] = "enhanced" };
            MagicPromptEndpoint endpoint = CreateEndpoint(httpClient);

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
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject { ["success"] = true, ["response"] = "enhanced" };
            MagicPromptEndpoint endpoint = CreateEndpoint(httpClient);

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
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            MagicPromptEndpoint endpoint = CreateEndpoint(httpClient);

            ISwarmExtensionEndpoint extensionEndpoint = endpoint;

            Assert.Equal("MagicPrompt", extensionEndpoint.Extension.Name);
            Assert.Contains("MagicPromptPhoneHome", extensionEndpoint.Extension.Endpoints);
            Assert.False(string.IsNullOrWhiteSpace(extensionEndpoint.Extension.RepositoryUrl));
        }

        [Fact]
        public void SwarmExtensions_ReportsEverySupportedExtension()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            SwarmExtensions extensions = new SwarmExtensions(httpClient, new RecordingExtensionWebSocketClient(), new StubSessionManager(), loggerFactory: null);

            Assert.Contains(extensions.All, info => info.Name == "AudioLab");
            Assert.Contains(extensions.All, info => info.Name == "LLMAssistant");
            Assert.Contains(extensions.All, info => info.Name == "MagicPrompt");
            Assert.Equal(extensions.All.Count, extensions.All.Select(info => info.Name).Distinct().Count());
            Assert.Equal(MagicPromptEndpoint.ExtensionInfo, extensions.MagicPrompt.Extension);
            Assert.All(extensions.All, info => Assert.NotEmpty(info.Endpoints));
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
