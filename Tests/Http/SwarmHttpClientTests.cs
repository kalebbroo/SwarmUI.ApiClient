using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Http;

/// <summary>Tests for the HTTP layer: session injection, retry budget, error mapping precedence, payload immutability, log redaction.</summary>
public class SwarmHttpClientTests
{
    /// <summary>Scripted message handler: pops one canned response per request and records everything sent.</summary>
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> Responses = new();
        public readonly List<string> SentBodies = [];
        public int RequestCount;

        public void EnqueueJson(string json, HttpStatusCode status = HttpStatusCode.OK)
            => Responses.Enqueue(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

        public void EnqueueRaw(string body, HttpStatusCode status, string mediaType = "text/html")
            => Responses.Enqueue(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) });

        public void EnqueueThrow(Exception exception) => Responses.Enqueue(_ => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            SentBodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            if (Responses.Count == 0)
            {
                throw new InvalidOperationException("Test made more requests than were scripted");
            }
            return Responses.Dequeue()(request);
        }
    }

    private static (SwarmHttpClient Client, ScriptedHandler Handler, FakeSessionManager Sessions) Create(Action<SwarmClientOptions>? configure = null)
    {
        ScriptedHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:7801") };
        FakeSessionManager sessions = new();
        SwarmClientOptions options = new() { MaxRetryAttempts = 3, RetryBaseDelay = TimeSpan.FromMilliseconds(1) };
        configure?.Invoke(options);
        return (new SwarmHttpClient(() => httpClient, sessions, options), handler, sessions);
    }

    [Fact]
    public async Task PostJson_InjectsSessionId_ForConfiguredKey()
    {
        (SwarmHttpClient client, ScriptedHandler handler, FakeSessionManager sessions) = Create();
        handler.EnqueueJson("""{"ok":true}""");
        await client.PostJsonAsync<JObject>("GetCurrentStatus", new JObject(), "user-3");
        JObject sent = JObject.Parse(handler.SentBodies[0]);
        Assert.Equal(await sessions.GetOrCreateSessionAsync("user-3"), sent["session_id"]!.ToString());
    }

    [Fact]
    public async Task PostJson_GetNewSession_SkipsSessionInjection()
    {
        (SwarmHttpClient client, ScriptedHandler handler, FakeSessionManager sessions) = Create();
        handler.EnqueueJson("""{"session_id":"abc"}""");
        await client.PostJsonAsync<JObject>("GetNewSession");
        Assert.False(JObject.Parse(handler.SentBodies[0]).ContainsKey("session_id"));
        Assert.Equal(0, sessions.CreateCount);
    }

    [Fact]
    public async Task PostJson_DoesNotMutateCallerPayload()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create();
        handler.EnqueueJson("""{"ok":true}""");
        JObject payload = new() { ["prompt"] = "cat" };
        await client.PostJsonAsync<JObject>("GetCurrentStatus", payload);
        Assert.False(payload.ContainsKey("session_id"));
    }

    [Fact]
    public async Task PostJson_InvalidSession_InvalidatesWithObservedId_AndRetriesWithFreshSession()
    {
        (SwarmHttpClient client, ScriptedHandler handler, FakeSessionManager sessions) = Create();
        handler.EnqueueJson("""{"error":"Session expired","error_id":"invalid_session_id"}""");
        handler.EnqueueJson("""{"ok":true}""");
        JObject result = await client.PostJsonAsync<JObject>("GetCurrentStatus", new JObject(), "user-1");
        Assert.True(result["ok"]!.Value<bool>());
        Assert.Equal(2, handler.RequestCount);
        (string key, string? observed) = Assert.Single(sessions.Invalidations);
        Assert.Equal("user-1", key);
        Assert.NotNull(observed);
        // The retry must carry a DIFFERENT session id.
        string first = JObject.Parse(handler.SentBodies[0])["session_id"]!.ToString();
        string second = JObject.Parse(handler.SentBodies[1])["session_id"]!.ToString();
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task PostJson_RetryBudget_HonorsMaxRetryAttempts()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create(o => o.MaxRetryAttempts = 3);
        for (int i = 0; i < 4; i++)
        {
            handler.EnqueueJson("""{"error":"Session expired","error_id":"invalid_session_id"}""");
        }
        await Assert.ThrowsAsync<SwarmSessionException>(() => client.PostJsonAsync<JObject>("GetCurrentStatus"));
        // 1 initial + 3 retries = 4 — the old code hardcoded a single retry.
        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task PostJson_TransientHttpErrors_RetriedWhenEnabled_NotWhenDisabled()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create(o => o.MaxRetryAttempts = 2);
        handler.EnqueueRaw("bad gateway", HttpStatusCode.BadGateway);
        handler.EnqueueThrow(new HttpRequestException("connection reset"));
        handler.EnqueueJson("""{"ok":true}""");
        JObject result = await client.PostJsonAsync<JObject>("GetCurrentStatus");
        Assert.True(result["ok"]!.Value<bool>());
        Assert.Equal(3, handler.RequestCount);

        (SwarmHttpClient noRetryClient, ScriptedHandler noRetryHandler, _) = Create(o => o.RetryTransientHttpErrors = false);
        noRetryHandler.EnqueueRaw("bad gateway", HttpStatusCode.BadGateway);
        await Assert.ThrowsAsync<SwarmHttpException>(() => noRetryClient.PostJsonAsync<JObject>("GetCurrentStatus"));
        Assert.Equal(1, noRetryHandler.RequestCount);
    }

    [Fact]
    public async Task PostJson_HandlerError_HTTP200_IsNotRetried()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create();
        handler.EnqueueJson("""{"error":"Model not found"}""");
        SwarmException ex = await Assert.ThrowsAsync<SwarmException>(() => client.PostJsonAsync<JObject>("GetCurrentStatus"));
        Assert.Equal("Model not found", ex.Message);
        // Deterministic server-side errors must never burn retry budget.
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task PostJson_ErrorBodyPrecedence_JsonErrorWinsOverStatusCode()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create(o => o.RetryTransientHttpErrors = false);
        handler.EnqueueJson("""{"error":"backend exploded","error_id":"some_error"}""", HttpStatusCode.InternalServerError);
        SwarmException ex = await Assert.ThrowsAsync<SwarmException>(() => client.PostJsonAsync<JObject>("GetCurrentStatus"));
        Assert.Equal("backend exploded", ex.Message);
        Assert.Equal("some_error", ex.ErrorId);
    }

    [Fact]
    public async Task PostJson_NonJsonErrorBody_SurfacesAsHttpExceptionWithSnippet()
    {
        (SwarmHttpClient client, ScriptedHandler handler, _) = Create(o => o.RetryTransientHttpErrors = false);
        handler.EnqueueRaw("<html><body>502 Bad Gateway (cloudflare)</body></html>", HttpStatusCode.BadGateway);
        SwarmHttpException ex = await Assert.ThrowsAsync<SwarmHttpException>(() => client.PostJsonAsync<JObject>("GetCurrentStatus"));
        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Contains("cloudflare", ex.BodySnippet);
    }

    [Fact]
    public void RedactForLog_MasksSecretsAndTruncatesSessionIds()
    {
        JObject payload = new()
        {
            ["password"] = "hunter2",
            ["key"] = "sk-secret-key",
            ["session_id"] = "0123456789abcdef0123456789abcdef",
            ["initimage"] = new string('x', 5000),
            ["prompt"] = "a cat",
            ["nested"] = new JObject { ["new_password"] = "hunter3" }
        };
        string log = SwarmHttpClient.RedactForLog(payload);
        Assert.DoesNotContain("hunter2", log);
        Assert.DoesNotContain("hunter3", log);
        Assert.DoesNotContain("sk-secret-key", log);
        Assert.DoesNotContain("0123456789abcdef0123456789abcdef", log);
        Assert.Contains("01234567...", log);
        Assert.Contains("<5000 chars>", log);
        Assert.Contains("a cat", log);
        // The original payload must be untouched.
        Assert.Equal("hunter2", payload["password"]!.ToString());
    }
}
