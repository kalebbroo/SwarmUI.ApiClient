using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Common;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Endpoints.Generation;

/// <summary>Provides access to SwarmUI text-to-image generation endpoints.</summary>
/// <remarks>Streaming follows the server's actual lifecycle: frames flow until a <c>socket_intention:"close"</c> frame marks the batch done, at which point exactly one terminal "complete" update is emitted. Image counting is never used for completion detection — discarded indices, grid composites (batch_index "-1"), intermediates, and API-backend image counts all make counters unreliable.</remarks>
public class GenerationEndpoint : IGenerationEndpoint
{
    /// <summary>Serializer honoring the [JsonProperty] wire names on GenerationRequest and omitting nulls.</summary>
    private static readonly JsonSerializer PayloadSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    });

    private readonly ISwarmHttpClient _httpClient;
    private readonly ISwarmWebSocketClient _webSocketClient;
    private readonly string _sessionKey;
    private readonly ILogger<GenerationEndpoint> _logger;

    /// <summary>Creates a new GenerationEndpoint.</summary>
    /// <param name="httpClient">HTTP client wrapper for non-streaming operations.</param>
    /// <param name="webSocketClient">WebSocket client for streaming generation.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public GenerationEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, ILogger<GenerationEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<GenerationEndpoint>.Instance;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<GenerationUpdate> StreamGenerationAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        // Validate eagerly so failures throw at the call site, not at first enumeration.
        ArgumentNullException.ThrowIfNull(request);
        bool hasAudioInput = !string.IsNullOrWhiteSpace(request.AudioInput) || !string.IsNullOrWhiteSpace(request.SourceAudio);
        if (string.IsNullOrWhiteSpace(request.Prompt) && !hasAudioInput)
        {
            throw new ArgumentException("Generation needs a prompt, or an audio input for models that transcribe or convert audio", nameof(request));
        }
        if (request.Images is < 1 or > 10000)
        {
            throw new ArgumentException("Images must be between 1 and 10000", nameof(request));
        }
        if (request.BatchSize is < 1 or > 100)
        {
            throw new ArgumentException("BatchSize must be between 1 and 100", nameof(request));
        }
        if (request.Width <= 0 || request.Height <= 0)
        {
            throw new ArgumentException("Width and Height must be positive values", nameof(request));
        }
        JObject payload = CreateGenerationPayload(request);
        return StreamGenerationCoreAsync(payload, request, cancellationToken);
    }

    /// <summary>Streaming core: consumes raw frames and emits typed updates, ending with exactly one "complete" update.</summary>
    private async IAsyncEnumerable<GenerationUpdate> StreamGenerationCoreAsync(JObject payload, GenerationRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting generation: model='{Model}' size={Width}x{Height} steps={Steps} images={Images} batchsize={BatchSize}", request.Model ?? string.Empty, request.Width, request.Height, request.Steps, request.Images, request.BatchSize);
        int imagesReceived = 0;
        List<int> discardedIndices = [];
        List<ErrorInfo> errors = [];
        bool sawSocketIntentionClose = false;
        await foreach (JObject frame in _webSocketClient.StreamFramesAsync("GenerateText2ImageWS", payload, _sessionKey, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(frame["socket_intention"]?.ToString(), "close", StringComparison.OrdinalIgnoreCase))
            {
                sawSocketIntentionClose = true;
                break;
            }
            // A single frame can carry multiple keys; emit one update per recognized key so nothing is dropped.
            foreach (GenerationUpdate update in ParseFrame(frame))
            {
                switch (update.Type)
                {
                    case "image":
                        imagesReceived++;
                        break;
                    case "discard" when update.DiscardIndices is not null:
                        discardedIndices.AddRange(update.DiscardIndices);
                        break;
                    case "error" when update.Error is not null:
                        errors.Add(update.Error);
                        break;
                }
                yield return update;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (!sawSocketIntentionClose)
        {
            // The server always signals a finished batch with socket_intention:"close"; without it the
            // connection died mid-flight. Record it as an error so consumers always have a reason to
            // show — a failure with an empty Errors list leaves callers with nothing but "unknown".
            _logger.LogWarning("Generation stream for session key '{Key}' ended without a socket_intention close frame (server closed early)", _sessionKey);
            errors.Add(new ErrorInfo
            {
                Message = "The generation stream ended before the server signaled completion, so the result is unconfirmed. The connection may have dropped; work already queued keeps running on the server until interrupted.",
                ErrorId = ErrorInfo.StreamEndedEarlyErrorId
            });
        }
        bool succeeded = imagesReceived > 0 && errors.Count == 0;
        _logger.LogInformation("Generation complete: succeeded={Succeeded} images={Images} discards={Discards} errors={Errors}", succeeded, imagesReceived, discardedIndices.Count, errors.Count);
        yield return new GenerationUpdate
        {
            Type = "complete",
            Completion = new CompletionInfo
            {
                Succeeded = succeeded,
                ImagesReceived = imagesReceived,
                DiscardedIndices = discardedIndices,
                Errors = errors
            }
        };
    }

    /// <summary>Parses one raw server frame into zero or more typed updates. Frames may combine keys (e.g. an error alongside a status); all recognized keys are emitted.</summary>
    private IEnumerable<GenerationUpdate> ParseFrame(JObject frame)
    {
        bool recognized = false;
        if (frame["status"] is JObject statusObj)
        {
            recognized = true;
            yield return new GenerationUpdate
            {
                Type = "status",
                Status = statusObj.ToObject<StatusInfo>(),
                BackendStatus = frame["backend_status"] is JObject backendObj ? backendObj.ToObject<BackendStatus>() : null,
                SupportedFeatures = frame["supported_features"] is JArray featuresArr ? featuresArr.ToObject<List<string>>() : null
            };
        }
        if (frame["gen_progress"] is JObject progressObj)
        {
            recognized = true;
            yield return new GenerationUpdate
            {
                Type = "progress",
                Progress = new ProgressInfo
                {
                    BatchIndex = progressObj["batch_index"]?.ToString() ?? string.Empty,
                    OverallPercent = progressObj["overall_percent"]?.ToObject<float>() ?? 0.0f,
                    CurrentPercent = progressObj["current_percent"]?.ToObject<float>() ?? 0.0f,
                    Preview = progressObj["preview"]?.ToString()
                }
            };
        }
        if (frame["image"] is not null)
        {
            recognized = true;
            yield return new GenerationUpdate
            {
                Type = "image",
                Image = new ImageInfo
                {
                    Image = frame["image"]?.ToString() ?? string.Empty,
                    BatchIndex = frame["batch_index"]?.ToString() ?? string.Empty,
                    RequestId = frame["request_id"]?.ToString(),
                    Metadata = frame["metadata"] is null or { Type: JTokenType.Null } ? null : frame["metadata"]!.ToString()
                }
            };
        }
        if (frame["discard_indices"] is JArray discardArray)
        {
            recognized = true;
            yield return new GenerationUpdate
            {
                Type = "discard",
                DiscardIndices = discardArray.ToObject<List<int>>()
            };
        }
        if (frame["error"] is not null)
        {
            recognized = true;
            yield return new GenerationUpdate
            {
                Type = "error",
                Error = new ErrorInfo
                {
                    Message = frame["error"]!.Type == JTokenType.String ? frame["error"]!.ToString() : frame["error"]!.ToString(Formatting.None),
                    ErrorId = frame["error_id"]?.ToString()
                }
            };
        }
        if (!recognized && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Ignoring unrecognized generation frame with keys: {Keys}", string.Join(",", frame.Properties().Select(p => p.Name)));
        }
    }

    /// <inheritdoc />
    public async Task<ServerStatusResponse> GetCurrentStatusAsync(bool includeDebug = false, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["do_debug"] = includeDebug
        };
        return await _httpClient.PostJsonAsync<ServerStatusResponse>("GetCurrentStatus", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InterruptAllAsync(bool otherSessions = false, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["other_sessions"] = otherSessions
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("InterruptAll", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T2IParamsResponse> ListT2IParamsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.PostJsonAsync<T2IParamsResponse>("ListT2IParams", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T2IParamsResponse> TriggerRefreshAsync(bool strong = true, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["strong"] = strong
        };
        return await _httpClient.PostJsonAsync<T2IParamsResponse>("TriggerRefresh", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
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
        JObject _ = await _httpClient.PostJsonAsync<JObject>("ServerDebugMessage", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds the GenerateText2ImageWS payload from the request's [JsonProperty] wire names, then applies the LoRA post-pass.</summary>
    /// <remarks>LoRAs are sent as parallel JSON arrays (never comma-joined — LoRA names may legally contain commas) with full-precision weights. Internal so payload round-trip tests can verify every property reaches the wire.</remarks>
    internal static JObject CreateGenerationPayload(GenerationRequest request)
    {
        JObject payload = JObject.FromObject(request, PayloadSerializer);
        if (request.Loras is { Count: > 0 })
        {
            List<string> loraNames = [];
            List<string> loraWeights = [];
            foreach (LoraModel lora in request.Loras)
            {
                if (lora is not null && !string.IsNullOrWhiteSpace(lora.Name))
                {
                    loraNames.Add(lora.Name.Trim());
                    loraWeights.Add(lora.Weight.ToString("0.####", CultureInfo.InvariantCulture));
                }
            }
            if (loraNames.Count > 0)
            {
                payload["loras"] = new JArray(loraNames);
                payload["loraweights"] = new JArray(loraWeights);
            }
        }
        return payload;
    }
}
