using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.APIBackends.Contracts;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Extensions.APIBackends;

/// <summary>Implements the endpoints added by the API-Backends SwarmUI extension.</summary>
/// <remarks>Requires the SwarmUI-API-Backends extension on the target server. None of these endpoints exist in stock SwarmUI.</remarks>
public class APIBackendsEndpoint : IAPIBackendsEndpoint
{
    /// <summary>Metadata for the API-Backends extension backing this endpoint group.</summary>
    public static readonly SwarmExtensionInfo ExtensionInfo = new()
    {
        Name = "APIBackends",
        DisplayName = "API Backends",
        RepositoryUrl = "https://github.com/HartsyAI/SwarmUI-API-Backends",
        Endpoints = new string[] { "APIBackendsListModelCapabilities" }
    };

    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<APIBackendsEndpoint> _logger;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new APIBackendsEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public APIBackendsEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<APIBackendsEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<APIBackendsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<ModelCapabilitiesResponse> ListModelCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing API backend model capabilities");
        ModelCapabilitiesResponse response = await _httpClient.PostJsonAsync<ModelCapabilitiesResponse>("APIBackendsListModelCapabilities", new JObject(), _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved capabilities for {Count} API backed models", response.Models?.Count ?? 0);
        return response;
    }
}
