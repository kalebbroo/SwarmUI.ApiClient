using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmUI.ApiClient.Sessions;

/// <summary>Well-known session keys for the keyed session pool.</summary>
public static class SwarmSessionKeys
{
    /// <summary>The default session key used when a caller does not select a session explicitly. Single-session consumers never need anything else.</summary>
    public const string Default = "__default";
}

/// <summary>Immutable snapshot of a SwarmUI session obtained from GetNewSession.</summary>
/// <param name="SessionId">The session id to pass on API calls. Treat as a bearer-equivalent secret.</param>
/// <param name="UserId">The SwarmUI account the session is bound to ("local" on single-user instances).</param>
/// <param name="ServerVersion">The server's version string, when provided.</param>
/// <param name="ServerId">The server's instance id, when provided. Changes when the server restarts or updates.</param>
/// <param name="CreatedAt">When this client created the session.</param>
public sealed record SwarmSessionInfo(string SessionId, string? UserId, string? ServerVersion, string? ServerId, DateTimeOffset CreatedAt);

/// <summary>Manages a pool of SwarmUI sessions keyed by an arbitrary caller-chosen string (for example an application user id).</summary>
/// <remarks>SwarmUI scopes generation queues, status counters, and InterruptAll to a session — one session key per logical consumer gives each consumer independent interrupt and status. Implementations must be thread-safe: creation is single-flight per key and invalidation is compare-and-swap on the observed session id so concurrent failures cannot stampede GetNewSession.</remarks>
public interface ISessionManager
{
    /// <summary>Gets the cached session id for the key, creating one via GetNewSession if none exists or the cached one was invalidated.</summary>
    /// <param name="sessionKey">The session pool key. Use the default for single-session scenarios.</param>
    /// <param name="cancellationToken">Cancellation token for the session creation request.</param>
    /// <returns>A session id valid at the time of return.</returns>
    /// <exception cref="Exceptions.SwarmSessionException">Thrown when GetNewSession fails or returns invalid data.</exception>
    Task<string> GetOrCreateSessionAsync(string sessionKey = SwarmSessionKeys.Default, CancellationToken cancellationToken = default);

    /// <summary>Invalidates (if still current) and replaces the session for the key, returning the replacement id.</summary>
    /// <param name="sessionKey">The session pool key.</param>
    /// <param name="observedSessionId">The session id the caller saw fail. If another caller already refreshed past it, the existing fresh session is returned with no extra server call. Pass null to force a refresh unconditionally.</param>
    /// <param name="cancellationToken">Cancellation token for the session creation request.</param>
    Task<string> RefreshSessionAsync(string sessionKey, string? observedSessionId, CancellationToken cancellationToken = default);

    /// <summary>Marks the session for the key invalid so the next <see cref="GetOrCreateSessionAsync"/> creates a fresh one — but only if the cached session still matches <paramref name="observedSessionId"/>.</summary>
    /// <param name="sessionKey">The session pool key.</param>
    /// <param name="observedSessionId">The session id the caller saw fail; pass null to invalidate unconditionally.</param>
    /// <remarks>The compare-and-swap semantics prevent N concurrent failures of one stale session from repeatedly discarding each other's replacement sessions.</remarks>
    void InvalidateSession(string sessionKey, string? observedSessionId);

    /// <summary>Returns the cached session snapshot for the key without creating one, or null when none is cached (or creation is still in flight or failed).</summary>
    SwarmSessionInfo? GetCachedSession(string sessionKey = SwarmSessionKeys.Default);
}
