using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmUI.ApiClient.Http;

/// <summary>Provides HTTP communication with the SwarmUI API. Handles JSON serialization, session injection, error handling, and response deserialization. See CodingGuidelines.md for responsibilities and design rationale.</summary>
/// <remarks>Handles JSON serialization, session injection, error handling, and response deserialization. See CodingGuidelines.md for responsibilities and design rationale.</remarks>
public interface ISwarmHttpClient
{
    /// <summary>Sends a POST request to a SwarmUI API endpoint with optional payload. Automatically injects session_id into the payload.</summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint name (e.g., "ListModels"). Do not include /API/ prefix.</param>
    /// <param name="payload">Optional payload data. Can be Dictionary{string,object}, anonymous object, or null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Deserialized response object.</returns>
    Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>Sends a POST request to a SwarmUI API endpoint with a strongly-typed request. Automatically injects session_id into the request.</summary>
    /// <typeparam name="TRequest">The request model type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint name (e.g., "GenerateText2Image"). Do not include /API/ prefix.</param>
    /// <param name="request">The request object to serialize and send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Deserialized response object.</returns>
    Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class;
}
