using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.Admin;

/// <summary>Provides access to SwarmUI administrative endpoints.</summary>
/// <remarks>Requires administrative permissions and covers user, role, server, extension, and log management. See the SwarmUI AdminAPI documentation for the full endpoint list and schemas.</remarks>
public interface IAdminEndpoint
{
    /// <summary>
    /// Creates a new user account using SwarmUI's administrative user management APIs.
    /// </summary>
    /// <param name="name">The username for the new account. Must be unique.</param>
    /// <param name="password">Initial password for the user. The caller is responsible for enforcing password policy.</param>
    /// <param name="role">The initial role to assign (for example, <c>user</c>, <c>admin</c>).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminAddUser</c> route.
    ///
    /// This is an administrative-only operation, guarded by the <c>manage_users</c> permission
    /// flag on the SwarmUI server. Use this from privileged tooling (for example, an
    /// operator console), not from untrusted client code.
    /// </remarks>
    Task AddUserAsync(string name, string password, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing user account.
    /// </summary>
    /// <param name="name">The username of the account to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminDeleteUser</c> route.
    /// Deletion is permanent from SwarmUI's perspective; callers should implement any
    /// confirmation or soft-delete semantics at a higher level if needed.
    /// </remarks>
    Task DeleteUserAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information about a specific user, including settings and
    /// effective limits derived from roles.
    /// </summary>
    /// <param name="name">The username of the account to inspect.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object containing fields such as <c>user_id</c>, <c>password_set_by_admin</c>,
    /// and <c>settings</c>. The exact schema is defined by SwarmUI's <c>AdminGetUserInfo</c>
    /// documentation and may evolve over time.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminGetUserInfo</c> route.
    /// This method returns a raw <see cref="JObject"/> for maximum forwards compatibility.
    /// </remarks>
    Task<JObject> GetUserInfoAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Force-sets a user's password as an administrator.
    /// </summary>
    /// <param name="name">The username of the account to modify.</param>
    /// <param name="password">The new password value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminSetUserPassword</c> route.
    /// This operation is guarded by the <c>manage_users</c> permission flag and should be
    /// used sparingly, for example when a user is locked out and recovery is required.
    /// </remarks>
    Task SetUserPasswordAsync(string name, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's settings as an administrator.
    /// </summary>
    /// <param name="name">The username of the account whose settings should be changed.</param>
    /// <param name="settings">
    /// Map of setting identifiers to new values. These will be applied as if the user
    /// had changed their own settings, but without requiring their session.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminChangeUserSettings</c> route.
    /// The underlying API expects a <c>rawData</c> object whose <c>settings</c> property
    /// contains the map of setting IDs to values; the endpoint implementation shapes this
    /// request accordingly.
    /// </remarks>
    Task ChangeUserSettingsAsync(string name, Dictionary<string, object> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all known user IDs on the SwarmUI server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>users</c> property is an array of usernames.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminListUsers</c> route.
    /// </remarks>
    Task<JObject> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new permission role that can be assigned to users.
    /// </summary>
    /// <param name="name">The unique role name (for example, <c>power_user</c>).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminAddRole</c> route.
    /// Additional role details (description, permissions, limits) can be configured
    /// via <see cref="EditRoleAsync"/> after creation.
    /// </remarks>
    Task AddRoleAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing permission role.
    /// </summary>
    /// <param name="name">The name of the role to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminDeleteRole</c> route.
    /// Only unused or non-critical roles should be removed; callers are responsible for
    /// ensuring no users depend on this role.
    /// </remarks>
    Task DeleteRoleAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits all configurable properties of an existing role, including limits and permissions.
    /// </summary>
    /// <param name="name">The name of the role to edit.</param>
    /// <param name="description">Human-readable description of the role.</param>
    /// <param name="maxOutpathDepth">Maximum outpath depth allowed for this role.</param>
    /// <param name="maxT2iSimultaneous">Maximum number of simultaneous T2I generations for this role.</param>
    /// <param name="allowUnsafeOutpaths">Whether users with this role may use unsafe output paths.</param>
    /// <param name="modelWhitelist">
    /// Optional list of model names this role is allowed to use. If null or empty, all models
    /// are allowed unless blacklisted.
    /// </param>
    /// <param name="modelBlacklist">
    /// Optional list of model names this role is forbidden from using.
    /// </param>
    /// <param name="permissions">
    /// List of permission node identifiers to enable for this role.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminEditRole</c> route.
    /// </remarks>
    Task EditRoleAsync(string name, string description, int maxOutpathDepth, int maxT2iSimultaneous, bool allowUnsafeOutpaths, IEnumerable<string>? modelWhitelist,
        IEnumerable<string>? modelBlacklist, IEnumerable<string>? permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all defined permission roles and their properties.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>roles</c> property is a map from role key to role definition
    /// as documented in SwarmUI's AdminAPI.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminListRoles</c> route.
    /// </remarks>
    Task<JObject> ListRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available permission nodes that can be attached to roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>permissions</c> property contains detailed permission
    /// metadata including names, descriptions, groups, and safety levels.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/AdminListPermissions</c> route.
    /// </remarks>
    Task<JObject> ListPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current global generation status across all sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="ServerStatusResponse"/> mirroring the structure of WebSocket
    /// status messages and the <c>GetCurrentStatus</c> HTTP endpoint.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/GetGlobalStatus</c> route.
    /// Requires the <c>read_server_info_panels</c> permission flag.
    /// </remarks>
    Task<ServerStatusResponse> GetGlobalStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves live server resource usage information including CPU, RAM, and GPU stats.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object containing <c>cpu</c>, <c>system_ram</c>, and <c>gpus</c> sections as
    /// documented in SwarmUI's AdminAPI.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/GetServerResourceInfo</c> route.
    /// </remarks>
    Task<JObject> GetServerResourceInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently connected users and their active sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>users</c> property contains user connection details,
    /// including last active time and active session addresses.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ListConnectedUsers</c> route.
    /// </remarks>
    Task<JObject> ListConnectedUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all configurable server settings along with metadata such as type, current
    /// value, and allowed values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>settings</c> property maps setting IDs to metadata objects.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ListServerSettings</c> route.
    /// </remarks>
    Task<JObject> ListServerSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes one or more server settings.
    /// </summary>
    /// <param name="settings">
    /// Map of setting identifiers to new values. Values must be compatible with the
    /// types and allowed ranges described by <see cref="ListServerSettingsAsync"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ChangeServerSettings</c> route.
    /// The endpoint shapes this into the <c>rawData</c> payload expected by SwarmUI.
    /// </remarks>
    Task ChangeServerSettingsAsync(Dictionary<string, object> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for available updates to SwarmUI itself, installed extensions, and backends.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object containing fields such as <c>server_updates_count</c>,
    /// <c>server_updates_preview</c>, <c>extension_updates</c>, and <c>backend_updates</c>.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/CheckForUpdates</c> route.
    /// </remarks>
    Task<JObject> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs an extension from the server's known extensions list.
    /// </summary>
    /// <param name="extensionName">The extension identifier as listed by the server.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A JSON object whose <c>success</c> field indicates whether installation succeeded.</returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/InstallExtension</c> route.
    /// Installation does not automatically restart SwarmUI but may signal that a rebuild is required.
    /// </remarks>
    Task<JObject> InstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls an installed extension.
    /// </summary>
    /// <param name="extensionName">The extension identifier to uninstall.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A JSON object whose <c>success</c> field indicates whether uninstallation succeeded.</returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/UninstallExtension</c> route.
    /// Uninstalling does not automatically restart SwarmUI but may signal that a rebuild is required.
    /// </remarks>
    Task<JObject> UninstallExtensionAsync(string extensionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an update for a specific installed extension.
    /// </summary>
    /// <param name="extensionName">The extension identifier to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A JSON object whose <c>success</c> field indicates whether an update was applied.</returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/UpdateExtension</c> route.
    /// Updating does not automatically restart SwarmUI but may signal that a rebuild is required.
    /// </remarks>
    Task<JObject> UpdateExtensionAsync(string extensionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates SwarmUI and optionally extensions and backends, then restarts the server
    /// if changes are available or <paramref name="force"/> is set.
    /// </summary>
    /// <param name="updateExtensions">Whether to also update extensions.</param>
    /// <param name="updateBackends">Whether to also update backends.</param>
    /// <param name="force">Whether to force a restart even if no updates are detected.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object with <c>success</c> and <c>result</c> fields describing the outcome.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/UpdateAndRestart</c> route.
    /// </remarks>
    Task<JObject> UpdateAndRestartAsync(bool updateExtensions = false, bool updateBackends = false, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of all available log types on the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object whose <c>types_available</c> property is an array of log type
    /// descriptors including name, color, and identifier.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ListLogTypes</c> route.
    /// </remarks>
    Task<JObject> ListLogTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns recent server log messages for one or more log types, with support for
    /// continuation via last-sequence IDs.
    /// </summary>
    /// <param name="types">Log type identifiers to include (for example, <c>info</c>, <c>error</c>).</param>
    /// <param name="lastSequenceIds">
    /// Optional map of log type identifier to last-seen sequence ID. When provided,
    /// the server will return only messages after these IDs.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A JSON object with <c>last_sequence_id</c> and <c>data</c> fields as documented
    /// in SwarmUI's AdminAPI.
    /// </returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ListRecentLogMessages</c> route.
    /// </remarks>
    Task<JObject> ListRecentLogMessagesAsync(IEnumerable<string> types, Dictionary<string, long>? lastSequenceIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits current server log data to a pastebin service and returns the resulting URL.
    /// </summary>
    /// <param name="minimumLevel">
    /// Minimum log level to include (for example, <c>verbose</c>, <c>debug</c>, <c>info</c>).
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The URL of the created pastebin entry.</returns>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/LogSubmitToPastebin</c> route.
    /// </remarks>
    Task<string> LogSubmitToPastebinAsync(string minimumLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts the SwarmUI server down.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/ShutdownServer</c> route.
    /// The API returns success before the server actually terminates.
    /// </remarks>
    Task ShutdownServerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers generation of API documentation on the server for debugging purposes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/DebugGenDocs</c> route.
    /// Intended for internal tooling and diagnostics rather than end-user workflows.
    /// </remarks>
    Task DebugGenerateDocsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds language data to SwarmUI's language file builder for debugging/localization
    /// tooling.
    /// </summary>
    /// <param name="words">Set of words or phrases to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Maps to SwarmUI's <c>/API/DebugLanguageAdd</c> route.
    /// </remarks>
    Task DebugAddLanguageDataAsync(IEnumerable<string> words, CancellationToken cancellationToken = default);
}
