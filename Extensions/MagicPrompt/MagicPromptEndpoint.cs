using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Extensions.MagicPrompt;

/// <summary>Implements the endpoints added by the MagicPrompt SwarmUI extension.</summary>
/// <remarks>Requires the MagicPrompt extension to be installed and configured on the target SwarmUI server. None of these endpoints exist in stock SwarmUI.</remarks>
public class MagicPromptEndpoint : IMagicPromptEndpoint
{
    /// <summary>Metadata for the MagicPrompt extension backing this endpoint group.</summary>
    public static readonly SwarmExtensionInfo ExtensionInfo = new()
    {
        Name = "MagicPrompt",
        DisplayName = "MagicPrompt",
        RepositoryUrl = "https://github.com/HartsyAI/SwarmUI-MagicPromptExtension",
        Endpoints = new string[] { "MagicPromptPhoneHome" }
    };

    /// <summary>Internal implementation data containing dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>Session manager for obtaining session IDs (used indirectly via HttpClient).</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for endpoint operations.</summary>
        public ILogger<MagicPromptEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public members.</summary>
    public Impl Internal;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new MagicPromptEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public MagicPromptEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<MagicPromptEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<MagicPromptEndpoint>.Instance;
    }

    /// <inheritdoc />
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
