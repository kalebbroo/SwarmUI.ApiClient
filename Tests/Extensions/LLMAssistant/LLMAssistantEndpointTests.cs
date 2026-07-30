using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Extensions.LLMAssistant;
using SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.LLMAssistant
{
    /// <summary>Unit tests for <see cref="LLMAssistantEndpoint"/> covering payload shaping, scoped writes, and streamed chat frames.</summary>
    public class LLMAssistantEndpointTests
    {
        /// <summary>Builds an endpoint over recording doubles.</summary>
        private static LLMAssistantEndpoint CreateEndpoint(RecordingExtensionHttpClient httpClient, RecordingExtensionWebSocketClient webSocketClient)
        {
            return new LLMAssistantEndpoint(httpClient, webSocketClient, new StubSessionManager(), logger: null);
        }

        [Fact]
        public async Task CompleteAsync_MapsRequestToExpectedPayload()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["response"] = "enhanced text" }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            ChatCompletionResponse response = await endpoint.CompleteAsync(new ChatCompletionRequest
            {
                Message = "a cat",
                Model = "llama-3",
                Temperature = 0.7,
                MaxTokens = 256,
                NoCache = true
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantSendMessage", httpClient.LastEndpoint);
            Assert.Equal("a cat", httpClient.LastPayload!["message"]?.ToString());
            Assert.Equal("llama-3", httpClient.LastPayload!["model"]?.ToString());
            Assert.Equal(0.7, httpClient.LastPayload!["temperature"]?.ToObject<double>());
            Assert.Equal(256, httpClient.LastPayload!["maxTokens"]?.ToObject<int>());
            Assert.True(httpClient.LastPayload!["noCache"]?.ToObject<bool>());
            Assert.Equal("enhanced text", response.Response);
        }

        [Fact]
        public async Task StreamMessageAsync_SendsThreadPayloadAndPreservesRawFrames()
        {
            RecordingExtensionWebSocketClient webSocketClient = new RecordingExtensionWebSocketClient
            {
                FramesToReturn =
                [
                    new JObject { ["token"] = "Hel" },
                    new JObject { ["token"] = "lo" },
                    new JObject { ["error"] = "backend died", ["lane"] = 1 }
                ]
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), webSocketClient);

            List<ChatStreamUpdate> updates = [];
            await foreach (ChatStreamUpdate update in endpoint.StreamMessageAsync(new ChatStreamRequest
            {
                ThreadId = "thread-1",
                Message = "hi"
            }, CancellationToken.None))
            {
                updates.Add(update);
            }

            Assert.Equal("LLMAssistantSendMessageWS", webSocketClient.LastEndpoint);
            Assert.Equal("thread-1", webSocketClient.LastPayload!["threadId"]?.ToString());
            Assert.Equal(3, updates.Count);
            Assert.Equal("Hel", updates[0].Raw["token"]?.ToString());
            Assert.False(updates[0].IsError);
            Assert.True(updates[2].IsError);
            Assert.Equal("backend died", updates[2].Error);
            Assert.Equal(1, updates[2].Lane);
        }

        [Fact]
        public void StreamEditMessageAsync_RequiresMessageIdAndContent()
        {
            LLMAssistantEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), new RecordingExtensionWebSocketClient());

            Assert.Throws<ArgumentException>(() => endpoint.StreamEditMessageAsync(new ChatStreamRequest { ThreadId = "thread-1" }));
            Assert.Throws<ArgumentException>(() => endpoint.StreamEditMessageAsync(new ChatStreamRequest { ThreadId = "thread-1", MessageId = "msg-1" }));
        }

        [Fact]
        public async Task SetThreadToolsEnabledAsync_OmitsEnabledToClearTheOverride()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["threadId"] = "thread-1", ["toolsEnabled"] = null }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            ThreadToolsEnabledResponse cleared = await endpoint.SetThreadToolsEnabledAsync("thread-1", enabled: null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantSetThreadToolsEnabled", httpClient.LastEndpoint);
            Assert.Equal("thread-1", httpClient.LastPayload!["threadId"]?.ToString());
            Assert.Null(httpClient.LastPayload!["enabled"]);
            Assert.Null(cleared.ToolsEnabled);

            await endpoint.SetThreadToolsEnabledAsync("thread-1", enabled: true, CancellationToken.None).ConfigureAwait(false);

            Assert.True(httpClient.LastPayload!["enabled"]?.ToObject<bool>());
        }

        [Fact]
        public async Task SaveAssistantAsync_SendsAssistantAndScope()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["id"] = "asst-1", ["scope"] = "shared" }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            ScopedWriteResponse response = await endpoint.SaveAssistantAsync(new SaveAssistantRequest
            {
                Assistant = new JObject { ["name"] = "Swarmie" },
                Scope = "shared"
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantSaveAssistant", httpClient.LastEndpoint);
            Assert.Equal("Swarmie", httpClient.LastPayload!["assistant"]?["name"]?.ToString());
            Assert.Equal("shared", httpClient.LastPayload!["scope"]?.ToString());
            Assert.Equal("asst-1", response.Id);
            Assert.Equal("shared", response.Scope);
        }

        [Fact]
        public async Task DeleteToolAsync_IncludesScopeOnlyWhenSupplied()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            await endpoint.DeleteToolAsync("tool-1", scope: null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantDeleteTool", httpClient.LastEndpoint);
            Assert.Equal("tool-1", httpClient.LastPayload!["toolId"]?.ToString());
            Assert.Null(httpClient.LastPayload!["scope"]);

            await endpoint.DeleteToolAsync("tool-1", scope: "shared", CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("shared", httpClient.LastPayload!["scope"]?.ToString());
        }

        [Fact]
        public async Task GetModelsAsync_SurfacesPerProviderWarnings()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = true,
                    ["models"] = new JArray(new JObject { ["name"] = "llama-3", ["title"] = "llama-3" }),
                    ["warnings"] = new JArray("Ollama: did not respond within 8s (backend unreachable?)")
                }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            LLMModelsResponse response = await endpoint.GetModelsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantGetModels", httpClient.LastEndpoint);
            Assert.Single(response.Models);
            Assert.Single(response.Warnings);
            Assert.Contains("Ollama", response.Warnings[0]);
        }

        [Fact]
        public async Task ExecuteToolAsync_MapsArgumentsAndEchoesCallId()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = true,
                    ["result"] = new JObject { ["images"] = new JArray("out.png") },
                    ["callId"] = "call-9"
                }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            ExecuteToolResponse response = await endpoint.ExecuteToolAsync(new ExecuteToolRequest
            {
                ToolId = "generate_image",
                Arguments = new JObject { ["prompt"] = "a cat" },
                CallId = "call-9",
                ThreadId = "thread-1"
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("LLMAssistantExecuteTool", httpClient.LastEndpoint);
            Assert.Equal("generate_image", httpClient.LastPayload!["toolId"]?.ToString());
            Assert.Equal("a cat", httpClient.LastPayload!["arguments"]?["prompt"]?.ToString());
            Assert.Equal("thread-1", httpClient.LastPayload!["threadId"]?.ToString());
            Assert.Equal("call-9", response.CallId);
            Assert.NotNull(response.Result);
        }

        [Fact]
        public async Task GetCompanionContextAsync_HandlesEmptyImageHistory()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["lastImage"] = null }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            CompanionContextResponse response = await endpoint.GetCompanionContextAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.True(response.Success);
            Assert.Null(response.LastImage);
        }

        [Fact]
        public async Task ExportThreadAsync_DefaultsToJsonFormat()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["content"] = "{}", ["filename"] = "chat.json" }
            };
            LLMAssistantEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            ThreadExportResponse response = await endpoint.ExportThreadAsync("thread-1", cancellationToken: CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("json", httpClient.LastPayload!["format"]?.ToString());
            Assert.Equal("chat.json", response.Filename);
        }

        [Fact]
        public void Endpoint_ExposesExtensionMetadata()
        {
            ISwarmExtensionEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), new RecordingExtensionWebSocketClient());

            Assert.Equal("LLMAssistant", endpoint.Extension.Name);
            Assert.Contains("LLMAssistantSendMessageWS", endpoint.Extension.Endpoints);
            Assert.Contains("LLMAssistantGetThreads", endpoint.Extension.Endpoints);
            Assert.False(string.IsNullOrWhiteSpace(endpoint.Extension.RepositoryUrl));
        }
    }
}
