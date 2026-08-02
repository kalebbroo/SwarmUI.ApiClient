using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Common;
using SwarmUI.ApiClient.Contracts.Enums;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Contracts.Responses;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Endpoints.Models;

/// <summary>Provides access to SwarmUI model management endpoints.</summary>
public class ModelsEndpoint : IModelsEndpoint
{
    private readonly ISwarmHttpClient _httpClient;
    private readonly ISwarmWebSocketClient _webSocketClient;
    private readonly string _sessionKey;
    private readonly ILogger<ModelsEndpoint> _logger;

    /// <summary>Creates a new ModelsEndpoint.</summary>
    /// <param name="httpClient">HTTP client wrapper for model HTTP operations.</param>
    /// <param name="webSocketClient">WebSocket client for streaming model operations.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public ModelsEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, ILogger<ModelsEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<ModelsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<ModelListResponse> ListModelsAsync(string modelType = "Stable-Diffusion", string path = "", int depth = 4, string sortBy = "Name",
        bool allowRemote = true, bool sortReverse = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing models of type '{ModelType}' at path '{Path}' with depth={Depth}", modelType, path, depth);
        ModelListResponse response = await _httpClient.PostJsonAsync<ModelListResponse>("ListModels",
            new
            {
                path,
                depth,
                subtype = modelType,
                sortBy,
                allowRemote,
                sortReverse
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {FolderCount} folders and {FileCount} models for type '{ModelType}' at path '{Path}'",
            response.Folders?.Count ?? 0, response.Files?.Count ?? 0, modelType, path);
        return response;
    }

    /// <inheritdoc />
    public async Task<ModelDescription> DescribeModelAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        _logger.LogDebug("Describing model '{ModelName}' of type '{ModelType}'", modelName, modelType);
        JObject response = await _httpClient.PostJsonAsync<JObject>("DescribeModel",
            new
            {
                modelName,
                subtype = modelType
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
        JToken? modelToken = response["model"];
        ModelDescription? description = modelToken is { Type: not JTokenType.Null }
            ? modelToken.ToObject<ModelDescription>()
            : response.ToObject<ModelDescription>();
        if (description is null || string.IsNullOrEmpty(description.Name))
        {
            // No fabricated fallback: an unusable response is an error, not a fake success.
            throw new SwarmException($"DescribeModel returned no usable description for model '{modelName}'", "no_result");
        }
        return description;
    }

    /// <inheritdoc />
    public async Task DeleteModelAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        _logger.LogDebug("Deleting model '{ModelName}' of type '{ModelType}'", modelName, modelType);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DeleteModel",
            new
            {
                modelName,
                subtype = modelType
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteWildcardAsync(string card, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(card))
        {
            throw new ArgumentException("Wildcard card name cannot be null or empty", nameof(card));
        }
        _logger.LogDebug("Deleting wildcard card '{Card}'", card);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("DeleteWildcard",
            new
            {
                card
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ModelHashResponse> GetModelHashAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        _logger.LogDebug("Getting model hash for '{ModelName}' of type '{ModelType}'", modelName, modelType);
        return await _httpClient.PostJsonAsync<ModelHashResponse>("GetModelHash",
            new
            {
                modelName,
                subtype = modelType
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EditModelMetadataAsync(EditModelMetadataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Editing metadata for model '{Model}' of type '{Subtype}'", request.Model, request.Subtype);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("EditModelMetadata", JObject.FromObject(request), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EditWildcardAsync(EditWildcardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Editing wildcard card '{Card}'", request.Card);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("EditWildcard", JObject.FromObject(request), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<JObject> ForwardMetadataRequestAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }
        _logger.LogDebug("Forwarding metadata request to URL '{Url}'", url);
        return await _httpClient.PostJsonAsync<JObject>("ForwardMetadataRequest",
            new
            {
                url
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LoadedModelsResponse> ListLoadedModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing currently loaded models");
        return await _httpClient.PostJsonAsync<LoadedModelsResponse>("ListLoadedModels", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RenameModelAsync(string oldName, string newName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldName))
        {
            throw new ArgumentException("Old model name cannot be null or empty", nameof(oldName));
        }
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("New model name cannot be null or empty", nameof(newName));
        }
        _logger.LogDebug("Renaming model from '{OldName}' to '{NewName}' (type '{ModelType}')", oldName, newName, modelType);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("RenameModel",
            new
            {
                oldName,
                newName,
                subtype = modelType
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SelectModelAsync(string model, string? backendId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model cannot be null or empty", nameof(model));
        }
        _logger.LogDebug("Selecting model '{Model}' on backend '{BackendId}' (null means all backends)", model, backendId ?? string.Empty);
        JObject _ = await _httpClient.PostJsonAsync<JObject>("SelectModel",
            new
            {
                model,
                backendId
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ModelOperationUpdate> StreamModelDownloadAsync(string url, string modelType, string name, string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Validate eagerly so failures throw at the call site, not at first enumeration.
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }
        if (string.IsNullOrWhiteSpace(modelType))
        {
            throw new ArgumentException("Model type cannot be null or empty", nameof(modelType));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Model filename cannot be null or empty", nameof(name));
        }
        _logger.LogInformation("Starting model download from '{Url}' as '{Name}' of type '{ModelType}'", url, name, modelType);
        JObject payload = new()
        {
            ["url"] = url,
            ["type"] = modelType,
            ["name"] = name
        };
        if (!string.IsNullOrEmpty(metadata))
        {
            payload["metadata"] = metadata;
        }
        return StreamModelOperationCoreAsync("DoModelDownloadWS", payload, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ModelOperationUpdate> StreamModelDownloadAsync(string url, SwarmSubType subType, string name, string? metadata = null,
        CancellationToken cancellationToken = default)
        => StreamModelDownloadAsync(url, subType.AsApiType(), name, metadata, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<ModelOperationUpdate> StreamModelSelectionAsync(string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model cannot be null or empty", nameof(model));
        }
        _logger.LogInformation("Starting model selection stream for model '{Model}'", model);
        JObject payload = new()
        {
            ["model"] = model
        };
        return StreamModelOperationCoreAsync("SelectModelWS", payload, cancellationToken);
    }

    /// <summary>Shared streaming core for model operation endpoints.</summary>
    private async IAsyncEnumerable<ModelOperationUpdate> StreamModelOperationCoreAsync(string endpoint, JObject payload, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (JObject frame in _webSocketClient.StreamFramesAsync(endpoint, payload, _sessionKey, cancellationToken).ConfigureAwait(false))
        {
            ModelOperationUpdate? update = frame.ToObject<ModelOperationUpdate>();
            if (update is not null)
            {
                yield return update;
            }
        }
    }

    /// <inheritdoc />
    public async Task<TestPromptFillResponse> TestPromptFillAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }
        _logger.LogDebug("Testing prompt fill for prompt of length {Length}", prompt.Length);
        return await _httpClient.PostJsonAsync<TestPromptFillResponse>("TestPromptFill",
            new
            {
                prompt
            },
            _sessionKey, cancellationToken).ConfigureAwait(false);
    }
}
