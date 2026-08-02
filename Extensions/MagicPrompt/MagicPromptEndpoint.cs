using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;
using SwarmUI.ApiClient.Http;

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

    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<MagicPromptEndpoint> _logger;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new MagicPromptEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public MagicPromptEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<MagicPromptEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<MagicPromptEndpoint>.Instance;
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
        _logger.LogDebug("Enhancing prompt with MagicPrompt using model: {ModelId}", modelIdLog);
        MagicPromptResponse response = await _httpClient.PostJsonAsync<MagicPromptResponse>("MagicPromptPhoneHome", JObject.FromObject(request), _sessionKey, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
        {
            _logger.LogWarning("MagicPrompt enhancement failed: {Error}", response.Error ?? "Unknown error");
        }
        else
        {
            _logger.LogInformation("Successfully enhanced prompt (original: {OriginalLength} chars → enhanced: {EnhancedLength} chars)",
                request.Content.Text.Length,
                response.Response?.Length ?? 0);
        }
        return response;
    }
}
