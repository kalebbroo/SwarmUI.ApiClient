using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Endpoints.LLM;

/// <summary>Implements LLM endpoints for text processing via SwarmUI.</summary>
/// <remarks>
/// NOTE: LLM endpoints are Hartsy-specific extensions and are not part of the standard SwarmUI distribution.
/// These endpoints require custom extensions to be installed and configured on the SwarmUI server.
///
/// Provides access to LLM-based text enhancement features like MagicPrompt.
/// </remarks>
public class LLMEndpoint : ILLMEndpoint
{
    /// <summary>Internal implementation data containing dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>Session manager for obtaining session IDs (used indirectly via HttpClient).</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for endpoint operations.</summary>
        public ILogger<LLMEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public members.</summary>
    public Impl Internal;

    /// <summary>Creates a new LLMEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public LLMEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<LLMEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<LLMEndpoint>.Instance;
    }

    /// <summary>Enhances a text prompt using the MagicPrompt extension.</summary>
    /// <param name="request">MagicPrompt request with text to enhance and model configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Enhanced text response from the LLM.</returns>
    /// <remarks>
    /// NOTE: This is a Hartsy-specific extension endpoint. Calls the LLMAPICalls.MagicPromptPhoneHome
    /// endpoint in SwarmUI, which requires the Hartsy MagicPrompt extension to be installed and configured.
    /// This endpoint does not exist in standard SwarmUI.
    /// </remarks>
    public async Task<MagicPromptResponse> EnhancePromptAsync(MagicPromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Content?.Text))
        {
            throw new ArgumentException("Text content cannot be empty", nameof(request));
        }
        string modelIdLog = string.IsNullOrWhiteSpace(request.ModelId) ? "(using server default)" : request.ModelId;
        Internal.Logger.LogDebug("Enhancing prompt with MagicPrompt using model: {ModelId}", modelIdLog);
        MagicPromptResponse response = await Internal.HttpClient.PostJsonAsync<MagicPromptRequest, MagicPromptResponse>("MagicPromptPhoneHome", request, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
        {
            Internal.Logger.LogWarning("MagicPrompt enhancement failed: {Error}", response.Error ?? "Unknown error");
        }
        else
        {
            Internal.Logger.LogInformation("Successfully enhanced prompt (original: {OriginalLength} chars → enhanced: {EnhancedLength} chars)",
                request.Content.Text.Length,
                response.Response?.Length ?? 0);
        }
        return response;
    }
}
