using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

namespace SwarmUI.ApiClient.Extensions.AudioLab;

/// <summary>Provides access to the endpoints added by the AudioLab SwarmUI extension.</summary>
/// <remarks>Requires the AudioLab extension to be installed on the target SwarmUI server, and most operations additionally require an enabled Audio Backend. None of these endpoints exist in stock SwarmUI.
///
/// AudioLab also registers audio T2I parameters and an audio backend type, so generating audio from a prompt runs through the stock generation pipeline via <see cref="ISwarmClient.Generation"/> with an audio model selected. The endpoints here cover the operations that pipeline does not: direct synthesis and transcription, engine and model management, format conversion, and DAW project storage.</remarks>
public interface IAudioLabEndpoint : ISwarmExtensionEndpoint
{
    /// <summary>Synthesizes speech from text.</summary>
    /// <param name="request">Synthesis request. Text is required and capped at 1000 characters by the server.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Base64 encoded audio and synthesis metadata.</returns>
    /// <remarks>Calls the <c>ProcessTTS</c> endpoint. Requires the <c>audio_process</c> permission.</remarks>
    Task<TextToSpeechResponse> SynthesizeSpeechAsync(TextToSpeechRequest request, CancellationToken cancellationToken = default);

    /// <summary>Transcribes speech from audio.</summary>
    /// <param name="request">Transcription request. Base64 audio data is required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Transcribed text and confidence metadata.</returns>
    /// <remarks>Calls the <c>ProcessSTT</c> endpoint. Requires the <c>audio_process</c> permission.</remarks>
    Task<SpeechToTextResponse> TranscribeAudioAsync(SpeechToTextRequest request, CancellationToken cancellationToken = default);

