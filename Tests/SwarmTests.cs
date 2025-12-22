using System;
using System.Net.Http;
using System.Threading.Tasks;
using SwarmUI.ApiClient;
using Xunit;

namespace SwarmUI.ApiClient.Tests
{
    /// <summary>Basic integration test verifying that <see cref="SwarmClient"/> initializes all endpoint properties.</summary>
    public class SwarmClientTests
    {
        [Fact]
        public async Task Constructor_InitializesAllEndpoints()
        {
            SwarmClientOptions options = new()
            {
                BaseUrl = "http://192.168.0.163:7801",
                Authorization = "sui_auth_EPdaV_aVaw_Cr99IkUgKvA_hjW8fAWH",
                HttpTimeout = TimeSpan.FromSeconds(30)
            };

            await using SwarmClient client = new(options);

            Assert.NotNull(client.Generation);
            Assert.NotNull(client.Models);
            Assert.NotNull(client.Backends);
            Assert.NotNull(client.Presets);
            Assert.NotNull(client.User);
            Assert.NotNull(client.Admin);
        }
    }
}
