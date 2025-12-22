using System;
using System.Threading.Tasks;
using SwarmUI.ApiClient;
using SwarmUI.ApiClient.Models.Responses;
using Xunit;

namespace SwarmUI.ApiClient.Tests.IntegrationTests
{
    /// <summary>Base class for integration tests that connect to a real SwarmUI instance.</summary>
    /// <remarks>Requires SwarmUI to be running at <see cref="BaseUrl"/>; skip these tests if SwarmUI is not available.</remarks>
    [Trait("Category", "Integration")]
    public abstract class SwarmIntegrationTestBase : IAsyncLifetime
    {
        protected SwarmClient Client { get; private set; } = null!;

        protected const string BaseUrl = "http://localhost:7801";
        protected const string? Authorization = "";

        /// <summary>Creates default <see cref="SwarmClientOptions"/> for integration tests; override to customize per test class.</summary>
        protected virtual SwarmClientOptions CreateOptions()
        {
            return new SwarmClientOptions
            {
                BaseUrl = BaseUrl,
                Authorization = Authorization,
                HttpTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttempts = 2
            };
        }

        /// <summary>Initializes the <see cref="SwarmClient"/> and verifies that the SwarmUI instance is reachable before running tests.</summary>
        public async Task InitializeAsync()
        {
            Client = new SwarmClient(CreateOptions());

            try
            {
                ServerStatusResponse status = await Client.Generation.GetCurrentStatusAsync(includeDebug: false);
                if (status == null)
                {
                    throw new InvalidOperationException(
                        $"SwarmUI is not responding at {BaseUrl}. Please start SwarmUI before running integration tests.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to SwarmUI at {BaseUrl}. Please ensure SwarmUI is running before running integration tests.",
                    ex);
            }
        }

        /// <summary>Disposes the <see cref="SwarmClient"/> after integration tests complete.</summary>
        public async Task DisposeAsync()
        {
            if (Client != null)
            {
                await Client.DisposeAsync();
            }
        }
    }
}
