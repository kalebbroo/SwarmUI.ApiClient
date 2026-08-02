using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Sessions;

/// <summary>Keyed SwarmUI session pool with single-flight creation, compare-and-swap invalidation, and failure backoff.</summary>
/// <remarks>Each key holds one immutable unit of state: a <c>Task&lt;SwarmSessionInfo&gt;</c>. Concurrent callers of one key await the same creation task; invalidation only clears the state when the failing session id still matches, so a wave of failures for one stale session converges on exactly one GetNewSession call.</remarks>
public class SessionManager : ISessionManager, IDisposable
{
    /// <summary>Per-key session slot. The unit of published state is the single <see cref="_current"/> task reference — never a multi-field pair — so readers can never observe a torn or half-updated session.</summary>
    private sealed class SessionSlot
    {
        /// <summary>Guards swaps of <see cref="_current"/>. Never held across I/O.</summary>
        public readonly object Gate = new();

        /// <summary>The current session state: an in-flight, completed, or faulted creation task; null when invalidated or never created.</summary>
        private Task<SwarmSessionInfo>? _current;

        /// <summary>UTC ticks of the most recent failed creation, for failure backoff.</summary>
        public long LastFailureUtcTicks;

        /// <summary>UTC ticks of the last access, for optional idle eviction.</summary>
        public long LastAccessTicks;

        public Task<SwarmSessionInfo>? Current => Volatile.Read(ref _current);

        /// <summary>Sets the current task. Callers must hold <see cref="Gate"/>.</summary>
        public void SetCurrent(Task<SwarmSessionInfo>? value) => Volatile.Write(ref _current, value);

        public void Touch() => Volatile.Write(ref LastAccessTicks, DateTimeOffset.UtcNow.UtcTicks);
    }

    private readonly ConcurrentDictionary<string, SessionSlot> _slots = new();
    private readonly Func<ISwarmHttpClient> _httpClientFactory;
    private readonly SwarmClientOptions _options;
    private readonly ILogger<SessionManager> _logger;
    private ISwarmHttpClient? _httpClientCache;
    private long _lastEvictionSweepTicks;
    private volatile bool _disposed;

    /// <summary>Creates a new SessionManager.</summary>
    /// <param name="httpClientFactory">Factory used to obtain the HTTP client for GetNewSession calls (deferred to break the circular dependency with SwarmHttpClient).</param>
    /// <param name="options">Client options (failure backoff, idle eviction).</param>
    /// <param name="logger">Optional logger for session lifecycle events.</param>
    public SessionManager(Func<ISwarmHttpClient> httpClientFactory, SwarmClientOptions options, ILogger<SessionManager>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<SessionManager>.Instance;
    }

    private ISwarmHttpClient HttpClient => _httpClientCache ??= _httpClientFactory();

    /// <inheritdoc />
    public async Task<string> GetOrCreateSessionAsync(string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default)
    {
        SwarmSessionInfo info = await GetOrCreateSessionInfoAsync(sessionKey, cancellationToken).ConfigureAwait(false);
        return info.SessionId;
    }

