using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints
{
    /// <summary>Unit tests for <see cref="PresetsEndpoint"/> verifying payload shaping and error handling.</summary>
    public class PresetsEndpointTests
    {
        [Fact]
        public async Task AddNewPresetAsync_MapsRequestToExpectedPayload()
        {
            FakeSwarmHttpClient http = new();
            PresetsEndpoint endpoint = new(http, SwarmSessionKeys.Default);
            PresetRequest request = new()
            {
                Title = "My Preset",
                Description = "Desc",
                Parameters = new Dictionary<string, object>
                {
                    { "model", "flux" },
                    { "steps", 20 }
                },
                PreviewImage = "base64data",
                IsEdit = true,
                EditingName = "Old Preset"
            };
            await endpoint.AddNewPresetAsync(request);
            (string ep, JObject? payload, _) = Assert.Single(http.Requests);
            Assert.Equal("AddNewPreset", ep);
            Assert.Equal("My Preset", payload!["title"]?.ToString());
            Assert.Equal("Desc", payload["description"]?.ToString());
            Assert.Equal("base64data", payload["preview_image"]?.ToString());
            Assert.True(payload["is_edit"]?.ToObject<bool>() ?? false);
            Assert.Equal("Old Preset", payload["editing"]?.ToString());
            JObject? raw = payload["raw"] as JObject;
            Assert.NotNull(raw);
            JObject? paramMap = raw!["param_map"] as JObject;
            Assert.NotNull(paramMap);
            Assert.Equal("flux", paramMap!["model"]?.ToString());
        }

        [Fact]
        public async Task AddNewPresetAsync_ThrowsSwarmExceptionWhenServerReturnsPresetFail()
        {
            FakeSwarmHttpClient http = new()
            {
                Handler = (_, _, _) => new JObject { ["preset_fail"] = "Name already exists" }
            };
            PresetsEndpoint endpoint = new(http, SwarmSessionKeys.Default);
            PresetRequest request = new()
            {
                Title = "My Preset",
                Parameters = []
            };
            SwarmException ex = await Assert.ThrowsAsync<SwarmException>(() => endpoint.AddNewPresetAsync(request));
            Assert.Equal("preset_fail", ex.ErrorId);
        }
    }
}
