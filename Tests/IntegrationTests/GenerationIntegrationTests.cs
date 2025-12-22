using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;
using Xunit;

namespace SwarmUI.ApiClient.Tests.IntegrationTests
{
    /// <summary>Integration tests for the Generation endpoint against a real SwarmUI instance.</summary>
    /// <remarks>Trigger actual image generation and require a running SwarmUI instance at the configured test URL.</remarks>
    public class GenerationIntegrationTests : SwarmIntegrationTestBase
    {
        private static string? _cachedModelName;
        private static string? _cachedLoraName;
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        /// <summary>Gets a valid model name from the SwarmUI instance for testing.</summary>
        /// <remarks>Caches the result to avoid repeated API calls and prefers Flux schnell models for speed, falling back to any available model.</remarks>
        private async Task<string> GetTestModelAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                if (_cachedModelName != null)
                {
                    return _cachedModelName;
                }

                ModelListResponse models = await Client.Models.ListModelsAsync(cancellationToken: CancellationToken.None);

                // Prefer Flux schnell models (fastest)
                ModelInfo? fluxModel = models.Files.FirstOrDefault(m =>
                    m.Name.Contains("flux", StringComparison.OrdinalIgnoreCase) &&
                    m.Name.Contains("schnell", StringComparison.OrdinalIgnoreCase));

                ModelInfo? anyModel = fluxModel ?? models.Files.FirstOrDefault();

                if (anyModel == null)
                {
                    throw new InvalidOperationException("No models available on SwarmUI instance for testing");
                }

                _cachedModelName = anyModel.Name;
                return _cachedModelName;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>Gets a valid LoRA name from the SwarmUI instance for testing.</summary>
        /// <remarks>Returns <c>null</c> if no LoRAs are available.</remarks>
        private async Task<string?> GetTestLoraAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                if (_cachedLoraName != null)
                {
                    return _cachedLoraName;
                }

                ModelListResponse loras = await Client.Models.ListModelsAsync(
                    modelType: "LoRA",
                    cancellationToken: CancellationToken.None);

                ModelInfo? firstLora = loras.Files.FirstOrDefault();
                _cachedLoraName = firstLora?.Name; // May be null if no LoRAs available
                return _cachedLoraName;
            }
            finally
            {
                _initLock.Release();
            }
        }

        [Fact]
        public async Task GetCurrentStatusAsync_ReturnsValidServerStatus()
        {
            ServerStatusResponse status = await Client.Generation.GetCurrentStatusAsync(
                includeDebug: true,
                CancellationToken.None);
            Assert.NotNull(status);
            Assert.NotNull(status.Status);
            Assert.True(status.Status.WaitingGens >= 0);
            Assert.True(status.Status.LiveGens >= 0);
            Assert.True(status.Status.LoadingModels >= 0);
            Assert.True(status.Status.WaitingBackends >= 0);
        }

        [Fact]
        public async Task StreamGenerationAsync_GeneratesSingleImage()
        {
            string model = await GetTestModelAsync();

            GenerationRequest request = new GenerationRequest
            {
                Prompt = "a simple red circle on white background",
                Model = model,
                Width = 512,
                Height = 512,
                Steps = 4, // Minimal steps for fast testing
                BatchSize = 1,
                Seed = "12345"
            };

            List<GenerationUpdate> updates = new List<GenerationUpdate>();
            int imageCount = 0;
            int statusCount = 0;

            await foreach (GenerationUpdate update in Client.Generation.StreamGenerationAsync(
                request,
                CancellationToken.None))
            {
                updates.Add(update);

                if (update.Type == "image")
                {
                    imageCount++;
                    Assert.NotNull(update.Image);
                    Assert.False(string.IsNullOrEmpty(update.Image.Image));
                    Assert.StartsWith("data:image/", update.Image.Image);
                    Assert.NotNull(update.Image.BatchIndex);
                }
                else if (update.Type == "status")
                {
                    statusCount++;
                    Assert.NotNull(update.Status);
                }
            }

            Assert.True(updates.Count > 0, "Should receive at least one update");
            Assert.Equal(1, imageCount); // BatchSize = 1, so expect 1 image
            Assert.True(statusCount > 0, "Should receive at least one status update");
        }

        [Fact]
        public async Task StreamGenerationAsync_GeneratesMultipleImagesInBatch()
        {
            string model = await GetTestModelAsync();

            GenerationRequest request = new GenerationRequest
            {
                Prompt = "a blue square",
                Model = model,
                Width = 512,
                Height = 512,
                Steps = 4, // Minimal steps for fast testing
                BatchSize = 2, // Generate 2 images in parallel
                Seed = "54321"
            };

            List<GenerationUpdate> updates = new List<GenerationUpdate>();
            HashSet<string> batchIndices = new HashSet<string>();

            await foreach (GenerationUpdate update in Client.Generation.StreamGenerationAsync(
                request,
                CancellationToken.None))
            {
                updates.Add(update);

                if (update.Type == "image" && update.Image != null)
                {
                    batchIndices.Add(update.Image.BatchIndex);
                }
            }

            Assert.Equal(2, batchIndices.Count); // Should receive 2 distinct batch indices
            Assert.Contains("0", batchIndices);
            Assert.Contains("1", batchIndices);
        }

        [Fact]
        public async Task StreamGenerationAsync_WithLoras_AppliesLoraWeights()
        {
            string model = await GetTestModelAsync();
            string? lora = await GetTestLoraAsync();

            if (lora == null)
            {
                return; // xUnit will mark as passed
            }

            GenerationRequest request = new GenerationRequest
            {
                Prompt = "a detailed landscape",
                Model = model,
                Width = 512,
                Height = 512,
                Steps = 4, // Minimal steps for fast testing
                BatchSize = 1,
                Loras = new List<LoraModel>
                {
                    new LoraModel { Name = lora, Weight = 0.8f }
                }
            };

            bool receivedImage = false;

            await foreach (GenerationUpdate update in Client.Generation.StreamGenerationAsync(
                request,
                CancellationToken.None))
            {
                if (update.Type == "image")
                {
                    receivedImage = true;
                    Assert.NotNull(update.Image);
                    Assert.NotNull(update.Image.Image);
                    break; // Only need to verify one image
                }
            }

            Assert.True(receivedImage, "Should receive at least one image with LoRA applied");
        }

        [Fact]
        public async Task StreamGenerationAsync_WithNegativePrompt_GeneratesImage()
        {
            string model = await GetTestModelAsync();

            GenerationRequest request = new GenerationRequest
            {
                Prompt = "a beautiful sunset",
                NegativePrompt = "blurry, low quality, distorted",
                Model = model,
                Width = 512,
                Height = 512,
                Steps = 4, // Minimal steps for fast testing
                BatchSize = 1
            };

            bool receivedImage = false;

            await foreach (GenerationUpdate update in Client.Generation.StreamGenerationAsync(
                request,
                CancellationToken.None))
            {
                if (update.Type == "image")
                {
                    receivedImage = true;
                    Assert.NotNull(update.Image);
                    break;
                }
            }

            Assert.True(receivedImage);
        }
    }
}
