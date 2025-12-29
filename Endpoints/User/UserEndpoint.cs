using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Endpoints.User;

/// <summary>Implements user data and settings endpoints for the current SwarmUI user.</summary>
/// <remarks>Provides access to user presets, settings, API keys, and permissions via SwarmUI's HTTP API with automatic session management. See the SwarmUI user endpoints documentation for full schema and behavior.</remarks>
public class UserEndpoint : IUserEndpoint
{
    /// <summary>Internal implementation data containing dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>Session manager for obtaining session IDs (used indirectly via HttpClient).</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for endpoint operations.</summary>
        public ILogger<UserEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public members.</summary>
    public Impl Internal;

    /// <summary>Creates a new UserEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public UserEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<UserEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<UserEndpoint>.Instance;
    }

    /// <summary>Gets comprehensive user data including presets, permissions, and preferences.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User data including presets, permissions, settings, and session information.</returns>
    /// <remarks>Intended as the initial "load user profile" call for a session. Returns presets, permissions, settings, and session information used to configure the UI. See the SwarmUI user data documentation for full response details.</remarks>
    public async Task<UserDataResponse> GetMyUserDataAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Fetching user data including presets");
        UserDataResponse response = await Internal.HttpClient.PostJsonAsync<UserDataResponse>("GetMyUserData", payload: null, cancellationToken).ConfigureAwait(false);

        // Get preset count - Presets can be array, dictionary, or null depending on server version
        int presetCount = 0;
        if (response.Presets is System.Collections.ICollection collection)
        {
            presetCount = collection.Count;
        }

        Internal.Logger.LogInformation("Retrieved user data with {PresetCount} presets", presetCount);
        return response;
    }

    /// <summary>Gets the current user's settings, such as theme, UI preferences, and default parameters.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User settings as a dictionary of key-value pairs.</returns>
    /// <remarks>Returns persistent preferences stored on the server. The exact structure may vary by SwarmUI version; use the Settings dictionary or the <c>GetSetting&lt;T&gt;</c> helper on <see cref="UserSettingsResponse"/> for safe typed access.</remarks>
    public async Task<UserSettingsResponse> GetUserSettingsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Fetching user settings");
        UserSettingsResponse response = await Internal.HttpClient.PostJsonAsync<UserSettingsResponse>("GetUserSettings", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved user settings successfully");
        return response;
    }

    /// <summary>Updates user settings with new values; only provided keys are changed.</summary>
    /// <param name="settings">
    /// Dictionary of setting keys and their new values.
    /// Only include settings you want to change, not all settings.
    /// Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown if settings dictionary is empty.</exception>
    /// <remarks>Performs a partial update: only the provided settings are modified, and others remain unchanged. Setting values are validated by SwarmUI; invalid names or values may be rejected. See the SwarmUI documentation for valid setting keys.</remarks>
    public async Task ChangeUserSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        if (settings.Count is 0)
        {
            throw new ArgumentException("Settings dictionary cannot be empty", nameof(settings));
        }
        Internal.Logger.LogDebug("Updating user settings with {Count} values", settings.Count);
        JObject payload = new()
        {
            ["rawData"] = JObject.FromObject(settings)
        };
        await Internal.HttpClient.PostJsonAsync<JObject>("ChangeUserSettings", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("User settings updated successfully");
    }

    /// <summary>Gets the status of a specific external service API key.</summary>
    /// <param name="keyType">
    /// Type of API key to check (e.g., "stability_api", "openai_api").
    /// Must match a key type recognized by SwarmUI. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// JSON object containing key status information.
    /// Typical fields: "status" ("valid", "invalid", "missing"), "message" (details).
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    /// <remarks>Checks whether an external API key is configured and, for some key types, whether it is valid. Typical status values include "valid", "invalid", and "missing". See the SwarmUI documentation for supported key types and status fields.</remarks>
    public async Task<JObject> GetAPIKeyStatusAsync(string keyType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyType))
        {
            throw new ArgumentException("Key type cannot be null or empty", nameof(keyType));
        }
        Internal.Logger.LogDebug("Checking status of API key type: {KeyType}", keyType);
        JObject payload = new()
        {
            ["keyType"] = keyType
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("GetAPIKeyStatus", payload, cancellationToken).ConfigureAwait(false);
        string status = response["status"]?.ToString() ?? "unknown";
        Internal.Logger.LogInformation("Retrieved API key status for {KeyType}: {Status}", keyType, status);
        return response;
    }

    /// <summary>Sets or updates an external service API key for the current user.</summary>
    /// <param name="keyType">
    /// Type of API key to set (e.g., "stability_api", "openai_api").
    /// Must match a key type recognized by SwarmUI. Must not be null or empty.
    /// </param>
    /// <param name="key">
    /// The API key value to set. Pass "none" or empty string to unset/remove the key.
    /// Must not be null (use "none" to remove).
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    /// <remarks>Configures external service credentials for backend use. Keys are stored securely on the SwarmUI server and are not returned after being set; use <see cref="GetAPIKeyStatusAsync"/> to check configuration without exposing the key value. Passing "none" or an empty string removes a key. Some key types may validate the key when set.</remarks>
    public async Task SetAPIKeyAsync(string keyType, string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyType))
        {
            throw new ArgumentException("Key type cannot be null or empty", nameof(keyType));
        }
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Internal.Logger.LogDebug("Setting API key for type: {KeyType}", keyType);
        JObject payload = new()
        {
            ["keyType"] = keyType,
            ["key"] = string.IsNullOrEmpty(key) ? "none" : key
        };
        await Internal.HttpClient.PostJsonAsync<JObject>("SetAPIKey", payload, cancellationToken).ConfigureAwait(false);
        bool removing = string.IsNullOrEmpty(key) || key.Equals("none", StringComparison.OrdinalIgnoreCase);
        Internal.Logger.LogInformation("{Action} API key for type: {KeyType}", removing ? "Removed" : "Set", keyType);
    }
}
