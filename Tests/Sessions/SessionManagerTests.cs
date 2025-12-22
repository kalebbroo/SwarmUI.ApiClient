using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Sessions
{
    /// <summary>Unit tests for <see cref="SessionManager"/> verifying caching, invalidation, and concurrency.</summary>
    public class SessionManagerTests
    {
        /// <summary>Test HTTP client that returns queued session IDs for the GetNewSession endpoint and tracks call counts.</summary>
        private sealed class FakeSwarmHttpClient : ISwarmHttpClient
        {
            private readonly System.Collections.Generic.Queue<string> _sessionIds = new System.Collections.Generic.Queue<string>();

            public int GetNewSessionCallCount { get; private set; }

            public FakeSwarmHttpClient(params string[] sessionIds)
            {
                if (sessionIds == null || sessionIds.Length == 0)
                {
                    _sessionIds.Enqueue("session-1");
                }
                else
                {
                    foreach (string sessionId in sessionIds)
                    {
                        _sessionIds.Enqueue(sessionId);
                    }
                }
            }

            public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
            {
                if (!string.Equals(endpoint, "GetNewSession", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException("FakeSwarmHttpClient only supports GetNewSession endpoint.");
                }

                GetNewSessionCallCount++;

                string sessionId = _sessionIds.Count > 0 ? _sessionIds.Dequeue() : "session-last";

                if (typeof(TResponse) == typeof(JObject))
                {
                    JObject response = new JObject
                    {
                        ["session_id"] = sessionId
                    };

                    return Task.FromResult((TResponse)(object)response);
                }

                throw new NotSupportedException("Unsupported response type for FakeSwarmHttpClient.");
            }

            public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            {
                throw new NotSupportedException("FakeSwarmHttpClient does not support typed requests.");
            }
        }

        [Fact]
        public async Task GetOrCreateSessionAsync_CachesSession()
        {
            FakeSwarmHttpClient httpClient = new FakeSwarmHttpClient("session-1");
            SessionManager manager = new SessionManager(() => httpClient);

            string first = await manager.GetOrCreateSessionAsync(CancellationToken.None).ConfigureAwait(false);
            string second = await manager.GetOrCreateSessionAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("session-1", first);
            Assert.Equal("session-1", second);
            Assert.Equal(1, httpClient.GetNewSessionCallCount);
            Assert.Equal("session-1", manager.CurrentSessionId);
        }

        [Fact]
        public async Task InvalidateSession_ForcesNewSessionOnNextCall()
        {
            FakeSwarmHttpClient httpClient = new FakeSwarmHttpClient("session-1", "session-2");
            SessionManager manager = new SessionManager(() => httpClient);

            string first = await manager.GetOrCreateSessionAsync(CancellationToken.None).ConfigureAwait(false);
            manager.InvalidateSession();
            string second = await manager.GetOrCreateSessionAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("session-1", first);
            Assert.Equal("session-2", second);
            Assert.Equal(2, httpClient.GetNewSessionCallCount);
            Assert.Equal("session-2", manager.CurrentSessionId);
        }

        [Fact]
        public async Task RefreshSessionAsync_AlwaysCreatesNewSession()
        {
            FakeSwarmHttpClient httpClient = new FakeSwarmHttpClient("session-1", "session-2");
            SessionManager manager = new SessionManager(() => httpClient);

            string first = await manager.GetOrCreateSessionAsync(CancellationToken.None).ConfigureAwait(false);
            string refreshed = await manager.RefreshSessionAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("session-1", first);
            Assert.Equal("session-2", refreshed);
            Assert.Equal(2, httpClient.GetNewSessionCallCount);
            Assert.Equal("session-2", manager.CurrentSessionId);
        }

        [Fact]
        public async Task GetOrCreateSessionAsync_IsThreadSafe()
        {
            FakeSwarmHttpClient httpClient = new FakeSwarmHttpClient("session-1");
            SessionManager manager = new SessionManager(() => httpClient);

            Task<string>[] tasks = new Task<string>[10];
            for (int index = 0; index < tasks.Length; index++)
            {
                tasks[index] = manager.GetOrCreateSessionAsync(CancellationToken.None);
            }

            string[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (string sessionId in results)
            {
                Assert.Equal("session-1", sessionId);
            }

            Assert.Equal(1, httpClient.GetNewSessionCallCount);
        }
    }
}
