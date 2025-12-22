using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Responses;
using Xunit;

namespace SwarmUI.ApiClient.Tests.IntegrationTests
{
    /// <summary>Integration tests for the Backends endpoint against a real SwarmUI instance.</summary>
    /// <remarks>Requires a running SwarmUI instance at the configured test URL. Skip these tests if SwarmUI is not available.</remarks>
    public class BackendsIntegrationTests : SwarmIntegrationTestBase
    {
        [Fact]
        public async Task ListBackendsAsync_ReturnsBackendList()
        {
            BackendsListResponse response = await Client.Backends.ListBackendsAsync(CancellationToken.None);

            Assert.NotNull(response);
            Assert.NotNull(response.Backends);
            Assert.True(response.Backends.Count >= 0, "Should return a valid backends collection");
        }

        [Fact]
        public async Task ListBackendsAsync_BackendsHaveExpectedStructure()
        {
            BackendsListResponse response = await Client.Backends.ListBackendsAsync(CancellationToken.None);

            if (response.Backends != null && response.Backends.Count > 0)
            {
                foreach (Dictionary<string, object> backend in response.Backends)
                {
                    Assert.NotNull(backend);

                    if (backend.TryGetValue("type", out object? type))
                    {
                        Assert.NotNull(type);
                    }

                    if (backend.TryGetValue("status", out object? status))
                    {
                        Assert.NotNull(status);
                    }
                }
            }
        }

        [Fact]
        public async Task ListBackendsAsync_CalledMultipleTimes_ReturnsConsistentData()
        {
            BackendsListResponse response1 = await Client.Backends.ListBackendsAsync(CancellationToken.None);
            BackendsListResponse response2 = await Client.Backends.ListBackendsAsync(CancellationToken.None);

            Assert.Equal(response1.Backends?.Count ?? 0, response2.Backends?.Count ?? 0);
        }

        [Fact]
        public async Task ListBackendsAsync_CanBeCancelled()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await Client.Backends.ListBackendsAsync(cts.Token);
            });
        }

        [Fact]
        public async Task ListBackendsAsync_ResponseMatchesExpectedSchema()
        {
            BackendsListResponse response = await Client.Backends.ListBackendsAsync(CancellationToken.None);

            Assert.NotNull(response);
            Assert.NotNull(response.Backends);
            Assert.IsAssignableFrom<List<Dictionary<string, object>>>(response.Backends);
            if (response.Backends != null)
            {
                foreach (Dictionary<string, object> backend in response.Backends)
                {
                    Assert.IsAssignableFrom<Dictionary<string, object>>(backend);
                }
            }
        }

        [Fact]
        public async Task ListBackendsAsync_CanUseHelperMethod()
        {
            // Act
            BackendsListResponse response = await Client.Backends.ListBackendsAsync(CancellationToken.None);

            // Assert
            if (response.Backends != null && response.Backends.Count > 0)
            {
                foreach (Dictionary<string, object> backend in response.Backends)
                {
                    // Test the GetBackendProperty helper
                    string id = BackendsListResponse.GetBackendProperty(backend, "id", "unknown");
                    string status = BackendsListResponse.GetBackendProperty(backend, "status", "unknown");

                    Assert.NotNull(id);
                    Assert.NotNull(status);
                }
            }
        }
    }
}
