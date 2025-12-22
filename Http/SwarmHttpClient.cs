using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Http;

/// <summary>Provides HTTP communication with the SwarmUI API.</summary>
/// <remarks>Handles JSON serialization, session injection, basic retry, and error mapping. See CodingGuidelines.md (HTTP section) for details.</remarks>
public class SwarmHttpClient : ISwarmHttpClient
{
    public struct Impl
    {
        /// <summary> The HttpClient instance used for making requests.</summary>
        public HttpClient HttpClient;

        /// <summary>Session manager used to obtain and invalidate session IDs.</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for debugging HTTP requests and responses.</summary>
        public ILogger<SwarmHttpClient> Logger;
    }

    /// <summary>Internal implementation data.</summary>
    public Impl Internal;

    /// <summary>Creates a new SwarmHttpClient instance.</summary>
    /// <param name="httpClient">Configured HttpClient instance.</param>
    /// <param name="sessionManager">Session manager used for session_id injection.</param>
    /// <param name="logger">Optional logger for HTTP diagnostics.</param>
    public SwarmHttpClient(HttpClient httpClient, ISessionManager? sessionManager, ILogger<SwarmHttpClient>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.SessionManager = sessionManager!;
        Internal.Logger = logger ?? NullLogger<SwarmHttpClient>.Instance;
    }

    /// <summary>Sends a POST request to a SwarmUI API endpoint with an optional JSON payload.</summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">API endpoint name without the /API/ prefix.</param>
    /// <param name="payload">Optional payload; session_id is injected automatically unless the endpoint is "GetNewSession".</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP request.</param>
    /// <returns>Deserialized response object.</returns>
    /// <exception cref="SwarmSessionException">Thrown when the server returns error_id="invalid_session_id" after retry.</exception>
    /// <exception cref="SwarmException">Thrown for other API or HTTP errors.</exception>
    public async Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
    {
        try
        {
            return await PostJsonAsyncCore<TResponse>(endpoint, payload, cancellationToken).ConfigureAwait(false);
        }
        catch (SwarmSessionException)
        {
            Internal.Logger.LogInformation("Retrying request to {Endpoint} with fresh session after session expiration", endpoint);
            try
            {
                return await PostJsonAsyncCore<TResponse>(endpoint, payload, cancellationToken).ConfigureAwait(false);
            }
            catch (SwarmSessionException retryEx)
            {
                Internal.Logger.LogError("Retry failed for {Endpoint}: {Message}", endpoint, retryEx.Message);
                throw;
            }
        }
    }

    /// <summary>Core implementation of PostJsonAsync without retry logic.</summary>
    private async Task<TResponse> PostJsonAsyncCore<TResponse>(string endpoint, object? payload, CancellationToken cancellationToken) where TResponse : class
    {
        JObject payloadJson;
        if (payload is null)
        {
            payloadJson = [];
        }
        else if (payload is JObject existingJObject)
        {
            payloadJson = existingJObject;
        }
        else
        {
            payloadJson = JObject.FromObject(payload);
        }
        bool needsSession = !string.Equals(endpoint, "GetNewSession", StringComparison.OrdinalIgnoreCase);
        if (needsSession)
        {
            string sessionId = await Internal.SessionManager.GetOrCreateSessionAsync(cancellationToken).ConfigureAwait(false);
            payloadJson["session_id"] = sessionId;
        }
        string payloadString = payloadJson.ToString(Formatting.None);
        if (payloadString.Length > 500)
        {
            Internal.Logger.LogDebug("POST /API/{Endpoint}: {Payload}...", endpoint, payloadString.Substring(0, 497) + "...");
        }
        else
        {
            Internal.Logger.LogDebug("POST /API/{Endpoint}: {Payload}", endpoint, payloadString);
        }
        StringContent content = new(payloadString, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Internal.HttpClient.PostAsync($"/API/{endpoint}", content, cancellationToken).ConfigureAwait(false);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (responseText.Length > 1000)
        {
            Internal.Logger.LogDebug("Response ({StatusCode}): {Response}...", response.StatusCode, responseText.Substring(0, 997) + "...");
        }
        else
        {
            Internal.Logger.LogDebug("Response ({StatusCode}): {Response}", response.StatusCode, responseText);
        }
        await HandleErrorResponseAsync(response, responseText).ConfigureAwait(false);
        try
        {
            TResponse? result = JsonConvert.DeserializeObject<TResponse>(responseText);
            if (result == null)
            {
                throw new SwarmException($"API returned null response for endpoint {endpoint}");
            }
            return result;
        }
        catch (JsonException ex)
        {
            Internal.Logger.LogError(ex, "Failed to deserialize response from {Endpoint}", endpoint);
            throw new SwarmException($"Failed to parse response from {endpoint}. Response may be in unexpected format.", ex);
        }
    }

    /// <summary>Sends a POST request to a SwarmUI API endpoint with a strongly typed request object.</summary>
    /// <typeparam name="TRequest">The request model type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">API endpoint name without the /API/ prefix.</param>
    /// <param name="request">The request object to serialize and send.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP request.</param>
    /// <returns>Deserialized response object.</returns>
    /// <exception cref="SwarmSessionException">Thrown when the server returns error_id="invalid_session_id".</exception>
    /// <exception cref="SwarmException">Thrown for other API or HTTP errors.</exception>
    public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        JObject requestJson = JObject.FromObject(request);
        return await PostJsonAsync<TResponse>(endpoint, requestJson, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Handles error responses from the SwarmUI API by parsing error structures and throwing appropriate exception types.</summary>
    /// <param name="response">The HTTP response message to check for errors.</param>
    /// <param name="responseText">The response body text (already read from response.Content).</param>
    /// <exception cref="SwarmSessionException">
    /// Thrown when error_id="invalid_session_id". Session is invalidated automatically.
    /// </exception>
    /// <exception cref="SwarmException">
    /// Thrown for all other error conditions with appropriate error_id and message.
    /// </exception>
    /// <remarks>See CodingGuidelines.md (HTTP section) for error-handling scenarios.</remarks>
    private async Task HandleErrorResponseAsync(HttpResponseMessage response, string responseText)
    {
        JObject? responseJson = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                responseJson = JObject.Parse(responseText);
            }
        }
        catch (JsonException)
        {
        }
        if (responseJson is not null)
        {
            string? errorId = responseJson["error_id"]?.ToString();
            string? errorMessage = responseJson["error"]?.ToString();
            if (!string.IsNullOrEmpty(errorId) || !string.IsNullOrEmpty(errorMessage))
            {
                if (string.Equals(errorId, "invalid_session_id", StringComparison.OrdinalIgnoreCase))
                {
                    Internal.Logger.LogWarning("Session invalidated by server: {Message}", errorMessage);
                    Internal.SessionManager.InvalidateSession();
                    throw new SwarmSessionException(errorMessage ?? "Session ID is invalid or expired. A new session will be created on the next request.");
                }
                string message = errorMessage ?? $"API error: {errorId}";
                Internal.Logger.LogError("SwarmUI API error: {ErrorId} - {Message}", errorId, message);
                throw new SwarmException(message, errorId);
            }
        }
        if (!response.IsSuccessStatusCode)
        {
            string message = $"HTTP request failed with status {(int)response.StatusCode} {response.ReasonPhrase}";
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                message += $": {responseText}";
            }
            Internal.Logger.LogError("HTTP error: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new SwarmException(message);
        }
    }
}
