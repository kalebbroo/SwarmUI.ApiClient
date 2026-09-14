using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.APIBackends;
using SwarmUI.ApiClient.Extensions.APIBackends.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.APIBackends
{
    /// <summary>Unit tests for <see cref="APIBackendsEndpoint"/> verifying payload shaping, response parsing and extension metadata.</summary>
    public class APIBackendsEndpointTests
    {
        private static APIBackendsEndpoint CreateEndpoint(RecordingExtensionHttpClient httpClient)
        {
            return new APIBackendsEndpoint(httpClient, SwarmUI.ApiClient.Sessions.SwarmSessionKeys.Default, logger: null);
        }

        [Fact]
        public async Task ListModelCapabilitiesAsync_CallsTheEndpointAndParsesEveryField()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            httpClient.ResponseToReturn = new JObject
            {
                ["models"] = new JObject
                {
                    ["API Models/OpenAI/sora-2-t2v"] = new JObject
                    {
                        ["family"] = "video.openai_sora",
                        ["modality"] = "video",
                        ["init_image"] = false,
                        ["supports_batch"] = false,
                        ["flags"] = new JArray("openai_sora_params")
                    }
                }
            };
            APIBackendsEndpoint endpoint = CreateEndpoint(httpClient);

            ModelCapabilitiesResponse response = await endpoint.ListModelCapabilitiesAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("APIBackendsListModelCapabilities", httpClient.LastEndpoint);
            Assert.NotNull(response.Models);
            ApiModelCapability capability = response.Models!["API Models/OpenAI/sora-2-t2v"];
            Assert.Equal("video.openai_sora", capability.Family);
            Assert.Equal("video", capability.Modality);
            Assert.False(capability.InitImage);
            Assert.False(capability.SupportsBatch);
            Assert.Equal(new[] { "openai_sora_params" }, capability.Flags);
        }

        [Fact]
        public void ExtensionInfo_NamesEveryEndpointTheExtensionRegisters()
        {
            Assert.Equal("APIBackends", APIBackendsEndpoint.ExtensionInfo.Name);
            Assert.NotEmpty(APIBackendsEndpoint.ExtensionInfo.Endpoints);
            Assert.Contains("APIBackendsListModelCapabilities", APIBackendsEndpoint.ExtensionInfo.Endpoints);
        }
    }
}
