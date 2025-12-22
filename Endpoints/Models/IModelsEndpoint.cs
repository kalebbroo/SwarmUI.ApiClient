using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.Models;

/// <summary>Provides access to SwarmUI model management endpoints.</summary>
/// <remarks>Covers listing, describing, editing, deleting, downloading, and selecting models, LoRAs, and wildcards. See ModelsAPI.md for the full endpoint reference.</remarks>
public interface IModelsEndpoint
{
    // NOTE: This interface is intended to track the ModelsAPI endpoints from the SwarmUI documentation.
    // When new endpoints are added to SwarmUI, consider extending this interface to keep feature parity.
    // Each method should have XML docs, proper async signature, and CancellationToken support.

    /// <summary>Lists available models from the SwarmUI server with filtering and sorting options.</summary>
    /// <param name="modelType">Logical model subtype to retrieve, corresponding to SwarmUI's <c>subtype</c> field.</param>
    /// <param name="path">Relative path within the models directory to list.</param>
    /// <param name="depth">Maximum folder depth to search below the specified path.</param>
    /// <param name="sortBy">Field to sort by.</param>
    /// <param name="allowRemote">Whether to include models that are not yet present on the local filesystem.</param>
    /// <param name="sortReverse">When true, reverses the server-side sort order.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="ModelListResponse"/> containing folder names and detailed model entries at the requested path.</returns>
    Task<ModelListResponse> ListModelsAsync(string modelType = "Stable-Diffusion", string path = "", int depth = 4, string sortBy = "Name",
        bool allowRemote = true, bool sortReverse = false, CancellationToken cancellationToken = default);

    /// <summary>Gets a detailed description for a specific model using SwarmUI's <c>DescribeModel</c> API.</summary>
    /// <param name="modelName">The model identifier to describe.</param>
    /// <param name="modelType">Logical model subtype, mapped to SwarmUI's <c>subtype</c> field.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="ModelDescription"/> instance containing detailed information about the requested model.</returns>
    Task<ModelDescription> DescribeModelAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default);

    /// <summary>Deletes a model from storage using the <c>DeleteModel</c> API.</summary>
    /// <param name="modelName">Full filepath name of the model being deleted.</param>
    /// <param name="modelType">Model sub-type to delete from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task DeleteModelAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default);

    /// <summary>Deletes a wildcard file using the <c>DeleteWildcard</c> API.</summary>
    /// <param name="card">Exact filepath name of the wildcard card to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task DeleteWildcardAsync(string card, CancellationToken cancellationToken = default);

    /// <summary>Gets or computes the tensor hash for a specific model using the <c>GetModelHash</c> API.</summary>
    /// <param name="modelName">Full filepath name of the model to hash.</param>
    /// <param name="modelType">Model sub-type to hash.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Hash information for the requested model.</returns>
    Task<ModelHashResponse> GetModelHashAsync(string modelName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default);

    /// <summary>Modifies the metadata of a model using the <c>EditModelMetadata</c> API.</summary>
    /// <param name="request">Metadata edit request describing all new values.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task EditModelMetadataAsync(EditModelMetadataRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edits a wildcard file and optional preview using the <c>EditWildcard</c> API.</summary>
    /// <param name="request">Wildcard edit request describing card name, options, and preview data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task EditWildcardAsync(EditWildcardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Forwards a metadata request (for example to CivitAI) using the <c>ForwardMetadataRequest</c> API.</summary>
    /// <param name="url">Target URL to forward the request to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Raw JSON response from the forwarded metadata request.</returns>
    Task<Newtonsoft.Json.Linq.JObject> ForwardMetadataRequestAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Lists models that are currently loaded on at least one backend using the <c>ListLoadedModels</c> API.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of currently loaded models.</returns>
    Task<LoadedModelsResponse> ListLoadedModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Renames a model file within the model directory using the <c>RenameModel</c> API.</summary>
    /// <param name="oldName">Existing full filepath name of the model.</param>
    /// <param name="newName">New full filepath name for the model.</param>
    /// <param name="modelType">Model sub-type to operate on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task RenameModelAsync(string oldName, string newName, string modelType = "Stable-Diffusion", CancellationToken cancellationToken = default);

    /// <summary>Loads a model on one or more backends using the HTTP <c>SelectModel</c> API.</summary>
    /// <param name="model">Full filepath of the model to load.</param>
    /// <param name="backendId">Optional backend ID to load the model on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task SelectModelAsync(string model, string? backendId = null, CancellationToken cancellationToken = default);

    /// <summary>Streams live status updates while downloading a model using the <c>DoModelDownloadWS</c> WebSocket API.</summary>
    /// <param name="url">The URL to download the model from.</param>
    /// <param name="modelType">Model sub-type.</param>
    /// <param name="name">Filename to use for the downloaded model.</param>
    /// <param name="metadata">Optional raw JSON metadata text to inject into the model.</param>
    /// <param name="cancellationToken">Cancellation token that cancels the streaming operation.</param>
    /// <returns>Async stream of <see cref="ModelOperationUpdate"/> messages describing progress and status.</returns>
    IAsyncEnumerable<ModelOperationUpdate> StreamModelDownloadAsync(string url, string modelType, string name, string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>Streams live status updates while loading a model using the <c>SelectModelWS</c> WebSocket API.</summary>
    /// <param name="model">The full filepath of the model to load.</param>
    /// <param name="cancellationToken">Cancellation token that cancels the streaming operation.</param>
    /// <returns>Async stream of <see cref="ModelOperationUpdate"/> messages describing progress and status.</returns>
    IAsyncEnumerable<ModelOperationUpdate> StreamModelSelectionAsync(string model, CancellationToken cancellationToken = default);

    /// <summary>Tests how a prompt will be filled (for example, expanding wildcards and random segments) using the <c>TestPromptFill</c> API.</summary>
    /// <param name="prompt">Prompt text to fill.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Resulting filled prompt text.</returns>
    Task<TestPromptFillResponse> TestPromptFillAsync(string prompt, CancellationToken cancellationToken = default);
}