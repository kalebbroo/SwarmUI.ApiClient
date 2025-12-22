using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Http
{
    /// <summary>Unit tests for <see cref="SwarmHttpClient"/> verifying session injection, retry behavior, and error handling.</summary>
    public class SwarmHttpClientTests
    {
        /// <summary>Test HTTP handler that records outgoing requests and allows configuring responses for retry scenarios.</summary>
        private sealed class RecordingHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            public string? LastContent { get; private set; }

            public int CallCount { get; private set; }

            public HttpResponseMessage ResponseToReturn { get; set; } = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            public HttpResponseMessage? SecondResponseToReturn { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                CallCount++;

                if (request.Content != null)
                {
                    LastContent = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    LastContent = null;
                }

                if (CallCount == 2 && SecondResponseToReturn != null)
                {
                    return SecondResponseToReturn;
                }

                return ResponseToReturn;
            }
        }

        /// <summary>Test implementation of <see cref="ISessionManager"/> that tracks calls and returns a fixed session ID.</summary>
        private sealed class TestSessionManager : ISessionManager
        {
            public int GetOrCreateSessionCallCount { get; private set; }

            public int InvalidateSessionCallCount { get; private set; }

            public string SessionIdToReturn { get; set; } = "session-123";

            public Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default)
            {
                GetOrCreateSessionCallCount++;
                return Task.FromResult(SessionIdToReturn);
            }

            public Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default)
            {
                GetOrCreateSessionCallCount++;
                return Task.FromResult(SessionIdToReturn);
            }

            public void InvalidateSession()
            {
                InvalidateSessionCallCount++;
            }

            public string? CurrentSessionId => SessionIdToReturn;
        }

        private sealed class SimpleResponse
        {
            public string? Value { get; set; }
        }

        [Fact]
        public async Task PostJsonAsync_AddsSessionId_ForNonGetNewSession()
        {
            RecordingHandler handler = new RecordingHandler();
            HttpClient httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("http://localhost");

            TestSessionManager sessionManager = new TestSessionManager();
            SwarmHttpClient client = new SwarmHttpClient(httpClient, sessionManager);

            SimpleResponse response = await client.PostJsonAsync<SimpleResponse>(
                "SomeEndpoint",
                new
                {
                    foo = "bar"
                },
                CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, sessionManager.GetOrCreateSessionCallCount);
            Assert.NotNull(handler.LastContent);
            Assert.Contains("\"session_id\":\"session-123\"", handler.LastContent!, StringComparison.Ordinal);
            Assert.NotNull(response);
        }

        [Fact]
        public async Task PostJsonAsync_DoesNotInjectSession_ForGetNewSession()
        {
            RecordingHandler handler = new RecordingHandler();
            handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"session_id\":\"abc\"}", Encoding.UTF8, "application/json")
            };

            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            TestSessionManager sessionManager = new TestSessionManager();
            SwarmHttpClient client = new SwarmHttpClient(httpClient, sessionManager);

            JObject result = await client.PostJsonAsync<JObject>(
                "GetNewSession",
                payload: null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(0, sessionManager.GetOrCreateSessionCallCount);
            Assert.NotNull(handler.LastContent);
            Assert.DoesNotContain("session_id", handler.LastContent!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("abc", result["session_id"]?.ToString());
        }

        [Fact]
        public async Task PostJsonAsync_InvalidSession_RetriesAndSucceeds()
        {
            RecordingHandler handler = new RecordingHandler();
            handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"error_id\":\"invalid_session_id\",\"error\":\"Session expired\"}", Encoding.UTF8, "application/json")
            };
            handler.SecondResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":\"success\"}", Encoding.UTF8, "application/json")
            };

            HttpClient httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("http://localhost");

            TestSessionManager sessionManager = new TestSessionManager();
            SwarmHttpClient client = new SwarmHttpClient(httpClient, sessionManager);

            SimpleResponse result = await client.PostJsonAsync<SimpleResponse>(
                "SomeEndpoint",
                new { },
                CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal("success", result.Value);
            Assert.Equal(2, handler.CallCount);
            Assert.Equal(1, sessionManager.InvalidateSessionCallCount);
        }

        [Fact]
        public async Task PostJsonAsync_InvalidSession_RetriesAndStillFails()
        {
            RecordingHandler handler = new RecordingHandler();
            handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"error_id\":\"invalid_session_id\",\"error\":\"Session expired\"}", Encoding.UTF8, "application/json")
            };

            HttpClient httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("http://localhost");

            TestSessionManager sessionManager = new TestSessionManager();
            SwarmHttpClient client = new SwarmHttpClient(httpClient, sessionManager);

            await Assert.ThrowsAsync<SwarmSessionException>(async () =>
            {
                await client.PostJsonAsync<SimpleResponse>(
                    "SomeEndpoint",
                    new { },
                    CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.Equal(2, handler.CallCount);
            Assert.Equal(2, sessionManager.InvalidateSessionCallCount);
        }

        [Fact]
        public async Task PostJsonAsync_GenericError_ThrowsSwarmException_WithErrorId()
        {
            RecordingHandler handler = new RecordingHandler();
            handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"error_id\":\"some_error\",\"error\":\"Bad thing\"}", Encoding.UTF8, "application/json")
            };

            HttpClient httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("http://localhost");

            TestSessionManager sessionManager = new TestSessionManager();
            SwarmHttpClient client = new SwarmHttpClient(httpClient, sessionManager);

            SwarmException exception = await Assert.ThrowsAsync<SwarmException>(async () =>
            {
                await client.PostJsonAsync<SimpleResponse>(
                    "SomeEndpoint",
                    new
                    {
                    },
                    CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.Equal("some_error", exception.ErrorId);
        }
    }
}
