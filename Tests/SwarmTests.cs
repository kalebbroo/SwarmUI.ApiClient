using System;
using System.Net.Http;
using System.Threading.Tasks;
using SwarmUI.ApiClient;
using SwarmUI.ApiClient.Extensions.AudioLab;
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
            Assert.NotNull(client.Extensions);
            Assert.NotNull(client.Extensions.AudioLab);

            // The composition root threads SwarmClientOptions/ISessionManager all the way to AudioLabEndpoint,
            // so a client built the normal way (not via a test double) can create a voice session without
            // throwing -- this is the real wiring CreateVoiceSession's unit tests exercise with fakes instead.
            AudioLabVoiceSessionClient voiceSession = client.Extensions.AudioLab.CreateVoiceSession();
            Assert.NotNull(voiceSession);

            // SwarmClient has two SwarmExtensions construction sites -- the root client above, and
            // SessionScopedClient (ForSession) here -- both must thread the same options/sessionManager through.
            AudioLabVoiceSessionClient scopedVoiceSession = client.ForSession("some-user").Extensions.AudioLab.CreateVoiceSession();
            Assert.NotNull(scopedVoiceSession);
        }
    }
}