    /// <summary>Routes audio processing to a specific provider, forwarding arguments verbatim.</summary>
    /// <param name="request">Provider identifier and provider specific arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The provider's result. Fields beyond the standard envelope are available in <see cref="AudioProcessResponse.AdditionalData"/>.</returns>
    /// <remarks>Calls the <c>ProcessAudio</c> endpoint. Use this for providers whose arguments have no dedicated typed request.</remarks>
    Task<AudioProcessResponse> ProcessAudioAsync(AudioProcessRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs a chained audio workflow, piping each step's output into the next.</summary>
    /// <param name="request">Workflow definition. At least one step is required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Per-step results and the executed step order.</returns>
    /// <remarks>Calls the <c>ProcessWorkflow</c> endpoint. Audio input requires the first step to be "stt".</remarks>
    Task<AudioWorkflowResponse> ProcessWorkflowAsync(AudioWorkflowRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists every registered audio provider with its category and model count.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Registered providers.</returns>
    /// <remarks>Calls the <c>GetAllProvidersStatus</c> endpoint. Requires the <c>audio_check_status</c> permission.</remarks>
    Task<AudioProvidersStatusResponse> GetProvidersStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Reports which providers are installed and whether the audio engine is ready.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Engine availability and per-provider install state.</returns>
    /// <remarks>Calls the <c>GetInstallationStatus</c> endpoint.</remarks>
    Task<AudioInstallationStatusResponse> GetInstallationStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the current installation progress.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Progress percentage, current step, and completion state.</returns>
    /// <remarks>Calls the <c>GetInstallationProgress</c> endpoint. Poll this alongside a non-streaming install, or prefer <see cref="StreamEngineInstallAsync"/> which reports progress directly.</remarks>
    Task<AudioInstallationProgressResponse> GetInstallationProgressAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists every audio engine with install state, capability flags, and model variants.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Engines and the audio backend status.</returns>
    /// <remarks>Calls the <c>AudioLabListEngines</c> endpoint. Engines are reported even while the backend is still starting.</remarks>
    Task<AudioEnginesResponse> ListEnginesAsync(CancellationToken cancellationToken = default);

    /// <summary>Installs an audio engine, streaming progress as it downloads.</summary>
    /// <param name="providerId">Provider to install. Required.</param>
    /// <param name="modelId">Optional single model variant to install. When null the provider's default model set is fetched.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Streamed install updates, ending with a frame whose <see cref="AudioEngineInstallUpdate.IsTerminal"/> is set.</returns>
    /// <remarks>Streams the <c>AudioLabInstallEngine</c> WebSocket endpoint. Requires the <c>audio_manage_backends</c> permission and a running Audio Backend.</remarks>
    IAsyncEnumerable<AudioEngineInstallUpdate> StreamEngineInstallAsync(string providerId, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>Installs every not-yet-present model for a provider, streaming progress per model.</summary>
    /// <param name="providerId">Provider whose pending models should be installed. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Streamed install updates, ending with a frame carrying the installed and attempted counts.</returns>
    /// <remarks>Streams the <c>AudioLabInstallAllModels</c> WebSocket endpoint. Models whose weights are already on disk are skipped.</remarks>
    IAsyncEnumerable<AudioEngineInstallUpdate> StreamAllModelsInstallAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>Uninstalls an audio engine, or deletes one model variant's weights.</summary>
    /// <param name="providerId">Provider to uninstall. Required.</param>
    /// <param name="deleteWeights">Whether to also delete the provider's downloaded weights. Shared side-model caches are retained.</param>
    /// <param name="modelId">When set, deletes only this variant's weights and leaves the engine installed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Outcome of the uninstall.</returns>
    /// <remarks>Calls the <c>AudioLabUninstallEngine</c> endpoint. Requires the <c>audio_manage_backends</c> permission.</remarks>
    Task<AudioEngineOperationResponse> UninstallEngineAsync(string providerId, bool deleteWeights = false, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes the weights of every installed model for a provider, leaving the engine installed.</summary>
    /// <param name="providerId">Provider whose model weights should be removed. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Counts and identifiers of the removed models.</returns>
    /// <remarks>Calls the <c>AudioLabRemoveAllModels</c> endpoint. Requires the <c>audio_manage_backends</c> permission.</remarks>
    Task<AudioEngineOperationResponse> RemoveAllModelsAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>Converts audio to a different container format.</summary>
    /// <param name="request">Source audio and target format.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Converted audio with its MIME type and size.</returns>
    /// <remarks>Calls the <c>ConvertAudioFormat</c> endpoint. Requires ffmpeg on the SwarmUI host.</remarks>
    Task<AudioFormatConversionResponse> ConvertAudioFormatAsync(AudioFormatConversionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Time-stretches audio, optionally shifting pitch.</summary>
    /// <param name="request">Source audio, tempo multiplier, and pitch shift.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Stretched audio and the applied settings.</returns>
    /// <remarks>Calls the <c>AudioLabTimeStretch</c> endpoint. Requires ffmpeg on the SwarmUI host.</remarks>
    Task<AudioTimeStretchResponse> TimeStretchAsync(AudioTimeStretchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Saves a DAW project under the calling user's account.</summary>
    /// <param name="name">Project name. Required.</param>
    /// <param name="projectJson">Serialized arrangement, including embedded base64 clip audio. The server rejects payloads over 64 MB.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The saved name and stored size.</returns>
    /// <remarks>Calls the <c>AudioLabSaveProject</c> endpoint. Requires the <c>audio_daw_projects</c> permission.</remarks>
    Task<DawProjectSaveResponse> SaveProjectAsync(string name, string projectJson, CancellationToken cancellationToken = default);

    /// <summary>Loads a saved DAW project by name.</summary>
    /// <param name="name">Project name. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored project JSON.</returns>
    /// <remarks>Calls the <c>AudioLabLoadProject</c> endpoint.</remarks>
    Task<DawProjectResponse> LoadProjectAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Lists the calling user's saved DAW project names.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Saved project names.</returns>
    /// <remarks>Calls the <c>AudioLabListProjects</c> endpoint.</remarks>
    Task<DawProjectListResponse> ListProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes a saved DAW project.</summary>
    /// <param name="name">Project name. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deleted project name.</returns>
    /// <remarks>Calls the <c>AudioLabDeleteProject</c> endpoint.</remarks>
    Task<DawProjectDeleteResponse> DeleteProjectAsync(string name, CancellationToken cancellationToken = default);
}
