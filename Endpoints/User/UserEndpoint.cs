using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Endpoints.User;

/// <summary>Implements user data and settings endpoints for the current SwarmUI user.</summary>
/// <remarks>Provides access to user presets, settings, API keys, and permissions via SwarmUI's HTTP API. API key values are redacted from debug logs by the HTTP layer.</remarks>
public class UserEndpoint : IUserEndpoint
{
    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<UserEndpoint> _logger;

    /// <summary>Creates a new UserEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public UserEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<UserEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<UserEndpoint>.Instance;
    }

    /// <summary>Gets comprehensive user data including presets, permissions, and preferences.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User data including presets, permissions, settings, and session information.</returns>
    public async Task<UserDataResponse> GetMyUserDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching user data including presets");
        UserDataResponse response = await _httpClient.PostJsonAsync<UserDataResponse>("GetMyUserData", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved user data");
        return response;
    }

    /// <summary>Gets the current user's settings, such as theme, UI preferences, and default parameters.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User settings as a dictionary of key-value pairs.</returns>
    public async Task<UserSettingsResponse> GetUserSettingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching user settings");
        UserSettingsResponse response = await _httpClient.PostJsonAsync<UserSettingsResponse>("GetUserSettings", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved user settings successfully");
        return response;
    }

    /// <summary>Updates user settings with new values; only provided keys are changed.</summary>
    /// <param name="settings">Dictionary of setting keys and their new values. Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown if settings dictionary is empty.</exception>
    public async Task ChangeUserSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Count is 0)
        {
            throw new ArgumentException("Settings dictionary cannot be empty", nameof(settings));
        }
        _logger.LogDebug("Updating user settings with {Count} values", settings.Count);
        JObject payload = new()
        {
            ["rawData"] = JObject.FromObject(settings)
        };
        await _httpClient.PostJsonAsync<JObject>("ChangeUserSettings", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("User settings updated successfully");
    }

    /// <summary>Gets the status of a specific external service API key.</summary>
    /// <param name="keyType">Type of API key to check (e.g., "stability_api", "openai_api"). Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>JSON object containing key status information (typically "status" and "message").</returns>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    public async Task<JObject> GetAPIKeyStatusAsync(string keyType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyType))
        {
            throw new ArgumentException("Key type cannot be null or empty", nameof(keyType));
        }
        _logger.LogDebug("Checking status of API key type: {KeyType}", keyType);
        JObject payload = new()
        {
            ["keyType"] = keyType
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("GetAPIKeyStatus", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved API key status for {KeyType}: {Status}", keyType, response["status"]?.ToString() ?? "unknown");
        return response;
    }

    /// <summary>Sets or updates an external service API key for the current user.</summary>
    /// <param name="keyType">Type of API key to set (e.g., "stability_api", "openai_api"). Must not be null or empty.</param>
    /// <param name="key">The API key value to set. Pass "none" or empty string to unset/remove the key. Must not be null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public async Task SetAPIKeyAsync(string keyType, string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyType))
        {
            throw new ArgumentException("Key type cannot be null or empty", nameof(keyType));
        }
        ArgumentNullException.ThrowIfNull(key);
        _logger.LogDebug("Setting API key for type: {KeyType}", keyType);
        JObject payload = new()
        {
            ["keyType"] = keyType,
            ["key"] = string.IsNullOrEmpty(key) ? "none" : key
        };
        await _httpClient.PostJsonAsync<JObject>("SetAPIKey", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        bool removing = string.IsNullOrEmpty(key) || key.Equals("none", StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation("{Action} API key for type: {KeyType}", removing ? "Removed" : "Set", keyType);
    }
}
