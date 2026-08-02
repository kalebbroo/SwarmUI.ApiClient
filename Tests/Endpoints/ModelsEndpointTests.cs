using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Common;
using SwarmUI.ApiClient.Contracts.Enums;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Endpoints.Models;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints
{
    /// <summary>Unit tests for <see cref="ModelsEndpoint"/> verifying payload shaping, WebSocket streaming, and response parsing.</summary>
    public class ModelsEndpointTests
    {
        private static (ModelsEndpoint Endpoint, FakeSwarmHttpClient Http, FakeSwarmWebSocketClient Ws) Create(string sessionKey = SwarmSessionKeys.Default)
        {
            FakeSwarmHttpClient http = new();
            FakeSwarmWebSocketClient ws = new();
            return (new ModelsEndpoint(http, ws, sessionKey), http, ws);
        }

        [Fact]
        public async Task ListModelsAsync_ShapesPayloadCorrectly()
        {
            (ModelsEndpoint endpoint, FakeSwarmHttpClient http, _) = Create();
            ModelListResponse response = await endpoint.ListModelsAsync(modelType: "Stable-Diffusion", path: "SDXL", depth: 2, sortBy: "Name", allowRemote: false, sortReverse: true);
            Assert.NotNull(response);
            (string ep, JObject? payload, string sessionKey) = Assert.Single(http.Requests);
            Assert.Equal("ListModels", ep);
            Assert.Equal(SwarmSessionKeys.Default, sessionKey);
            Assert.Equal("SDXL", payload!["path"]?.ToString());
            Assert.Equal(2, payload["depth"]?.ToObject<int>());
            Assert.Equal("Stable-Diffusion", payload["subtype"]?.ToString());
            Assert.Equal("Name", payload["sortBy"]?.ToString());
            Assert.False(payload["allowRemote"]?.ToObject<bool>() ?? true);
            Assert.True(payload["sortReverse"]?.ToObject<bool>() ?? false);
        }

        [Fact]
        public async Task StreamModelDownloadAsync_UsesWebSocketClient()
        {
            (ModelsEndpoint endpoint, _, FakeSwarmWebSocketClient ws) = Create();
            ws.FrameScript = (_, _) =>
            [
                new JObject { ["status"] = "downloading", ["progress"] = 0.5 },
                new JObject { ["status"] = "complete", ["progress"] = 1.0 }
            ];
            List<ModelOperationUpdate> updates = [];
            await foreach (ModelOperationUpdate update in endpoint.StreamModelDownloadAsync(url: "https://example.com/model.safetensors", modelType: "Stable-Diffusion", name: "model.safetensors"))
            {
                updates.Add(update);
            }
            (string ep, JObject request, _) = Assert.Single(ws.Streams);
            Assert.Equal("DoModelDownloadWS", ep);
            Assert.Equal("https://example.com/model.safetensors", request["url"]?.ToString());
            Assert.Equal("Stable-Diffusion", request["type"]?.ToString());
            Assert.Equal("model.safetensors", request["name"]?.ToString());
            Assert.Equal(2, updates.Count);
            Assert.Equal("downloading", updates[0].Status);
            Assert.Equal(0.5, updates[0].Progress);
            Assert.Equal("complete", updates[1].Status);
            Assert.Equal(1.0, updates[1].Progress);
        }

        [Fact]
        public async Task StreamModelDownloadAsync_TypedOverload_ConvertsSubTypeEnumToApiString()
        {
            (ModelsEndpoint endpoint, _, FakeSwarmWebSocketClient ws) = Create();
            ws.FrameScript = (_, _) => [new JObject { ["status"] = "complete", ["progress"] = 1.0 }];
            await foreach (ModelOperationUpdate _ in endpoint.StreamModelDownloadAsync(
                url: "https://example.com/lora.safetensors",
                subType: SwarmSubType.LoRA,
                name: "BFL/Flux1/example.safetensors"))
            {
            }
            (string ep, JObject request, _) = Assert.Single(ws.Streams);
            Assert.Equal("DoModelDownloadWS", ep);
            Assert.Equal("LoRA", request["type"]?.ToString());
        }

        [Fact]
        public void StreamModelDownloadAsync_ValidatesEagerly()
        {
            (ModelsEndpoint endpoint, _, _) = Create();
            Assert.Throws<ArgumentException>(() => endpoint.StreamModelDownloadAsync(url: "", modelType: "LoRA", name: "x"));
            Assert.Throws<ArgumentException>(() => endpoint.StreamModelDownloadAsync(url: "https://x", modelType: "", name: "x"));
            Assert.Throws<ArgumentException>(() => endpoint.StreamModelDownloadAsync(url: "https://x", modelType: "LoRA", name: ""));
        }

        [Fact]
        public async Task DescribeModelAsync_ParsesModelFromWrapperObject()
        {
            (ModelsEndpoint endpoint, FakeSwarmHttpClient http, _) = Create();
            http.Handler = (_, _, _) => new JObject
            {
                ["model"] = JObject.FromObject(new ModelDescription
                {
                    Name = "flux-dev",
                    Description = "Test model"
                })
            };
            ModelDescription description = await endpoint.DescribeModelAsync("flux-dev", "Stable-Diffusion");
            Assert.Equal("flux-dev", description.Name);
            Assert.Equal("Test model", description.Description);
        }

        [Fact]
        public async Task DescribeModelAsync_UnusableResponse_ThrowsInsteadOfFabricatingSuccess()
        {
            (ModelsEndpoint endpoint, FakeSwarmHttpClient http, _) = Create();
            http.Handler = (_, _, _) => [];
            await Assert.ThrowsAsync<SwarmException>(() => endpoint.DescribeModelAsync("missing-model"));
        }
    }
}
