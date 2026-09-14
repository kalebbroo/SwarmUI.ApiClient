using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.HartsyInference;
using SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.HartsyInference
{
    /// <summary>Unit tests for <see cref="HartsyInferenceEndpoint"/> verifying payload shaping, response parsing and extension metadata.</summary>
    public class HartsyInferenceEndpointTests
    {
        private static HartsyInferenceEndpoint CreateEndpoint(RecordingExtensionHttpClient httpClient)
        {
            return new HartsyInferenceEndpoint(httpClient, SwarmUI.ApiClient.Sessions.SwarmSessionKeys.Default, logger: null);
        }

        [Fact]
        public async Task GetSupportedArchitecturesAsync_ParsesFeaturesAndSampling()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject
            {
                ["success"] = true,
                ["supported"] = new JArray("anima", "flux-1"),
                ["pending"] = new JObject { ["some-arch"] = "no recipe yet" },
                ["features"] = new JObject
                {
                    ["anima"] = new JArray("lora", "refiner", "img2img", "inpaint", "variationseed", "seamlesstiling"),
                    ["flux-1"] = new JArray()
                },
                ["sampling"] = new JObject
                {
                    ["anima"] = new JObject { ["samplers"] = new JArray("euler", "dpmpp_2m"), ["schedulers"] = new JArray("normal", "karras") },
                    ["flux-1"] = new JObject { ["samplers"] = new JArray(), ["schedulers"] = new JArray() }
                }
            };
            HartsyInferenceEndpoint endpoint = CreateEndpoint(httpClient);

            SupportedArchsResponse response = await endpoint.GetSupportedArchitecturesAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("HartsyInferenceGetSupportedArchs", httpClient.LastEndpoint);
            Assert.True(response.Success);
            Assert.Equal(new[] { "anima", "flux-1" }, response.Supported);
            Assert.Equal("no recipe yet", response.Pending["some-arch"]);
            Assert.Contains("lora", response.Features["anima"]);
            Assert.Empty(response.Features["flux-1"]);
            Assert.Equal(new[] { "euler", "dpmpp_2m" }, response.Sampling["anima"].Samplers);
            Assert.Empty(response.Sampling["flux-1"].Samplers);
        }

        [Fact]
        public async Task ProbeModelAsync_SendsModelNameAndParsesTheVerdict()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject
            {
                ["success"] = true,
                ["model_name"] = "SDXL/sd_xl_base_1.0.safetensors",
                ["arch_id"] = "stable-diffusion-xl-v1-base",
                ["compat_class"] = "stable-diffusion-xl-v1",
                ["state"] = "supported",
                ["reason"] = "HartsyInference can generate with this model"
            };
            HartsyInferenceEndpoint endpoint = CreateEndpoint(httpClient);

            ProbeModelResponse response = await endpoint.ProbeModelAsync("SDXL/sd_xl_base_1.0", CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("HartsyInferenceProbeModel", httpClient.LastEndpoint);
            Assert.Equal("SDXL/sd_xl_base_1.0", httpClient.LastPayload!["model_name"]?.ToString());
            Assert.Equal("stable-diffusion-xl-v1", response.CompatClass);
            Assert.Equal("supported", response.State);
        }

        [Fact]
        public async Task ProbeModelAsync_RejectsAnEmptyModelName()
        {
            HartsyInferenceEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient());
            await Assert.ThrowsAsync<ArgumentException>(() => endpoint.ProbeModelAsync("  ", CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ListLoadedPipelinesAsync_ParsesBackendPlacementAndUsage()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject
            {
                ["success"] = true,
                ["backends"] = new JArray(new JObject
                {
                    ["backend_id"] = 7,
                    ["status"] = "RUNNING",
                    ["current_model"] = "SDXL/sd_xl_base_1.0.safetensors",
                    ["compute_backend"] = "auto",
                    ["gpu_id"] = "0",
                    ["vae_gpu_id"] = "1",
                    ["vram_mode"] = "auto",
                    ["usages"] = 12
                })
            };
            HartsyInferenceEndpoint endpoint = CreateEndpoint(httpClient);

            LoadedPipelinesResponse response = await endpoint.ListLoadedPipelinesAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("HartsyInferenceListLoadedPipelines", httpClient.LastEndpoint);
            PipelineBackendInfo backend = Assert.Single(response.Backends);
            Assert.Equal(7, backend.BackendId);
            Assert.Equal("SDXL/sd_xl_base_1.0.safetensors", backend.CurrentModel);
            Assert.Equal("1", backend.VaeGpuId);
            Assert.Equal(12, backend.Usages);
        }

        [Fact]
        public async Task ClearCacheAsync_SendsBothArguments()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject { ["success"] = true, ["backends_cleared"] = 2 };
            HartsyInferenceEndpoint endpoint = CreateEndpoint(httpClient);

            ClearCacheResponse response = await endpoint.ClearCacheAsync(3, freeSystemRam: true, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("HartsyInferenceClearCache", httpClient.LastEndpoint);
            Assert.Equal(3, httpClient.LastPayload!["backend_id"]?.ToObject<int>());
            Assert.True(httpClient.LastPayload!["free_system_ram"]?.ToObject<bool>());
            Assert.Equal(2, response.BackendsCleared);
        }

        [Fact]
        public void ExtensionInfo_NamesEveryEndpointTheExtensionRegisters()
        {
            Assert.Equal(5, HartsyInferenceEndpoint.ExtensionInfo.Endpoints.Count);
            Assert.Contains("HartsyInferenceClearCache", HartsyInferenceEndpoint.ExtensionInfo.Endpoints);
        }
    }
}
