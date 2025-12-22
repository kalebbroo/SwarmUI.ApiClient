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

namespace SwarmUI.ApiClient.Endpoints.Admin;

/// <summary>Implements SwarmUI administrative endpoints using HTTP-based AdminAPI routes.</summary>
/// <remarks>Follows the shared endpoint patterns described in CodingGuidelines.md (Admin endpoints section).</remarks>
public class AdminEndpoint : IAdminEndpoint
{
    public struct Impl
    {
        public ISwarmHttpClient HttpClient;
        public ISessionManager SessionManager;
        public ILogger<AdminEndpoint> Logger;
    }

    public Impl Internal;

    public AdminEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<AdminEndpoint>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(sessionManager);
        Internal.HttpClient = httpClient;
        Internal.SessionManager = sessionManager;
        Internal.Logger = logger ?? NullLogger<AdminEndpoint>.Instance;
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
        Internal.Logger.LogDebug("Admin adding user '{UserName}' with role '{Role}'", name, role);
        JObject payload = new()
        {
            ["name"] = name,
            ["password"] = password,
            ["role"] = role
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminAddUser", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Admin created user '{UserName}' with role '{Role}'", name, role);
    }

    public async Task DeleteUserAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        Internal.Logger.LogDebug("Admin deleting user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminDeleteUser", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Admin deleted user '{UserName}'", name);
    }

    public async Task<JObject> GetUserInfoAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        Internal.Logger.LogDebug("Admin fetching info for user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AdminGetUserInfo", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Admin setting password for user '{UserName}'", name);
        JObject payload = new()
        {
            ["name"] = name,
            ["password"] = password
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminSetUserPassword", payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangeUserSettingsAsync(string name, Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be null or empty", nameof(name));
        }
        ArgumentNullException.ThrowIfNull(settings);
        Internal.Logger.LogDebug("Admin changing settings for user '{UserName}' with {SettingCount} entries", name, settings.Count);
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
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminChangeUserSettings", payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing users");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AdminListUsers", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task AddRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        }
        Internal.Logger.LogDebug("Admin adding role '{RoleName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminAddRole", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Admin created role '{RoleName}'", name);
    }

    public async Task DeleteRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        }
        Internal.Logger.LogDebug("Admin deleting role '{RoleName}'", name);
        JObject payload = new()
        {
            ["name"] = name
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminDeleteRole", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Admin deleted role '{RoleName}'", name);
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
        string whitelistString = modelWhitelist == null ? string.Empty : string.Join(",", modelWhitelist);
        string blacklistString = modelBlacklist == null ? string.Empty : string.Join(",", modelBlacklist);
        string permissionsString = permissions == null ? string.Empty : string.Join(",", permissions);
        Internal.Logger.LogDebug("Admin editing role '{RoleName}'", name);
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
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("AdminEditRole", payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing roles");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AdminListRoles", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing permissions");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AdminListPermissions", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<ServerStatusResponse> GetGlobalStatusAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin getting global status");
        ServerStatusResponse response = await Internal.HttpClient.PostJsonAsync<ServerStatusResponse>("GetGlobalStatus", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> GetServerResourceInfoAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin getting server resource info");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("GetServerResourceInfo", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListConnectedUsersAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing connected users");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ListConnectedUsers", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListServerSettingsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing server settings");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ListServerSettings", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task ChangeServerSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Internal.Logger.LogDebug("Admin changing server settings with {SettingCount} entries", settings.Count);
        JObject rawData = JObject.FromObject(settings);
        JObject payload = new()
        {
            ["rawData"] = rawData
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("ChangeServerSettings", payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JObject> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin checking for updates");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("CheckForUpdates", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> InstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        Internal.Logger.LogDebug("Admin installing extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("InstallExtension", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UninstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        Internal.Logger.LogDebug("Admin uninstalling extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("UninstallExtension", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UpdateExtensionAsync(string extensionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));
        }
        Internal.Logger.LogDebug("Admin updating extension '{ExtensionName}'", extensionName);
        JObject payload = new()
        {
            ["extensionName"] = extensionName
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("UpdateExtension", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> UpdateAndRestartAsync(bool updateExtensions = false, bool updateBackends = false, bool force = false, CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin triggering update and restart (updateExtensions={UpdateExtensions}, updateBackends={UpdateBackends}, force={Force})", updateExtensions, updateBackends, force);
        JObject payload = new()
        {
            ["updateExtensions"] = updateExtensions,
            ["updateBackends"] = updateBackends,
            ["force"] = force
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("UpdateAndRestart", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListLogTypesAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin listing log types");
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ListLogTypes", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JObject> ListRecentLogMessagesAsync(IEnumerable<string> types, Dictionary<string, long>? lastSequenceIds = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(types);
        Internal.Logger.LogDebug("Admin listing recent log messages");
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
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ListRecentLogMessages", payload, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<string> LogSubmitToPastebinAsync(string minimumLevel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(minimumLevel))
        {
            throw new ArgumentException("Minimum log level cannot be null or empty", nameof(minimumLevel));
        }
        Internal.Logger.LogDebug("Admin submitting logs to pastebin with minimum level '{Level}'", minimumLevel);
        JObject payload = new()
        {
            ["type"] = minimumLevel
        };
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("LogSubmitToPastebin", payload, cancellationToken).ConfigureAwait(false);
        string url = string.Empty;
        if (response != null && response["url"] != null)
        {
            url = response["url"]!.ToString() ?? string.Empty;
        }
        return url;
    }

    public async Task ShutdownServerAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogWarning("Admin shutting down SwarmUI server");
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("ShutdownServer", payload: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task DebugGenerateDocsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Admin triggering API documentation generation");
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DebugGenDocs", payload: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task DebugAddLanguageDataAsync(IEnumerable<string> words, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(words);
        Internal.Logger.LogDebug("Admin adding language data entries");
        JArray setArray = JArray.FromObject(words);
        JObject raw = new()
        {
            ["set"] = setArray
        };
        JObject payload = new()
        {
            ["raw"] = raw
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DebugLanguageAdd", payload, cancellationToken).ConfigureAwait(false);
    }
}
