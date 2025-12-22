using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's GetMyUserData endpoint containing user-specific data such as presets, permissions, settings, and session information.</summary>
/// <remarks>Used to load user state at session start so the UI can configure presets, permissions, and preferences. The schema can evolve between SwarmUI versions; unknown fields are captured in <see cref="RawData"/>.</remarks>
public class UserDataResponse
{
    /// <summary>Session ID associated with this user data.</summary>
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }

    /// <summary>User's saved parameter presets for this account; underlying JSON shape may vary by SwarmUI version.</summary>
    [JsonProperty("presets")]
    public object? Presets { get; set; }

    /// <summary>User permissions controlling what actions are allowed for this user (for example, managing presets or models).</summary>
    [JsonProperty("permissions")]
    public object? Permissions { get; set; }

    /// <summary>User settings and preferences mirrored from <c>GetUserSettings</c> for convenience during session initialization.</summary>
    [JsonProperty("user_settings")]
    public Dictionary<string, object>? UserSettings { get; set; }

    /// <summary>Identifier for this user, or null for single-user installations where user accounts are disabled.</summary>
    [JsonProperty("user_id")]
    public string? UserId { get; set; }

    /// <summary>Display name for this user, shown in the UI.</summary>
    [JsonProperty("username")]
    public string? Username { get; set; }

    /// <summary>Additional fields from the response that are not mapped to dedicated properties, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? RawData { get; set; }
}
