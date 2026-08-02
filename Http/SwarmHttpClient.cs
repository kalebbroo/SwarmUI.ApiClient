using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;

namespace SwarmUI.ApiClient.Http;

/// <summary>HTTP communication layer for the SwarmUI API.</summary>
/// <remarks>Each request attempt (session acquisition → POST → single parse → error mapping) executes inside the configured resilience pipeline; on <c>invalid_session_id</c> the session is CAS-invalidated and the retry transparently acquires a fresh one, per the official API docs' mandated pattern.</remarks>
public class SwarmHttpClient : ISwarmHttpClient
{
    /// <summary>Payload keys whose values are masked or summarized before debug logging.</summary>
    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase) { "password", "new_password", "key", "authorization", "webhooksecret", "webhook_secret" };

    /// <summary>Payload keys that carry large base64 blobs; replaced with a byte-count marker in debug logs.</summary>
    private static readonly HashSet<string> BulkKeys = new(StringComparer.OrdinalIgnoreCase) { "image", "initimage", "preview", "maskimage", "audio" };

    private readonly Func<HttpClient> _httpClientProvider;
    private readonly ISessionManager _sessionManager;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<SwarmHttpClient> _logger;

    /// <summary>Creates a new SwarmHttpClient.</summary>
    /// <param name="httpClientProvider">Provides the HttpClient per call. For DI hosts this should call IHttpClientFactory.CreateClient so handler rotation works; standalone clients return a fixed instance.</param>
    /// <param name="sessionManager">Session pool used for session_id injection and invalidation.</param>
    /// <param name="options">Client options used to build the resilience pipeline.</param>
    /// <param name="logger">Optional logger for HTTP diagnostics.</param>
    public SwarmHttpClient(Func<HttpClient> httpClientProvider, ISessionManager sessionManager, SwarmClientOptions options, ILogger<SwarmHttpClient>? logger = null)
    {
        _httpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        ArgumentNullException.ThrowIfNull(options);
        _pipeline = SwarmResiliencePipelines.BuildHttpPipeline(options);
        _logger = logger ?? NullLogger<SwarmHttpClient>.Instance;
    }

    /// <inheritdoc />
    public async Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default) where TResponse : class
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
        }
        // Snapshot the payload once; each retry attempt re-clones from this so attempts never see each other's session_id.
        JObject basePayload = payload switch
        {
            null => [],
            JObject jObject => jObject,
            _ => JObject.FromObject(payload)
        };
        return await _pipeline.ExecuteAsync(
            async (state, ct) => await state.self.ExecuteAttemptAsync<TResponse>(state.endpoint, state.basePayload, state.sessionKey, ct).ConfigureAwait(false),
            (self: this, endpoint, basePayload, sessionKey),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>One complete request attempt: session fetch, POST, single-parse, error mapping, deserialization.</summary>
    private async Task<TResponse> ExecuteAttemptAsync<TResponse>(string endpoint, JObject basePayload, string sessionKey, CancellationToken cancellationToken) where TResponse : class
    {
        JObject payloadJson = (JObject)basePayload.DeepClone();
        bool needsSession = !string.Equals(endpoint, "GetNewSession", StringComparison.OrdinalIgnoreCase);
        string? sessionId = null;
        if (needsSession)
        {
            sessionId = await _sessionManager.GetOrCreateSessionAsync(sessionKey, cancellationToken).ConfigureAwait(false);
            payloadJson["session_id"] = sessionId;
        }
        string payloadString = payloadJson.ToString(Formatting.None);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("POST /API/{Endpoint}: {Payload}", endpoint, RedactForLog(payloadJson));
        }
        using StringContent content = new(payloadString, Encoding.UTF8, "application/json");
        HttpClient httpClient = _httpClientProvider();
        using HttpResponseMessage response = await httpClient.PostAsync($"/API/{endpoint}", content, cancellationToken).ConfigureAwait(false);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        // Parse exactly once; error mapping and result extraction share this JObject.
        JObject? responseJson = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                responseJson = JObject.Parse(responseText);
            }
        }
        catch (JsonException ex)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Non-JSON error body (e.g. HTML 502 from a reverse proxy) — keep the evidence.
                throw new SwarmHttpException(response.StatusCode, $"HTTP request to {endpoint} failed with status {(int)response.StatusCode} {response.ReasonPhrase} and a non-JSON body", Snippet(responseText), ex);
            }
            throw new SwarmException($"Failed to parse response from {endpoint}. Response may be in unexpected format.", ex);
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Response ({StatusCode}) from {Endpoint}: {Response}", (int)response.StatusCode, endpoint, responseJson is null ? Snippet(responseText) : RedactForLog(responseJson));
        }
        MapError(endpoint, response, responseJson, responseText, sessionKey, sessionId);
        if (responseJson is null)
        {
            throw new SwarmException($"API returned an empty response for endpoint {endpoint}");
        }
        if (typeof(TResponse) == typeof(JObject))
        {
            return (TResponse)(object)responseJson;
        }
        try
        {
            TResponse? result = responseJson.ToObject<TResponse>();
            if (result is null)
            {
                throw new SwarmException($"API returned null response for endpoint {endpoint}");
            }
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from {Endpoint}", endpoint);
            throw new SwarmException($"Failed to parse response from {endpoint}. Response may be in unexpected format.", ex);
        }
    }

    /// <summary>Maps SwarmUI error bodies and transport failures to the exception hierarchy. Session rejections CAS-invalidate the pooled session before throwing so the retry gets a fresh one.</summary>
    private void MapError(string endpoint, HttpResponseMessage response, JObject? responseJson, string responseText, string sessionKey, string? sessionId)
    {
        if (responseJson is not null)
        {
            string? errorId = responseJson["error_id"]?.ToString();
            string? errorMessage = responseJson["error"]?.ToString();
            if (!string.IsNullOrEmpty(errorId) || !string.IsNullOrEmpty(errorMessage))
            {
                if (string.Equals(errorId, "invalid_session_id", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Session rejected by server for key '{Key}': {Message}", sessionKey, errorMessage);
                    _sessionManager.InvalidateSession(sessionKey, sessionId);
                    throw new SwarmSessionException(errorMessage ?? "Session ID is invalid or expired.");
                }
                string message = errorMessage ?? $"API error: {errorId}";
                _logger.LogError("SwarmUI API error from {Endpoint}: {ErrorId} - {Message}", endpoint, errorId, message);
                throw new SwarmException(message, errorId);
            }
        }
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("HTTP error from {Endpoint}: {StatusCode} {ReasonPhrase}", endpoint, (int)response.StatusCode, response.ReasonPhrase);
            throw new SwarmHttpException(response.StatusCode, $"HTTP request to {endpoint} failed with status {(int)response.StatusCode} {response.ReasonPhrase}", Snippet(responseText));
        }
    }

    /// <summary>Produces a redacted, size-bounded rendering of a payload for debug logs: secrets masked, session ids truncated, bulk base64 fields replaced by byte counts.</summary>
    internal static string RedactForLog(JObject payload)
    {
        JObject clone = (JObject)payload.DeepClone();
        RedactInPlace(clone);
        string text = clone.ToString(Formatting.None);
        return text.Length > 2000 ? text[..2000] + "..." : text;
    }

    private static void RedactInPlace(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (JProperty property in obj.Properties())
            {
                if (SecretKeys.Contains(property.Name))
                {
                    property.Value = "***";
                }
                else if (property.Name.Equals("session_id", StringComparison.OrdinalIgnoreCase) && property.Value.Type == JTokenType.String)
                {
                    string id = property.Value.ToString();
                    property.Value = id.Length > 8 ? id[..8] + "..." : id;
                }
                else if (BulkKeys.Contains(property.Name) && property.Value.Type == JTokenType.String && property.Value.ToString().Length > 256)
                {
                    property.Value = $"<{property.Value.ToString().Length} chars>";
                }
                else
                {
                    RedactInPlace(property.Value);
                }
            }
        }
        else if (token is JArray array)
        {
            foreach (JToken item in array)
            {
                RedactInPlace(item);
            }
        }
    }

    private static string Snippet(string text) => string.IsNullOrEmpty(text) ? "" : text.Length > 500 ? text[..500] + "..." : text;
}
