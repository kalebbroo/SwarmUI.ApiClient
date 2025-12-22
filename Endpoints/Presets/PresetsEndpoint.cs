using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Endpoints.Presets;

/// <summary>Provides access to SwarmUI preset management endpoints.</summary>
public class PresetsEndpoint : IPresetsEndpoint
{
    /// <summary>Internal implementation data containing dependencies for the presets endpoint.</summary>
    public struct Impl
    {
        /// <summary>HTTP client wrapper used for preset-related HTTP operations with session injection and SwarmUI-specific error handling.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>Session manager used to obtain and refresh session IDs for preset operations.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for preset operations.</summary>
        public ILogger<PresetsEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should go through the public methods.</summary>
    public Impl Internal;

    /// <summary>Creates a new PresetsEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client wrapper for preset HTTP operations. Must not be null.</param>
    /// <param name="sessionManager">Session manager used indirectly for session injection. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses <see cref="NullLogger"/> if null.</param>
    /// <remarks>The endpoint relies on injected HTTP and session services managed by <c>SwarmClient</c>, which keeps resource ownership centralized and makes the endpoint easy to test by mocking <see cref="ISwarmHttpClient"/>.</remarks>
    public PresetsEndpoint(ISwarmHttpClient httpClient, ISessionManager sessionManager, ILogger<PresetsEndpoint>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(sessionManager);
        Internal.HttpClient = httpClient;
        Internal.SessionManager = sessionManager;
        Internal.Logger = logger ?? NullLogger<PresetsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task AddNewPresetAsync(PresetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Preset title cannot be null or empty", nameof(request));
        }
        if (request.Parameters == null)
        {
            throw new ArgumentException("Preset parameters cannot be null", nameof(request));
        }
        Internal.Logger.LogDebug("Adding preset '{Title}' (IsEdit={IsEdit}) with {ParameterCount} parameters", request.Title,
            request.IsEdit, request.Parameters.Count);
        JObject paramMap = JObject.FromObject(request.Parameters);
        JObject rawObject = new()
        {
            ["param_map"] = paramMap
        };
        JObject payload = new()
        {
            ["title"] = request.Title,
            ["description"] = request.Description ?? string.Empty,
            ["raw"] = rawObject,
            ["is_edit"] = request.IsEdit
        };
        if (!string.IsNullOrEmpty(request.PreviewImage))
        {
            payload["preview_image"] = request.PreviewImage;
        }
        if (request.IsEdit && !string.IsNullOrEmpty(request.EditingName))
        {
            payload["editing"] = request.EditingName;
        }
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("AddNewPreset", payload, cancellationToken).ConfigureAwait(false);
        if (response != null)
        {
            JToken? presetFailToken = response["preset_fail"];
            if (presetFailToken != null && presetFailToken.Type != JTokenType.Null)
            {
                string error = presetFailToken.ToString() ?? "Unknown error";
                Internal.Logger.LogWarning("Failed to add or edit preset '{Title}': {Error}", request.Title, error);
                throw new InvalidOperationException("Failed to add preset: " + error);
            }
        }
        Internal.Logger.LogInformation("Preset '{Title}' {Operation} successfully", request.Title, request.IsEdit ? "edited" : "created");
    }

    /// <inheritdoc />
    public async Task DeletePresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            throw new ArgumentException("Preset name cannot be null or empty", nameof(presetName));
        }
        Internal.Logger.LogDebug("Deleting preset '{PresetName}'", presetName);
        JObject payload = new()
        {
            ["preset"] = presetName
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DeletePreset", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Preset deleted successfully: {PresetName}", presetName);
    }

    /// <inheritdoc />
    public async Task DuplicatePresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            throw new ArgumentException("Preset name cannot be null or empty", nameof(presetName));
        }
        Internal.Logger.LogDebug("Duplicating preset '{PresetName}'", presetName);
        JObject payload = new()
        {
            ["preset"] = presetName
        };
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DuplicatePreset", payload, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Preset duplicated successfully: {PresetName}", presetName);
    }
}
