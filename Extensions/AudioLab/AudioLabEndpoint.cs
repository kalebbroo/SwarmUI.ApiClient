using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions.AudioLab;

/// <summary>Implements the endpoints added by the AudioLab SwarmUI extension.</summary>
/// <remarks>Requires the AudioLab extension to be installed on the target SwarmUI server. None of these endpoints exist in stock SwarmUI.</remarks>
public class AudioLabEndpoint : IAudioLabEndpoint
{
    /// <summary>Metadata for the AudioLab extension backing this endpoint group.</summary>
    public static readonly SwarmExtensionInfo ExtensionInfo = new()
    {
        Name = "AudioLab",
        DisplayName = "AudioLab",
        RepositoryUrl = "https://github.com/HartsyAI/SwarmUI-AudioLab",
        Endpoints = new string[]
        {
            "ProcessTTS",
            "ProcessSTT",
            "ProcessAudio",
            "ProcessWorkflow",
            "GetAllProvidersStatus",
            "GetInstallationStatus",
            "GetInstallationProgress",
            "AudioLabListEngines",
            "AudioLabInstallEngine",
            "AudioLabInstallAllModels",
            "AudioLabUninstallEngine",
            "AudioLabRemoveAllModels",
            "ConvertAudioFormat",
            "AudioLabTimeStretch",
            "AudioLabSaveProject",
            "AudioLabLoadProject",
            "AudioLabListProjects",
            "AudioLabDeleteProject"
        }
    };

    private readonly ISwarmHttpClient _httpClient;
    private readonly ISwarmWebSocketClient _webSocketClient;
    private readonly string _sessionKey;
    private readonly ILogger<AudioLabEndpoint> _logger;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new AudioLabEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="webSocketClient">WebSocket client for streaming install operations.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public AudioLabEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, ILogger<AudioLabEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<AudioLabEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> SynthesizeSpeechAsync(TextToSpeechRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Synthesizing {CharCount} chars of speech with voice '{Voice}' via provider '{ProviderId}'",
            request.Text.Length, request.Voice, request.ProviderId ?? "(server default)");
        TextToSpeechResponse response = await _httpClient.PostJsonAsync<TextToSpeechResponse>("ProcessTTS", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Speech synthesis");
        return response;
    }

