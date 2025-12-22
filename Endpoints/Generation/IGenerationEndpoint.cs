using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Common;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.Generation;

/// <summary>Provides access to SwarmUI text-to-image generation endpoints.</summary>
/// <remarks>Handles streaming generation, status queries, and generation control. See CodingGuidelines.md (Generation endpoint section) for streaming flow and parsing details.</remarks>
public interface IGenerationEndpoint
{
    /// <summary>Streams image generation progress and results via WebSocket.</summary>
    /// <param name="request">Generation parameters including prompt, model, size, etc.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Async enumerable of generation updates.</returns>
    IAsyncEnumerable<GenerationUpdate> StreamGenerationAsync(GenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a snapshot of the current SwarmUI server status.</summary>
    /// <param name="includeDebug">Whether to include verbose debugging information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Server status information.</returns>
    Task<ServerStatusResponse> GetCurrentStatusAsync(bool includeDebug = false, CancellationToken cancellationToken = default);

    /// <summary>Interrupts all generations in the current session or optionally all sessions.</summary>
    /// <param name="otherSessions">Whether to interrupt generations in other sessions.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result of the interrupt operation.</returns>
    Task InterruptAllAsync(bool otherSessions = false, CancellationToken cancellationToken = default);

    /// <summary>Gets the available text-to-image parameters and associated metadata.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of available T2I parameters with metadata.</returns>
    Task<T2IParamsResponse> ListT2IParamsAsync(CancellationToken cancellationToken = default);

    /// <summary>Triggers a refresh of server-side parameter and model data.</summary>
    /// <param name="strong">If true, performs a full refresh; otherwise just waits for pending refreshes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated parameters and models.</returns>
    Task<T2IParamsResponse> TriggerRefreshAsync(bool strong = true, CancellationToken cancellationToken = default);

    /// <summary>Sends a debug message to the SwarmUI server logs.</summary>
    /// <param name="message">The message to log on the server.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task ServerDebugMessageAsync(string message, CancellationToken cancellationToken = default);
}
