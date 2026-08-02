using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Endpoints.Presets;

/// <summary>Provides access to SwarmUI preset management endpoints.</summary>
public class PresetsEndpoint : IPresetsEndpoint
{
    private readonly ISwarmHttpClient _httpClient;
    private readonly string _sessionKey;
    private readonly ILogger<PresetsEndpoint> _logger;

    /// <summary>Creates a new PresetsEndpoint.</summary>
    /// <param name="httpClient">HTTP client wrapper for preset HTTP operations.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public PresetsEndpoint(ISwarmHttpClient httpClient, string sessionKey, ILogger<PresetsEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<PresetsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task AddNewPresetAsync(PresetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Preset title cannot be null or empty", nameof(request));
        }
        if (request.Parameters is null)
        {
            throw new ArgumentException("Preset parameters cannot be null", nameof(request));
        }
        _logger.LogDebug("Adding preset '{Title}' (IsEdit={IsEdit}) with {ParameterCount} parameters", request.Title,
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
        JObject response = await _httpClient.PostJsonAsync<JObject>("AddNewPreset", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        JToken? presetFailToken = response["preset_fail"];
        if (presetFailToken is not null && presetFailToken.Type != JTokenType.Null)
        {
            string error = presetFailToken.ToString();
            _logger.LogWarning("Failed to add or edit preset '{Title}': {Error}", request.Title, error);
            throw new SwarmException("Failed to add preset: " + error, "preset_fail");
        }
        _logger.LogInformation("Preset '{Title}' {Operation} successfully", request.Title, request.IsEdit ? "edited" : "created");
    }

    /// <inheritdoc />
    public async Task DeletePresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            throw new ArgumentException("Preset name cannot be null or empty", nameof(presetName));
        }
        _logger.LogDebug("Deleting preset '{PresetName}'", presetName);
        JObject payload = new()
        {
            ["preset"] = presetName
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DeletePreset", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Preset deleted successfully: {PresetName}", presetName);
    }

    /// <inheritdoc />
    public async Task DuplicatePresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            throw new ArgumentException("Preset name cannot be null or empty", nameof(presetName));
        }
        _logger.LogDebug("Duplicating preset '{PresetName}'", presetName);
        JObject payload = new()
        {
            ["preset"] = presetName
        };
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DuplicatePreset", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Preset duplicated successfully: {PresetName}", presetName);
    }
}