    /// <inheritdoc />
    public async Task<string> RefreshSessionAsync(string sessionKey, string? observedSessionId, CancellationToken cancellationToken = default)
    {
        InvalidateSession(sessionKey, observedSessionId);
        return await GetOrCreateSessionAsync(sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void InvalidateSession(string sessionKey, string? observedSessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionKey);
        if (!_slots.TryGetValue(sessionKey, out SessionSlot? slot))
        {
            return;
        }
        lock (slot.Gate)
        {
            Task<SwarmSessionInfo>? current = slot.Current;
            if (current is null)
            {
                return;
            }
            // A still-running creation task is never invalidated: the caller can't have observed its result yet.
            if (!current.IsCompleted)
            {
                return;
            }
            if (current.IsCompletedSuccessfully)
            {
                if (observedSessionId is not null && current.Result.SessionId != observedSessionId)
                {
                    // Someone already refreshed past the session the caller saw fail; keep the newer one.
                    return;
                }
                _logger.LogWarning("Session invalidated for key '{Key}' (session {SessionId}...)", sessionKey, Truncate(current.Result.SessionId));
            }
            slot.SetCurrent(null);
        }
    }

    /// <inheritdoc />
    public SwarmSessionInfo? GetCachedSession(string sessionKey = SwarmSessionKeys.Default)
    {
        ArgumentNullException.ThrowIfNull(sessionKey);
        if (!_slots.TryGetValue(sessionKey, out SessionSlot? slot))
        {
            return null;
        }
        Task<SwarmSessionInfo>? current = slot.Current;
        return current is { IsCompletedSuccessfully: true } ? current.Result : null;
    }

    /// <summary>Gets or creates the full session snapshot for a key. Used internally and by SwarmClient for health data.</summary>
    public async Task<SwarmSessionInfo> GetOrCreateSessionInfoAsync(string sessionKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionKey);
        ObjectDisposedException.ThrowIf(_disposed, this);
        MaybeSweepIdleSlots();
        SessionSlot slot = _slots.GetOrAdd(sessionKey, static _ => new SessionSlot());
        slot.Touch();
        Task<SwarmSessionInfo>? current = slot.Current;
        if (current is { IsCompletedSuccessfully: true })
        {
            return current.Result;
        }
        Task<SwarmSessionInfo> creation;
        lock (slot.Gate)
        {
            current = slot.Current;
            if (current is not null)
            {
                if (current.IsFaulted || current.IsCanceled)
                {
                    // Failure backoff: fail fast with the cached error inside the window instead of hammering GetNewSession.
                    long lastFailure = Volatile.Read(ref slot.LastFailureUtcTicks);
                    if (DateTimeOffset.UtcNow.UtcTicks - lastFailure < _options.SessionCreateFailureBackoff.Ticks)
                    {
                        creation = current;
                    }
                    else
                    {
                        creation = StartCreation(sessionKey, slot);
                        slot.SetCurrent(creation);
                    }
                }
                else
                {
                    // In-flight or already successful: share it.
                    creation = current;
                }
            }
            else
            {
                creation = StartCreation(sessionKey, slot);
                slot.SetCurrent(creation);
            }
        }
        // WaitAsync so one caller's cancellation doesn't cancel the shared creation task for everyone else.
        return await creation.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts a session creation task for a slot. Must be called under the slot gate; the task body runs outside it.</summary>
    private Task<SwarmSessionInfo> StartCreation(string sessionKey, SessionSlot slot)
    {
        _logger.LogDebug("Creating new SwarmUI session for key '{Key}'", sessionKey);
        return CreateNewSessionAsync(sessionKey, slot);
    }

    /// <summary>Calls GetNewSession and converts the response to a <see cref="SwarmSessionInfo"/>. Failures stamp the slot for backoff.</summary>
    private async Task<SwarmSessionInfo> CreateNewSessionAsync(string sessionKey, SessionSlot slot)
    {
        try
        {
            JObject response = await HttpClient.PostJsonAsync<JObject>("GetNewSession", payload: null, sessionKey: sessionKey, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            string? sessionId = response["session_id"]?.ToString();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                _logger.LogError("GetNewSession returned a response without a session_id field");
                throw new SwarmSessionException("GetNewSession API returned a response without a valid session_id field");
            }
            SwarmSessionInfo info = new(
                SessionId: sessionId,
                UserId: response["user_id"]?.ToString(),
                ServerVersion: response["version"]?.ToString(),
                ServerId: response["server_id"]?.ToString(),
                CreatedAt: DateTimeOffset.UtcNow);
            _logger.LogInformation("Session created for key '{Key}': {SessionId}... (user '{UserId}', server version {Version})", sessionKey, Truncate(sessionId), info.UserId, info.ServerVersion);
            return info;
        }
        catch (Exception ex)
        {
            Volatile.Write(ref slot.LastFailureUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
            if (ex is SwarmSessionException)
            {
                throw;
            }
            _logger.LogError(ex, "Failed to create session for key '{Key}'", sessionKey);
            throw new SwarmSessionException("Failed to obtain session from SwarmUI. Verify the server is running and accessible.", ex);
        }
    }

    /// <summary>Opportunistically evicts slots idle beyond <see cref="SwarmClientOptions.SessionIdleEviction"/>. Runs at most once per minute, only when eviction is enabled.</summary>
    private void MaybeSweepIdleSlots()
    {
        if (_options.SessionIdleEviction is not TimeSpan idle)
        {
            return;
        }
        long now = DateTimeOffset.UtcNow.UtcTicks;
        long lastSweep = Volatile.Read(ref _lastEvictionSweepTicks);
        if (now - lastSweep < TimeSpan.TicksPerMinute || Interlocked.CompareExchange(ref _lastEvictionSweepTicks, now, lastSweep) != lastSweep)
        {
            return;
        }
        foreach (KeyValuePair<string, SessionSlot> pair in _slots)
        {
            if (now - Volatile.Read(ref pair.Value.LastAccessTicks) > idle.Ticks)
            {
                _slots.TryRemove(pair.Key, out _);
            }
        }
    }

    /// <summary>Marks the manager disposed. Session slots hold no unmanaged resources; server-side sessions expire on their own.</summary>
    public void Dispose()
    {
        _disposed = true;
        _slots.Clear();
        GC.SuppressFinalize(this);
    }

    private static string Truncate(string sessionId) => sessionId.Length > 8 ? sessionId[..8] : sessionId;
}
