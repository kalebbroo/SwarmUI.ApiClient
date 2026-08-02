using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Sessions;

/// <summary>Tests for the keyed session pool: single-flight creation, CAS invalidation, failure backoff, per-key isolation.</summary>
public class SessionManagerTests
{
    /// <summary>Counting GetNewSession backend with controllable failure and latency.</summary>
    private sealed class CountingHttpClient : ISwarmHttpClient
    {
        public int Calls;
        public volatile bool Fail;
        public TimeSpan Latency = TimeSpan.Zero;

        public async Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default) where TResponse : class
        {
            int call = Interlocked.Increment(ref Calls);
            if (Latency > TimeSpan.Zero)
            {
                await Task.Delay(Latency, cancellationToken);
            }
            if (Fail)
            {
                throw new SwarmHttpException(System.Net.HttpStatusCode.ServiceUnavailable, "server down");
            }
            JObject response = new()
            {
                ["session_id"] = $"session-{call}",
                ["user_id"] = "local",
                ["version"] = "1.0-test",
                ["server_id"] = "srv-test"
            };
            return (TResponse)(object)response;
        }
    }

    private static SessionManager Create(CountingHttpClient http, TimeSpan? backoff = null, TimeSpan? eviction = null)
    {
        SwarmClientOptions options = new()
        {
            SessionCreateFailureBackoff = backoff ?? TimeSpan.FromMilliseconds(200),
            SessionIdleEviction = eviction
        };
        return new SessionManager(() => http, options);
    }

    [Fact]
    public async Task GetOrCreate_CachesSession()
    {
        CountingHttpClient http = new();
        using SessionManager manager = Create(http);
        string first = await manager.GetOrCreateSessionAsync();
        string second = await manager.GetOrCreateSessionAsync();
        Assert.Equal(first, second);
        Assert.Equal(1, http.Calls);
    }

    [Fact]
    public async Task GetOrCreate_SingleFlight_UnderContention()
    {
        CountingHttpClient http = new() { Latency = TimeSpan.FromMilliseconds(50) };
        using SessionManager manager = Create(http);
        using Barrier barrier = new(100);
        Task<string>[] tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await manager.GetOrCreateSessionAsync();
        })).ToArray();
        string[] results = await Task.WhenAll(tasks);
        Assert.Single(results.Distinct());
        Assert.Equal(1, http.Calls);
    }

    [Fact]
    public async Task InvalidateSession_CAS_NoOpsWhenObservedIdIsStale()
    {
        CountingHttpClient http = new();
        using SessionManager manager = Create(http);
        string first = await manager.GetOrCreateSessionAsync();
        // First invalidation with the current id clears it; a new session gets created.
        manager.InvalidateSession(SwarmSessionKeys.Default, first);
        string second = await manager.GetOrCreateSessionAsync();
        Assert.NotEqual(first, second);
        // Stale invalidations (still quoting the FIRST id) must not discard the replacement.
        for (int i = 0; i < 50; i++)
        {
            manager.InvalidateSession(SwarmSessionKeys.Default, first);
        }
        string third = await manager.GetOrCreateSessionAsync();
        Assert.Equal(second, third);
        Assert.Equal(2, http.Calls);
    }

    [Fact]
    public async Task InvalidationStampede_ConvergesOnOneNewSession()
    {
        CountingHttpClient http = new() { Latency = TimeSpan.FromMilliseconds(20) };
        using SessionManager manager = Create(http);
        string stale = await manager.GetOrCreateSessionAsync();
        using Barrier barrier = new(50);
        Task<string>[] tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            // Every task saw the same stale session fail: invalidate-and-refresh.
            return await manager.RefreshSessionAsync(SwarmSessionKeys.Default, stale);
        })).ToArray();
        string[] results = await Task.WhenAll(tasks);
        Assert.Single(results.Distinct());
        Assert.NotEqual(stale, results[0]);
        // Exactly 2 GetNewSession calls total: the original + one shared replacement.
        Assert.Equal(2, http.Calls);
    }

    [Fact]
    public async Task FailureBackoff_FailsFastWithoutHammeringServer()
    {
        CountingHttpClient http = new() { Fail = true };
        using SessionManager manager = Create(http, backoff: TimeSpan.FromMinutes(5));
        await Assert.ThrowsAsync<SwarmSessionException>(() => manager.GetOrCreateSessionAsync());
        int callsAfterFirst = http.Calls;
        // Subsequent callers inside the backoff window fail fast with the cached error — no new calls.
        for (int i = 0; i < 20; i++)
        {
            await Assert.ThrowsAsync<SwarmSessionException>(() => manager.GetOrCreateSessionAsync());
        }
        Assert.Equal(callsAfterFirst, http.Calls);
    }

    [Fact]
    public async Task FailureBackoff_RecoversAfterWindow()
    {
        CountingHttpClient http = new() { Fail = true };
        using SessionManager manager = Create(http, backoff: TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<SwarmSessionException>(() => manager.GetOrCreateSessionAsync());
        http.Fail = false;
        await Task.Delay(100);
        string session = await manager.GetOrCreateSessionAsync();
        Assert.False(string.IsNullOrEmpty(session));
    }

    [Fact]
    public async Task Keys_AreIsolated()
    {
        CountingHttpClient http = new();
        using SessionManager manager = Create(http);
        string userA = await manager.GetOrCreateSessionAsync("user-a");
        string userB = await manager.GetOrCreateSessionAsync("user-b");
        Assert.NotEqual(userA, userB);
        // Invalidating A must not touch B.
        manager.InvalidateSession("user-a", userA);
        Assert.Null(manager.GetCachedSession("user-a"));
        Assert.Equal(userB, manager.GetCachedSession("user-b")?.SessionId);
    }

    [Fact]
    public async Task GetCachedSession_ExposesServerMetadata()
    {
        CountingHttpClient http = new();
        using SessionManager manager = Create(http);
        await manager.GetOrCreateSessionAsync();
        SwarmSessionInfo? info = manager.GetCachedSession();
        Assert.NotNull(info);
        Assert.Equal("local", info!.UserId);
        Assert.Equal("1.0-test", info.ServerVersion);
        Assert.Equal("srv-test", info.ServerId);
    }

    [Fact]
    public async Task Dispose_MakesFurtherUseThrow()
    {
        CountingHttpClient http = new();
        SessionManager manager = Create(http);
        await manager.GetOrCreateSessionAsync();
        manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetOrCreateSessionAsync());
    }
}
