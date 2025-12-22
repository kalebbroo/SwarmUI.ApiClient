using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmUI.ApiClient.Sessions;

/// <summary>Manages SwarmUI session lifecycle including creation, caching, and refresh.</summary>
/// <remarks>Abstracts away session management details such as invalidation, caching, and thread safety. See CodingGuidelines.md for lifecycle and implementation details.</remarks>
public interface ISessionManager
{
    /// <summary>Gets the current cached session ID, or creates a new one if none exists or if cached session is invalid. This method is thread-safe and ensures only one session creation happens at a time.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A valid session ID for use in API requests.</returns>
    Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>Forces creation of a new session, invalidating any cached session. Use this when you explicitly need a fresh session.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A new session ID.</returns>
    Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks the current cached session as invalid. The next call to GetOrCreateSessionAsync will create a new session. This should be called when API returns error_id="invalid_session_id".</summary>
    void InvalidateSession();

    /// <summary>Gets the current cached session ID without creating a new one. Returns null if no session is cached or if the cached session is invalid.</summary>
    string? CurrentSessionId { get; }
}
