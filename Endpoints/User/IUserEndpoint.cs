using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.User;

/// <summary>Provides access to SwarmUI user data and settings endpoints for the current user.</summary>
/// <remarks>
/// User endpoints are focused on personalization and configuration for the current session's user.
/// They control how the UI behaves, what defaults are used, and what external services are configured.
///
/// **Common Use Cases**:
///
/// 1. **Session Initialization**: Call GetMyUserDataAsync when starting a session to load
///    user presets, permissions, and settings for UI configuration.
///
/// 2. **Settings Management**: Use GetUserSettingsAsync/ChangeUserSettingsAsync to read
///    and modify user preferences like theme, defaults, and UI behavior.
///
/// 3. **API Key Management**: Configure external service API keys (Stability AI, OpenAI, etc.)
/// </remarks>
public interface IUserEndpoint
{
    /// <summary>Gets comprehensive user data including presets, permissions, and preferences.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User data including presets, permissions, and session information.</returns>
    Task<UserDataResponse> GetMyUserDataAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current user's settings, including theme, UI preferences, and default parameters.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of user settings with keys and values.</returns>
    Task<UserSettingsResponse> GetUserSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates user settings with new values; only the provided settings are changed.</summary>
    /// <param name="settings">Dictionary of setting keys and new values to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when settings are updated.</returns>
    Task ChangeUserSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default);

    /// <summary>Gets the status of a specific external service API key.</summary>
    /// <param name="keyType">
    /// Type of API key to check (e.g., "stability_api", "openai_api").
    /// Must match a key type recognized by SwarmUI.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>JSON object containing key status ("valid", "invalid", "missing", etc.).</returns>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    Task<JObject> GetAPIKeyStatusAsync(string keyType, CancellationToken cancellationToken = default);

    /// <summary>Sets or updates an external service API key for the current user.</summary>
    /// <param name="keyType">
    /// Type of API key to set (e.g., "stability_api", "openai_api").
    /// Must match a key type recognized by SwarmUI.
    /// </param>
    /// <param name="key">
    /// The API key value to set. Pass "none" or empty string to unset/remove the key.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when the key is set.</returns>
    /// <exception cref="ArgumentException">Thrown if keyType is null or empty.</exception>
    /// <remarks>
    /// API keys are stored securely on the SwarmUI server and used for backend operations.
    /// The key is never sent back to the client after being set.
    ///
    /// To remove a key, pass "none" as the key value:
    /// ```csharp
    /// await user.SetAPIKeyAsync("stability_api", "none");
    /// ```
    /// </remarks>
    Task SetAPIKeyAsync(string keyType, string key, CancellationToken cancellationToken = default);
}