    /// <inheritdoc />
    public async Task<SpeechToTextResponse> TranscribeAudioAsync(SpeechToTextRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.AudioData))
        {
            throw new ArgumentException("AudioData cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Transcribing audio in language '{Language}' via provider '{ProviderId}'",
            request.Language, request.ProviderId ?? "(server default)");
        SpeechToTextResponse response = await _httpClient.PostJsonAsync<SpeechToTextResponse>("ProcessSTT", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Transcription");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioProcessResponse> ProcessAudioAsync(AudioProcessRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProviderId))
        {
            throw new ArgumentException("ProviderId cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Processing audio via provider '{ProviderId}' with {ArgCount} argument(s)", request.ProviderId, request.Arguments.Count);
        AudioProcessResponse response = await _httpClient.PostJsonAsync<AudioProcessResponse>("ProcessAudio", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Audio processing");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioWorkflowResponse> ProcessWorkflowAsync(AudioWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Steps.Count == 0)
        {
            throw new ArgumentException("At least one workflow step is required", nameof(request));
        }
        _logger.LogDebug("Running '{WorkflowType}' workflow with {StepCount} step(s)", request.WorkflowType, request.Steps.Count);
        AudioWorkflowResponse response = await _httpClient.PostJsonAsync<AudioWorkflowResponse>("ProcessWorkflow", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Workflow");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioProvidersStatusResponse> GetProvidersStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing audio providers");
        AudioProvidersStatusResponse response = await _httpClient.PostJsonAsync<AudioProvidersStatusResponse>("GetAllProvidersStatus", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {ProviderCount} audio provider(s)", response.TotalCount);
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioInstallationStatusResponse> GetInstallationStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking audio installation status");
        return await _httpClient.PostJsonAsync<AudioInstallationStatusResponse>("GetInstallationStatus", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AudioInstallationProgressResponse> GetInstallationProgressAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading audio installation progress");
        return await _httpClient.PostJsonAsync<AudioInstallationProgressResponse>("GetInstallationProgress", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AudioEnginesResponse> ListEnginesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing audio engines");
        AudioEnginesResponse response = await _httpClient.PostJsonAsync<AudioEnginesResponse>("AudioLabListEngines", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {EngineCount} audio engine(s), backend status: {BackendStatus}", response.Engines.Length, response.BackendStatus);
        return response;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<AudioEngineInstallUpdate> StreamEngineInstallAsync(string providerId, string? modelId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        }
        JObject payload = new()
        {
            ["provider_id"] = providerId
        };
        if (!string.IsNullOrEmpty(modelId))
        {
            payload["model_id"] = modelId;
        }
        _logger.LogDebug("Streaming install of engine '{ProviderId}' (model: {ModelId})", providerId, modelId ?? "(default set)");
        return StreamInstallCoreAsync("AudioLabInstallEngine", payload, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<AudioEngineInstallUpdate> StreamAllModelsInstallAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        }
        JObject payload = new()
        {
            ["provider_id"] = providerId
        };
        _logger.LogDebug("Streaming install of all pending models for engine '{ProviderId}'", providerId);
        return StreamInstallCoreAsync("AudioLabInstallAllModels", payload, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AudioEngineOperationResponse> UninstallEngineAsync(string providerId, bool deleteWeights = false, string? modelId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        }
        JObject payload = new()
        {
            ["provider_id"] = providerId,
            ["delete_weights"] = deleteWeights
        };
        if (!string.IsNullOrEmpty(modelId))
        {
            payload["model_id"] = modelId;
        }
        _logger.LogDebug("Uninstalling engine '{ProviderId}' (deleteWeights: {DeleteWeights}, model: {ModelId})", providerId, deleteWeights, modelId ?? "(all)");
        AudioEngineOperationResponse response = await _httpClient.PostJsonAsync<AudioEngineOperationResponse>("AudioLabUninstallEngine", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Engine uninstall");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioEngineOperationResponse> RemoveAllModelsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        }
        JObject payload = new()
        {
            ["provider_id"] = providerId
        };
        _logger.LogDebug("Removing all model weights for engine '{ProviderId}'", providerId);
        AudioEngineOperationResponse response = await _httpClient.PostJsonAsync<AudioEngineOperationResponse>("AudioLabRemoveAllModels", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        if (response.Success)
        {
            _logger.LogInformation("Removed {Removed}/{Total} model(s) for engine '{ProviderId}'", response.Removed, response.Total, providerId);
        }
        else
        {
            _logger.LogWarning("Failed to remove models for engine '{ProviderId}': {Error}", providerId, response.Error ?? "Unknown error");
        }
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioFormatConversionResponse> ConvertAudioFormatAsync(AudioFormatConversionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.AudioData))
        {
            throw new ArgumentException("AudioData cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Converting audio to '{Format}'", request.Format);
        AudioFormatConversionResponse response = await _httpClient.PostJsonAsync<AudioFormatConversionResponse>("ConvertAudioFormat", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Audio conversion");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioTimeStretchResponse> TimeStretchAsync(AudioTimeStretchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.AudioData))
        {
            throw new ArgumentException("AudioData cannot be null or empty", nameof(request));
        }
        if (request.Rate < 0.25 || request.Rate > 4.0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Rate, "Rate must be within 0.25-4.0");
        }
        _logger.LogDebug("Time-stretching audio at rate {Rate} with {Semitones} semitone shift",
            request.Rate.ToString("0.###", CultureInfo.InvariantCulture),
            request.Semitones.ToString("0.###", CultureInfo.InvariantCulture));
        AudioTimeStretchResponse response = await _httpClient.PostJsonAsync<AudioTimeStretchResponse>("AudioLabTimeStretch", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Time stretch");
        return response;
    }

    /// <inheritdoc />
    public async Task<DawProjectSaveResponse> SaveProjectAsync(string name, string projectJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be null or empty", nameof(name));
        }
        if (string.IsNullOrEmpty(projectJson))
        {
            throw new ArgumentException("Project JSON cannot be null or empty", nameof(projectJson));
        }
        JObject payload = new()
        {
            ["name"] = name,
            ["project_json"] = projectJson
        };
        _logger.LogDebug("Saving DAW project '{Name}' ({Size} chars)", name, projectJson.Length);
        DawProjectSaveResponse response = await _httpClient.PostJsonAsync<DawProjectSaveResponse>("AudioLabSaveProject", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "DAW project save");
        return response;
    }

    /// <inheritdoc />
    public async Task<DawProjectResponse> LoadProjectAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be null or empty", nameof(name));
        }
        JObject payload = new()
        {
            ["name"] = name
        };
        _logger.LogDebug("Loading DAW project '{Name}'", name);
        DawProjectResponse response = await _httpClient.PostJsonAsync<DawProjectResponse>("AudioLabLoadProject", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "DAW project load");
        return response;
    }

    /// <inheritdoc />
    public async Task<DawProjectListResponse> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing DAW projects");
        DawProjectListResponse response = await _httpClient.PostJsonAsync<DawProjectListResponse>("AudioLabListProjects", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {ProjectCount} DAW project(s)", response.Projects.Length);
        return response;
    }

    /// <inheritdoc />
    public async Task<DawProjectDeleteResponse> DeleteProjectAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be null or empty", nameof(name));
        }
        JObject payload = new()
        {
            ["name"] = name
        };
        _logger.LogDebug("Deleting DAW project '{Name}'", name);
        DawProjectDeleteResponse response = await _httpClient.PostJsonAsync<DawProjectDeleteResponse>("AudioLabDeleteProject", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "DAW project delete");
        return response;
    }

    /// <summary>Shared streaming core for engine install endpoints.</summary>
    private async IAsyncEnumerable<AudioEngineInstallUpdate> StreamInstallCoreAsync(string endpoint, JObject payload, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (JObject frame in _webSocketClient.StreamFramesAsync(endpoint, payload, _sessionKey, cancellationToken).ConfigureAwait(false))
        {
            AudioEngineInstallUpdate? update = frame.ToObject<AudioEngineInstallUpdate>();
            if (update is not null)
            {
                yield return update;
            }
        }
    }

    /// <summary>Logs whether an AudioLab operation succeeded, using the shared response envelope.</summary>
    private void LogOutcome(AudioLabResponse response, string operation)
    {
        if (response.Success)
        {
            _logger.LogInformation("{Operation} succeeded", operation);
        }
        else
        {
            _logger.LogWarning("{Operation} failed: {Error} ({ErrorCode})", operation, response.Error ?? "Unknown error", response.ErrorCode ?? "no code");
        }
    }
}
