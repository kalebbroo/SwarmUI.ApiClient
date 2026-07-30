using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Common;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Endpoints.Generation;

/// <summary>Provides access to SwarmUI text-to-image generation endpoints.</summary>
/// <remarks>Coordinates request shaping, WebSocket-based streaming, and related control operations (status, refresh, interrupt). See CodingGuidelines.md (Generation endpoint section) for implementation details.</remarks>
public class GenerationEndpoint : IGenerationEndpoint
{
    /// <summary>Internal implementation data containing dependencies for the generation endpoint.</summary>
    public struct Impl
    {
        /// <summary>HTTP client wrapper for non-streaming generation operations (status, refresh, interrupt, etc.).</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>WebSocket client for streaming generation via GenerateText2ImageWS.</summary>
        public ISwarmWebSocketClient WebSocketClient;

        /// <summary>Session manager used indirectly by the HTTP and WebSocket clients.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for generation operations.</summary>
        public ILogger<GenerationEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should go through the public members.</summary>
    public Impl Internal;

    /// <summary>Creates a new GenerationEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client wrapper for non-streaming operations. Must not be null.</param>
    /// <param name="webSocketClient">WebSocket client for streaming generation. Must not be null.</param>
    /// <param name="sessionManager">Session manager used indirectly by HTTP/WebSocket layers. Must not be null.</param>
    /// <param name="logger">Optional logger for generation operations. Uses NullLogger if null.</param>
    /// <remarks>Depends on injected HTTP, WebSocket, and session services; resource ownership stays in SwarmClient. See CodingGuidelines.md (Generation endpoint section) for DI and testing guidance.</remarks>
    public GenerationEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, ISessionManager sessionManager, ILogger<GenerationEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.WebSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<GenerationEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<GenerationUpdate> StreamGenerationAsync(GenerationRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt is required for generation", nameof(request));
        }
        if (request.BatchSize <= 0)
        {
            throw new ArgumentException("BatchSize must be greater than 0 for parallel generation", nameof(request));
        }
        if (request.Width <= 0 || request.Height <= 0)
        {
            throw new ArgumentException("Width and Height must be positive values", nameof(request));
        }
        Internal.Logger.LogInformation("Starting generation: prompt='{Prompt}' model='{Model}' size={Width}x{Height} steps={Steps} batchSize={BatchSize}", request.Prompt, request.Model ?? string.Empty, request.Width, request.Height, request.Steps, request.BatchSize);
        JObject payload = CreateGenerationPayload(request);
        int expectedImages = request.BatchSize;
        int receivedImages = 0;
        await foreach (GenerationUpdate update in Internal.WebSocketClient.StreamMessagesAsync<GenerationUpdate>(
            "GenerateText2ImageWS",
            payload,
            ParseGenerationMessage,
            cancellationToken))
        {
            if (update is null)
            {
                continue;
            }
            if (update.Type == "keep_alive")
            {
                Internal.Logger.LogDebug("Received keep-alive message during generation");
                continue;
            }
            if (update.Type == "image" && update.Image is not null)
            {
                receivedImages++;
                Internal.Logger.LogDebug("Received image {Received}/{Expected} for batch_index {BatchIndex}", receivedImages, expectedImages, update.Image.BatchIndex);
            }
            Internal.Logger.LogDebug("Received generation update of type '{Type}'", update.Type ?? "unknown");
            yield return update;
            if (update.Type == "status" &&
                update.Status is not null &&
                update.Status.WaitingGens == 0 &&
                update.Status.LiveGens == 0 &&
                receivedImages >= expectedImages)
            {
                Internal.Logger.LogInformation("All {Expected} images received and no live/waiting generations remain. Completing stream.", expectedImages);
                yield break;
            }
        }
        if (!cancellationToken.IsCancellationRequested && receivedImages < expectedImages)
        {
            Internal.Logger.LogWarning("Generation stream ended before all expected images were received. Expected={Expected} Received={Received}", expectedImages, receivedImages);
        }
    }

