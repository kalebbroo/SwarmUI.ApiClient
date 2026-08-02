using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Endpoints.Admin;

/// <summary>Implements SwarmUI administrative endpoints using HTTP-based AdminAPI routes.</summary>
/// <remarks>Password and API key payload values are redacted from debug logs by the HTTP layer.</remarks>
public class AdminEndpoint : IAdminEndpoint
{
    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<AdminEndpoint> _logger;

    /// <summary>Creates a new AdminEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public AdminEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<AdminEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<AdminEndpoint>.Instance;
    }

    public async Task AddUserAsync(string name, string password, string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be null or empty", nameof(role));
        }
        _logger.LogDebug("Admin adding user '{UserName}' with role '{Role}'", name, role);
        JObject payload = new()
        {
            ["name"] = name,
            ["password"] = password,
            ["role"] = role
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminAddUser", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin created user '{UserName}' with role '{Role}'", name, role);
    }

    public async Task DeleteUserAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        _logger.LogDebug("Admin deleting user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminDeleteUser", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin deleted user '{UserName}'", name);
    }

    public async Task<JObject> GetUserInfoAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        _logger.LogDebug("Admin fetching info for user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("AdminGetUserInfo", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task SetUserPasswordAsync(string name, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }
        _logger.LogDebug("Admin setting password for user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name,
            ["password"] = password
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminSetUserPassword", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangeUserSettingsAsync(string name, Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        ArgumentNullException.ThrowIfNull(settings);
        _logger.LogDebug("Admin changing settings for user '{UserName}' with {SettingCount} entries", name, settings.Count);
        JObject settingsObject = JObject.FromObject(settings);
        JObject rawData = new()
        {
            ["settings"] = settingsObject
        };
        JObject payload = new()
        {
            ["name"] = name,
            ["rawData"] = rawData
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminChangeUserSettings", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing users");
        JObject response = await _httpClient.PostJsonAsync<JObject>("AdminListUsers", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task AddRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        }
        _logger.LogDebug("Admin adding role '{RoleName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminAddRole", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin created role '{RoleName}'", name);
    }

    public async Task DeleteRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        }
        _logger.LogDebug("Admin deleting role '{RoleName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminDeleteRole", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin deleted role '{RoleName}'", name);
    }

    public async Task EditRoleAsync(string name, string description, int maxOutpathDepth, int maxT2iSimultaneous, bool allowUnsafeOutpaths, IEnumerable<string>? modelWhitelist,
        IEnumerable<string>? modelBlacklist, IEnumerable<string>? permissions, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Role description cannot be null or empty", nameof(description));
        }
        // The AdminEditRole API is documented as taking comma-separated strings — that IS the server contract.
        // Consequence: model names containing commas cannot be expressed in role lists (server-side limitation).
        string whitelistString = modelWhitelist == null ? string.Empty : string.Join(",", modelWhitelist);
        string blacklistString = modelBlacklist == null ? string.Empty : string.Join(",", modelBlacklist);
        string permissionsString = permissions == null ? string.Empty : string.Join(",", permissions);
        _logger.LogDebug("Admin editing role '{RoleName}'", name);
        JObject payload = new()
        {
            ["name"] = name,
            ["description"] = description,
            ["max_outpath_depth"] = maxOutpathDepth,
            ["max_t2i_simultaneous"] = maxT2iSimultaneous,
            ["allow_unsafe_outpaths"] = allowUnsafeOutpaths,
            ["model_whitelist"] = whitelistString,
            ["model_blacklist"] = blacklistString,
            ["permissions"] = permissionsString
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("AdminEditRole", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing roles");
        JObject response = await _httpClient.PostJsonAsync<JObject>("AdminListRoles", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing permissions");
        JObject response = await _httpClient.PostJsonAsync<JObject>("AdminListPermissions", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<ServerStatusResponse> GetGlobalStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin getting global status");
        ServerStatusResponse response = await _httpClient.PostJsonAsync<ServerStatusResponse>("GetGlobalStatus", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> GetServerResourceInfoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin getting server resource info");
        JObject response = await _httpClient.PostJsonAsync<JObject>("GetServerResourceInfo", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListConnectedUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing connected users");
        JObject response = await _httpClient.PostJsonAsync<JObject>("ListConnectedUsers", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListServerSettingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing server settings");
        JObject response = await _httpClient.PostJsonAsync<JObject>("ListServerSettings", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task ChangeServerSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _logger.LogDebug("Admin changing server settings with {SettingCount} entries", settings.Count);
        JObject rawData = JObject.FromObject(settings);
        JObject payload = new()
        {
            ["rawData"] = rawData
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("ChangeServerSettings", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin checking for updates");
        JObject response = await _httpClient.PostJsonAsync<JObject>("CheckForUpdates", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> InstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        _logger.LogDebug("Admin installing extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("InstallExtension", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UninstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        _logger.LogDebug("Admin uninstalling extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("UninstallExtension", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UpdateExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        _logger.LogDebug("Admin updating extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("UpdateExtension", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UpdateAndRestartAsync(bool updateExtensions = false, bool updateBackends = false, bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin triggering update and restart (updateExtensions={UpdateExtensions}, updateBackends={UpdateBackends}, force={Force})", updateExtensions, updateBackends, force);
        JObject payload = new()
        {
            ["updateExtensions"] = updateExtensions,
            ["updateBackends"] = updateBackends,
            ["force"] = force
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("UpdateAndRestart", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListLogTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin listing log types");
        JObject response = await _httpClient.PostJsonAsync<JObject>("ListLogTypes", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListRecentLogMessagesAsync(IEnumerable<string> types, Dictionary<string, long>? lastSequenceIds = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(types);
        _logger.LogDebug("Admin listing recent log messages");
        JArray typeArray = JArray.FromObject(types);
        JObject raw = new()
        {
            ["types"] = typeArray
        };
        if (lastSequenceIds != null && lastSequenceIds.Count > 0)
        {
            JObject lastSeqObject = JObject.FromObject(lastSequenceIds);
            raw["last_sequence_ids"] = lastSeqObject;
        }
        JObject payload = new()
        {
            ["raw"] = raw
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("ListRecentLogMessages", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<string> LogSubmitToPastebinAsync(string minimumLevel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(minimumLevel))
        {
            throw new ArgumentException("Minimum log level cannot be null or empty", nameof(minimumLevel));
        }
        _logger.LogDebug("Admin submitting logs to pastebin with minimum level '{Level}'", minimumLevel);
        JObject payload = new()
        {
            ["type"] = minimumLevel
        };
        JObject response = await _httpClient.PostJsonAsync<JObject>("LogSubmitToPastebin", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        string url = string.Empty;
        if (response != null && response["url"] != null)
        {
            url = response["url"]!.ToString() ?? string.Empty;
        }
        return url;
    }

    public async Task ShutdownServerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Admin shutting down SwarmUI server");
        JObject _ = await _httpClient.PostJsonAsync<JObject>("ShutdownServer", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task DebugGenerateDocsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Admin triggering API documentation generation");
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DebugGenDocs", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task DebugAddLanguageDataAsync(IEnumerable<string> words, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(words);
        _logger.LogDebug("Admin adding language data entries");
        JArray setArray = JArray.FromObject(words);
        JObject raw = new()
        {
            ["set"] = setArray
        };
        JObject payload = new()
        {
            ["raw"] = raw
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DebugLanguageAdd", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }
}
