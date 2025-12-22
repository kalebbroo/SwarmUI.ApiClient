using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Responses;
using Xunit;

namespace SwarmUI.ApiClient.Tests.IntegrationTests
{
    /// <summary>Integration tests for the Models endpoint against a real SwarmUI instance.</summary>
    /// <remarks>Verify model operations and require a running SwarmUI instance at the configured test URL.</remarks>
    public class ModelsIntegrationTests : SwarmIntegrationTestBase
    {
        [Fact]
        public async Task ListModelsAsync_ReturnsModelList()
        {
            ModelListResponse response = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);

            Assert.NotNull(response);
            Assert.NotNull(response.Files);
            Assert.True(response.Files.Count > 0, "Should have at least one model available");

            ModelInfo firstModel = response.Files.First();
            Assert.False(string.IsNullOrEmpty(firstModel.Name));
        }

        [Fact]
        public async Task ListModelsAsync_ModelsHaveExpectedProperties()
        {
            ModelListResponse response = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);

            foreach (ModelInfo model in response.Files)
            {
                Assert.False(string.IsNullOrEmpty(model.Name), "Model name should not be empty");
                Assert.NotNull(model.Type);
                Assert.NotNull(model.Metadata);
            }
        }

        [Fact]
        public async Task ListModelsAsync_ContainsStableDiffusionModels()
        {
            ModelListResponse response = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);

            bool hasStableDiffusionModel = response.Files.Any(model =>
                model.Type.Contains("Stable-Diffusion", StringComparison.OrdinalIgnoreCase) ||
                model.Name.Contains("stable-diffusion", StringComparison.OrdinalIgnoreCase) ||
                model.Name.Contains("sd", StringComparison.OrdinalIgnoreCase) ||
                model.Name.Contains("xl", StringComparison.OrdinalIgnoreCase));

            Assert.True(hasStableDiffusionModel,
                "Should have at least one Stable Diffusion model. Available models: " +
                string.Join(", ", response.Files.Select(m => m.Name)));
        }

        [Fact]
        public async Task ListModelsAsync_CalledMultipleTimes_ReturnsSameData()
        {
            ModelListResponse response1 = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);
            ModelListResponse response2 = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);

            Assert.Equal(response1.Files.Count, response2.Files.Count);

            foreach (ModelInfo model in response1.Files)
            {
                Assert.True(response2.Files.Any(m => m.Name == model.Name),
                    $"Second response should contain model '{model.Name}'");
            }
        }

        [Fact]
        public async Task ListModelsAsync_CanBeCancelled()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await Client.Models.ListModelsAsync(cancellationToken: cts.Token);
            });
        }

        [Fact]
        public async Task ListModelsAsync_ResponseDeserializesCorrectly()
        {
            ModelListResponse response = await Client.Models.ListModelsAsync(
                cancellationToken: CancellationToken.None);

            Assert.NotNull(response);
            Assert.NotNull(response.Files);
            Assert.IsAssignableFrom<List<ModelInfo>>(response.Files);

            foreach (ModelInfo model in response.Files)
            {
                Assert.NotNull(model.Name);
                Assert.NotNull(model.Type);
                Assert.NotNull(model.Metadata);
                Assert.IsAssignableFrom<Dictionary<string, object>>(model.Metadata);
            }
        }

        [Fact]
        public async Task ListModelsAsync_WithDifferentModelTypes_Works()
        {
            // Act - Try listing different model types
            ModelListResponse sdModels = await Client.Models.ListModelsAsync(
                modelType: "Stable-Diffusion",
                cancellationToken: CancellationToken.None);

            ModelListResponse loraModels = await Client.Models.ListModelsAsync(
                modelType: "LoRA",
                cancellationToken: CancellationToken.None);

            // Assert - At minimum SD models should exist
            Assert.NotNull(sdModels);
            Assert.NotNull(sdModels.Files);
            Assert.True(sdModels.Files.Count > 0, "Should have Stable Diffusion models");

            // LoRAs may or may not exist
            Assert.NotNull(loraModels);
            Assert.NotNull(loraModels.Files);
        }
    }
}