    /// <inheritdoc />
    public async Task<ServerStatusResponse> GetCurrentStatusAsync(bool includeDebug = false, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["do_debug"] = includeDebug
        };
        ServerStatusResponse response = await Internal.HttpClient.PostJsonAsync<ServerStatusResponse>("GetCurrentStatus", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async Task InterruptAllAsync(bool otherSessions = false, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["other_sessions"] = otherSessions
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("InterruptAll", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T2IParamsResponse> ListT2IParamsAsync(CancellationToken cancellationToken = default)
    {
        T2IParamsResponse response = await Internal.HttpClient.PostJsonAsync<T2IParamsResponse>("ListT2IParams", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async Task<T2IParamsResponse> TriggerRefreshAsync(bool strong = true, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["strong"] = strong
        };
        T2IParamsResponse response = await Internal.HttpClient.PostJsonAsync<T2IParamsResponse>("TriggerRefresh", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async Task ServerDebugMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }
        JObject payload = new()
        {
            ["message"] = message
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("ServerDebugMessage", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates the JSON payload for SwarmUI's GenerateText2ImageWS WebSocket endpoint
    /// from the high-level GenerationRequest model.</summary>
    /// <param name="request">High-level generation request containing prompt, model, and parameters.</param>
    /// <returns>JObject representing the payload expected by SwarmUI.</returns>
    private JObject CreateGenerationPayload(GenerationRequest request)
    {
        JObject payload = new()
        {
            ["images"] = request.BatchSize,
            ["batchsize"] = 1,
            ["prompt"] = request.Prompt,
            ["model"] = request.Model ?? string.Empty,
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["steps"] = request.Steps,
            ["cfgscale"] = request.CfgScale,
            ["sampler"] = request.Sampler,
            ["scheduler"] = request.Scheduler,
            ["donotsave"] = request.DoNotSave,
            ["imageformat"] = request.ImageFormat
        };
        if (!string.IsNullOrEmpty(request.NegativePrompt))
        {
            payload["negativeprompt"] = request.NegativePrompt;
        }
        if (!string.IsNullOrEmpty(request.Seed) && request.Seed != "-1")
        {
            payload["seed"] = request.Seed;
        }
        if (!string.IsNullOrEmpty(request.StylePreset))
        {
            payload["style_preset"] = request.StylePreset;
        }
        if (!string.IsNullOrEmpty(request.FluxGuidanceScale))
        {
            payload["fluxguidancescale"] = request.FluxGuidanceScale;
        }
        if (request.Loras is not null && request.Loras.Count > 0)
        {
            List<string> loraNames = new();
            List<string> loraWeights = new();
            foreach (LoraModel lora in request.Loras)
            {
                if (lora is not null && !string.IsNullOrWhiteSpace(lora.Name))
                {
                    loraNames.Add(lora.Name.Trim());
                    loraWeights.Add(lora.Weight.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            if (loraNames.Count > 0)
            {
                payload["loras"] = string.Join(",", loraNames);
                payload["loraweights"] = string.Join(",", loraWeights);
            }
        }
        if (!string.IsNullOrEmpty(request.InitImage))
        {
            payload["initimage"] = request.InitImage;
            payload["initimagecreativity"] = request.InitImageCreativity;
        }
        return payload;
    }

    /// <summary>Parses a raw WebSocket JSON message from SwarmUI into a <see cref="GenerationUpdate"/>.</summary>
    /// <param name="message">Raw JSON message as JObject.</param>
    /// <returns>Parsed GenerationUpdate instance describing the update.</returns>
    /// <remarks>Understands the main GenerateText2ImageWS message types (status, progress, image, discard, error, keep-alive). See T2IAPI.md and CodingGuidelines.md (Generation endpoint section) for full schema and parsing notes.</remarks>
    private GenerationUpdate ParseGenerationMessage(JObject message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        if (message["keep_alive"] is not null)
        {
            return new GenerationUpdate
            {
                Type = "keep_alive",
                KeepAlive = message["keep_alive"]
            };
        }
        if (message["status"] is not null)
        {
            GenerationUpdate statusUpdate = new()
            {
                Type = "status",
                Status = message["status"] is not null ? message["status"]!.ToObject<StatusInfo>() : null,
                BackendStatus = message["backend_status"] is not null ? message["backend_status"]!.ToObject<BackendStatus>() : null,
                SupportedFeatures = message["supported_features"] is not null ? message["supported_features"]!.ToObject<List<string>>() : null
            };
            return statusUpdate;
        }
        if (message["gen_progress"] is JObject progressObject)
        {
            ProgressInfo progress = new()
            {
                BatchIndex = progressObject["batch_index"] is not null ? progressObject["batch_index"]!.ToString() ?? string.Empty : string.Empty,
                OverallPercent = progressObject["overall_percent"] is not null ? progressObject["overall_percent"]!.ToObject<float>() : 0.0f,
                CurrentPercent = progressObject["current_percent"] is not null ? progressObject["current_percent"]!.ToObject<float>() : 0.0f,
                Preview = progressObject["preview"] is not null ? progressObject["preview"]!.ToString() : null
            };
            GenerationUpdate progressUpdate = new()
            {
                Type = "progress",
                Progress = progress
            };
            return progressUpdate;
        }
        if (message["image"] is not null)
        {
            ImageInfo imageInfo = new()
            {
                Image = message["image"] is not null ? message["image"]!.ToString() ?? string.Empty : string.Empty,
                BatchIndex = message["batch_index"] is not null ? message["batch_index"]!.ToString() ?? string.Empty : string.Empty,
                Metadata = message["metadata"] is not null ? message["metadata"]!.ToString() ?? string.Empty : string.Empty
            };
            GenerationUpdate imageUpdate = new()
            {
                Type = "image",
                Image = imageInfo
            };
            return imageUpdate;
        }
        if (message["discard_indices"] is JArray discardArray)
        {
            List<int>? indices = discardArray.ToObject<List<int>>();
            GenerationUpdate discardUpdate = new()
            {
                Type = "discard",
                DiscardIndices = indices
            };
            return discardUpdate;
        }
        if (message["error"] is not null)
        {
            string errorMessage = message["error"] is not null ? message["error"]!.ToString() ?? string.Empty : string.Empty;
            GenerationUpdate errorUpdate = new()
            {
                Type = "error",
                Error = new ErrorInfo
                {
                    Message = errorMessage
                }
            };
            return errorUpdate;
        }
        Internal.Logger.LogDebug("Received unknown/unhandled generation message format, skipping: {Message}", message.ToString());
        return null!;
    }
}
