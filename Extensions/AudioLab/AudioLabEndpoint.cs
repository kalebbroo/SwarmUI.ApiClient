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
using SwarmUI.ApiClient.Sessions;
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

    /// <summary>Internal implementation data containing dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>WebSocket client for streaming engine install operations.</summary>
        public ISwarmWebSocketClient WebSocketClient;

        /// <summary>Session manager for obtaining session IDs (used indirectly via HttpClient).</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for endpoint operations.</summary>
        public ILogger<AudioLabEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public members.</summary>
    public Impl Internal;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new AudioLabEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="webSocketClient">WebSocket client for streaming install operations. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public AudioLabEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, ISessionManager sessionManager, ILogger<AudioLabEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.WebSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<AudioLabEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> SynthesizeSpeechAsync(TextToSpeechRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(request));
        }
        Internal.Logger.LogDebug("Synthesizing {CharCount} chars of speech with voice '{Voice}' via provider '{ProviderId}'",
            request.Text.Length, request.Voice, request.ProviderId ?? "(server default)");
        TextToSpeechResponse response = await Internal.HttpClient.PostJsonAsync<TextToSpeechRequest, TextToSpeechResponse>("ProcessTTS", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Transcribing audio in language '{Language}' via provider '{ProviderId}'",
            request.Language, request.ProviderId ?? "(server default)");
        SpeechToTextResponse response = await Internal.HttpClient.PostJsonAsync<SpeechToTextRequest, SpeechToTextResponse>("ProcessSTT", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Processing audio via provider '{ProviderId}' with {ArgCount} argument(s)", request.ProviderId, request.Arguments.Count);
        AudioProcessResponse response = await Internal.HttpClient.PostJsonAsync<AudioProcessRequest, AudioProcessResponse>("ProcessAudio", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Running '{WorkflowType}' workflow with {StepCount} step(s)", request.WorkflowType, request.Steps.Count);
        AudioWorkflowResponse response = await Internal.HttpClient.PostJsonAsync<AudioWorkflowRequest, AudioWorkflowResponse>("ProcessWorkflow", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Workflow");
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioProvidersStatusResponse> GetProvidersStatusAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing audio providers");
        AudioProvidersStatusResponse response = await Internal.HttpClient.PostJsonAsync<AudioProvidersStatusResponse>("GetAllProvidersStatus", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {ProviderCount} audio provider(s)", response.TotalCount);
        return response;
    }

    /// <inheritdoc />
    public async Task<AudioInstallationStatusResponse> GetInstallationStatusAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Checking audio installation status");
        return await Internal.HttpClient.PostJsonAsync<AudioInstallationStatusResponse>("GetInstallationStatus", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AudioInstallationProgressResponse> GetInstallationProgressAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading audio installation progress");
        return await Internal.HttpClient.PostJsonAsync<AudioInstallationProgressResponse>("GetInstallationProgress", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AudioEnginesResponse> ListEnginesAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing audio engines");
        AudioEnginesResponse response = await Internal.HttpClient.PostJsonAsync<AudioEnginesResponse>("AudioLabListEngines", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {EngineCount} audio engine(s), backend status: {BackendStatus}", response.Engines.Length, response.BackendStatus);
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
        Internal.Logger.LogDebug("Streaming install of engine '{ProviderId}' (model: {ModelId})", providerId, modelId ?? "(default set)");
        return Internal.WebSocketClient.StreamMessagesAsync("AudioLabInstallEngine", payload, ParseInstallUpdate, cancellationToken);
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
        Internal.Logger.LogDebug("Streaming install of all pending models for engine '{ProviderId}'", providerId);
        return Internal.WebSocketClient.StreamMessagesAsync("AudioLabInstallAllModels", payload, ParseInstallUpdate, cancellationToken);
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
        Internal.Logger.LogDebug("Uninstalling engine '{ProviderId}' (deleteWeights: {DeleteWeights}, model: {ModelId})", providerId, deleteWeights, modelId ?? "(all)");
        AudioEngineOperationResponse response = await Internal.HttpClient.PostJsonAsync<AudioEngineOperationResponse>("AudioLabUninstallEngine", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Removing all model weights for engine '{ProviderId}'", providerId);
        AudioEngineOperationResponse response = await Internal.HttpClient.PostJsonAsync<AudioEngineOperationResponse>("AudioLabRemoveAllModels", payload, cancellationToken).ConfigureAwait(false);
        if (response.Success)
        {
            Internal.Logger.LogInformation("Removed {Removed}/{Total} model(s) for engine '{ProviderId}'", response.Removed, response.Total, providerId);
        }
        else
        {
            Internal.Logger.LogWarning("Failed to remove models for engine '{ProviderId}': {Error}", providerId, response.Error ?? "Unknown error");
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
        Internal.Logger.LogDebug("Converting audio to '{Format}'", request.Format);
        AudioFormatConversionResponse response = await Internal.HttpClient.PostJsonAsync<AudioFormatConversionRequest, AudioFormatConversionResponse>("ConvertAudioFormat", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Time-stretching audio at rate {Rate} with {Semitones} semitone shift",
            request.Rate.ToString("0.###", CultureInfo.InvariantCulture),
            request.Semitones.ToString("0.###", CultureInfo.InvariantCulture));
        AudioTimeStretchResponse response = await Internal.HttpClient.PostJsonAsync<AudioTimeStretchRequest, AudioTimeStretchResponse>("AudioLabTimeStretch", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Saving DAW project '{Name}' ({Size} chars)", name, projectJson.Length);
        DawProjectSaveResponse response = await Internal.HttpClient.PostJsonAsync<DawProjectSaveResponse>("AudioLabSaveProject", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Loading DAW project '{Name}'", name);
        DawProjectResponse response = await Internal.HttpClient.PostJsonAsync<DawProjectResponse>("AudioLabLoadProject", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "DAW project load");
        return response;
    }

    /// <inheritdoc />
    public async Task<DawProjectListResponse> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing DAW projects");
        DawProjectListResponse response = await Internal.HttpClient.PostJsonAsync<DawProjectListResponse>("AudioLabListProjects", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {ProjectCount} DAW project(s)", response.Projects.Length);
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
        Internal.Logger.LogDebug("Deleting DAW project '{Name}'", name);
        DawProjectDeleteResponse response = await Internal.HttpClient.PostJsonAsync<DawProjectDeleteResponse>("AudioLabDeleteProject", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "DAW project delete");
        return response;
    }

    /// <summary>Parses a streamed engine install frame into a typed update.</summary>
    private static AudioEngineInstallUpdate ParseInstallUpdate(JObject message)
    {
        return message.ToObject<AudioEngineInstallUpdate>() ?? new AudioEngineInstallUpdate();
    }

    /// <summary>Logs whether an AudioLab operation succeeded, using the shared response envelope.</summary>
    private void LogOutcome(AudioLabResponse response, string operation)
    {
        if (response.Success)
        {
            Internal.Logger.LogInformation("{Operation} succeeded", operation);
        }
        else
        {
            Internal.Logger.LogWarning("{Operation} failed: {Error} ({ErrorCode})", operation, response.Error ?? "Unknown error", response.ErrorCode ?? "no code");
        }
    }
}
