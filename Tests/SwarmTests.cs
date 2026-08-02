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
                BaseUrl = Environment.GetEnvironmentVariable("SWARM_TEST_URL") ?? "http://localhost:7801",
                Authorization = Environment.GetEnvironmentVariable("SWARM_TEST_AUTH") ?? "",
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
