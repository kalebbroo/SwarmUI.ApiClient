using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Endpoints.Models;

/// <summary>Provides access to SwarmUI model management endpoints.</summary>
/// <remarks>Follows the shared endpoint patterns for HTTP and WebSocket operations described in CodingGuidelines.md.</remarks>
public class ModelsEndpoint : IModelsEndpoint
{
    /// <summary>Internal implementation data containing dependencies for the Models endpoint.</summary>
    public struct Impl
    {
        /// <summary>HTTP client wrapper used for all model-related HTTP operations.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>WebSocket client used for model-related streaming operations.</summary>
        public ISwarmWebSocketClient WebSocketClient;

        /// <summary>Session manager responsible for obtaining and refreshing session IDs.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for model management operations.</summary>
        public ILogger<ModelsEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public methods.</summary>
    public Impl Internal;

    /// <summary>Creates a new ModelsEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client wrapper for model HTTP operations. Must not be null.</param>
    /// <param name="webSocketClient">WebSocket client for streaming model operations. Must not be null.</param>
    /// <param name="sessionManager">Session manager used indirectly for session injection. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    /// <remarks>Relies on injected HTTP, WebSocket, and session services managed by SwarmClient. See CodingGuidelines.md (Models endpoint section) for DI and testing guidance.</remarks>
    public ModelsEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, ISessionManager sessionManager, ILogger<ModelsEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.WebSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<ModelsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<ModelListResponse> ListModelsAsync(string modelType = "Stable-Diffusion", string path = "", int depth = 4, string sortBy = "Name",
        bool allowRemote = true, bool sortReverse = false, CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing models of type '{ModelType}' at path '{Path}' with depth={Depth}, sortBy={SortBy}, allowRemote={AllowRemote}, sortReverse={SortReverse}",
            modelType, path, depth, sortBy, allowRemote, sortReverse);
        ModelListResponse response = await Internal.HttpClient.PostJsonAsync<ModelListResponse>("ListModels",
            new
            {
                path,
                depth,
                subtype = modelType,
                sortBy,
                allowRemote,
                sortReverse
            },
            cancellationToken).ConfigureAwait(false);
        int folderCount = response.Folders != null ? response.Folders.Count : 0;
        int fileCount = response.Files != null ? response.Files.Count : 0;
        Internal.Logger.LogInformation("Retrieved {FolderCount} folders and {FileCount} models for type '{ModelType}' at path '{Path}'",
            folderCount, fileCount, modelType, path);
        return response;
    }

    /// <inheritdoc />
    public async Task<ModelDescription> DescribeModelAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        Internal.Logger.LogDebug("Describing model '{ModelName}' of type '{ModelType}'", modelName, modelType);
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("DescribeModel",
            new
            {
                modelName,
                subtype = modelType
            },
            cancellationToken).ConfigureAwait(false);
        ModelDescription? description = null;
        if (response != null)
        {
            JToken? modelToken = response["model"];
            if (modelToken != null && modelToken.Type != JTokenType.Null)
            {
                description = modelToken.ToObject<ModelDescription>();
            }
            else
            {
                description = response.ToObject<ModelDescription>();
            }
        }
        if (description == null)
        {
            ModelDescription fallback = new()
            {
                Name = modelName,
                Description = "No description available"
            };
            return fallback;
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
        Internal.Logger.LogDebug("Deleting model '{ModelName}' of type '{ModelType}'", modelName, modelType);

        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DeleteModel",
            new
            {
                modelName,
                subtype = modelType
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteWildcardAsync(string card, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(card))
        {
            throw new ArgumentException("Wildcard card name cannot be null or empty", nameof(card));
        }
        Internal.Logger.LogDebug("Deleting wildcard card '{Card}'", card);
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("DeleteWildcard",
            new
            {
                card
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ModelHashResponse> GetModelHashAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }
        Internal.Logger.LogDebug("Getting model hash for '{ModelName}' of type '{ModelType}'", modelName, modelType);
        ModelHashResponse response = await Internal.HttpClient.PostJsonAsync<ModelHashResponse>("GetModelHash",
            new
            {
                modelName,
                subtype = modelType
            },
            cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async Task EditModelMetadataAsync(EditModelMetadataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Internal.Logger.LogDebug("Editing metadata for model '{Model}' of type '{Subtype}'", request.Model, request.Subtype);
        JObject _ = await Internal.HttpClient.PostJsonAsync<EditModelMetadataRequest, JObject>("EditModelMetadata", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EditWildcardAsync(EditWildcardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Internal.Logger.LogDebug("Editing wildcard card '{Card}'", request.Card);
        JObject _ = await Internal.HttpClient.PostJsonAsync<EditWildcardRequest, JObject>("EditWildcard", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<JObject> ForwardMetadataRequestAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }
        Internal.Logger.LogDebug("Forwarding metadata request to URL '{Url}'", url);
        JObject response = await Internal.HttpClient.PostJsonAsync<JObject>("ForwardMetadataRequest",
            new
            {
                url
            },
            cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async Task<LoadedModelsResponse> ListLoadedModelsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing currently loaded models");
        LoadedModelsResponse response = await Internal.HttpClient.PostJsonAsync<LoadedModelsResponse>("ListLoadedModels", payload: null, cancellationToken).ConfigureAwait(false);
        return response;
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
        Internal.Logger.LogDebug("Renaming model from '{OldName}' to '{NewName}' (type '{ModelType}')", oldName, newName, modelType);
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("RenameModel",
            new
            {
                oldName,
                newName,
                subtype = modelType
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SelectModelAsync(string model, string? backendId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model cannot be null or empty", nameof(model));
        }
        Internal.Logger.LogDebug("Selecting model '{Model}' on backend '{BackendId}' (null means all backends)", model, backendId ?? string.Empty);
        JObject _ = await Internal.HttpClient.PostJsonAsync<JObject>("SelectModel",
            new
            {
                model,
                backendId
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ModelOperationUpdate> StreamModelDownloadAsync(string url, string modelType, string name, string? metadata = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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
        Internal.Logger.LogInformation("Starting model download from '{Url}' as '{Name}' of type '{ModelType}'", url, name, modelType);
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
        await foreach (ModelOperationUpdate update in Internal.WebSocketClient.StreamMessagesAsync<ModelOperationUpdate>("DoModelDownloadWS", payload,
            ParseModelOperationMessage, cancellationToken))
        {
            if (update is not null)
            {
                yield return update;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ModelOperationUpdate> StreamModelSelectionAsync(string model, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model cannot be null or empty", nameof(model));
        }
        Internal.Logger.LogInformation("Starting model selection stream for model '{Model}'", model);
        JObject payload = new()
        {
            ["model"] = model
        };
        await foreach (ModelOperationUpdate update in Internal.WebSocketClient.StreamMessagesAsync<ModelOperationUpdate>("SelectModelWS",
            payload, ParseModelOperationMessage, cancellationToken))
        {
            if (update != null)
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
        Internal.Logger.LogDebug("Testing prompt fill for prompt of length {Length}", prompt.Length);
        TestPromptFillResponse response = await Internal.HttpClient.PostJsonAsync<TestPromptFillResponse>("TestPromptFill",
            new
            {
                prompt
            },
            cancellationToken).ConfigureAwait(false);
        return response;
    }

    public ModelOperationUpdate ParseModelOperationMessage(JObject message)
    {
        if (message is null)
        {
            return new ModelOperationUpdate();
        }
        ModelOperationUpdate? update = message.ToObject<ModelOperationUpdate>();
        update ??= new ModelOperationUpdate();
        return update;
    }
}
