using System;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Http;

/// <summary>Provides HTTP communication with the SwarmUI API: JSON serialization, session injection, retry, and error mapping.</summary>
/// <remarks>SwarmUI handler-level errors arrive as HTTP 200 with an <c>error</c>/<c>error_id</c> body; implementations parse every body and map errors from it, never from the status code alone. Session expiry (<c>error_id="invalid_session_id"</c>) is invalidated and retried transparently.</remarks>
public interface ISwarmHttpClient
{
    /// <summary>Sends a POST request to a SwarmUI API endpoint with an optional payload. Injects the session id for <paramref name="sessionKey"/> into the payload (except for GetNewSession).</summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint name (e.g., "ListModels"). Do not include the /API/ prefix.</param>
    /// <param name="payload">Optional payload data. Can be a JObject, Dictionary{string,object}, anonymous object, or null. The caller's object is never mutated.</param>
    /// <param name="sessionKey">Which pooled session to authenticate the call with.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Deserialized response object.</returns>
    /// <exception cref="Exceptions.SwarmSessionException">Session was rejected and could not be refreshed within the retry budget.</exception>
    /// <exception cref="Exceptions.SwarmHttpException">Transport-level failure (non-success status with no SwarmUI error body).</exception>
    /// <exception cref="Exceptions.SwarmException">SwarmUI handler error (error/error_id body).</exception>
    Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default) where TResponse : class;
}
