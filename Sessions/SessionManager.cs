using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Http;

namespace SwarmUI.ApiClient.Sessions;

/// <summary>Manages SwarmUI session lifecycle with caching and thread-safe creation.</summary>
/// <remarks>Handles session_id creation, caching, invalidation, and refresh in response to <c>invalid_session_id</c> errors. See CodingGuidelines.md (Sessions section) for lifecycle and implementation details.</remarks>
public class SessionManager : ISessionManager, IDisposable
{
    /// <summary>Internal implementation data containing session state and dependencies.</summary>
    public struct Impl
    {
        /// <summary>Currently cached session ID string, or null if none has been created or it was invalidated.</summary>
        public string? CurrentSession;

        /// <summary>Indicates whether the cached session is considered valid. When false, a new session will be created on the next request.</summary>
        public bool IsSessionValid;

        /// <summary>Semaphore ensuring only one thread creates a session at a time.</summary>
        public SemaphoreSlim SessionLock;

        /// <summary>Factory function that provides the HTTP client when needed, breaking the circular dependency with SwarmHttpClient.</summary>
        public Func<ISwarmHttpClient> HttpClientFactory;

        /// <summary>Lazily-initialized HTTP client for making GetNewSession API calls.</summary>
        public ISwarmHttpClient? HttpClientCache;

        /// <summary>Logger for session lifecycle events.</summary>
        public ILogger<SessionManager> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; typical usage should go through the public methods.</summary>
    public Impl Internal;

    /// <summary>Gets the HTTP client, lazily initializing it on first access.</summary>
    private ISwarmHttpClient HttpClient => Internal.HttpClientCache ??= Internal.HttpClientFactory();

    /// <summary>Creates a new SessionManager instance.</summary>
    /// <param name="httpClientFactory">Factory used to obtain the HTTP client for GetNewSession calls.</param>
    /// <param name="logger">Optional logger for session lifecycle events.</param>
    public SessionManager(Func<ISwarmHttpClient> httpClientFactory, ILogger<SessionManager>? logger = null)
    {
        Internal.HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        Internal.HttpClientCache = null;
        Internal.Logger = logger ?? NullLogger<SessionManager>.Instance;
        Internal.SessionLock = new(1, 1);
        Internal.IsSessionValid = false;
        Internal.CurrentSession = null;
    }

    /// <summary>Gets the current cached session ID or creates a new one if none exists or if invalid.</summary>
    /// <param name="cancellationToken">Cancellation token for the session creation request.</param>
    /// <returns>A valid session ID string that can be used immediately for API calls.</returns>
    /// <exception cref="SwarmSessionException">Thrown when GetNewSession fails or returns invalid data. Callers should treat this as a fatal error requiring user intervention.</exception>
    /// <remarks>Uses double-check locking with a semaphore so only one thread creates a session when none is cached.</remarks>
    public async Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: return cached session without locking
        if (Internal.IsSessionValid && Internal.CurrentSession is not null)
        {
            return Internal.CurrentSession;
        }
        // Slow path: need to create session, acquire lock for thread safety
        await Internal.SessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: another thread might have created session while we waited
            if (Internal.IsSessionValid && Internal.CurrentSession is not null)
            {
                return Internal.CurrentSession;
            }
            Internal.Logger.LogDebug("Creating new SwarmUI session");
            string newSession = await CreateNewSessionAsync(cancellationToken).ConfigureAwait(false);
            Internal.CurrentSession = newSession;
            Internal.IsSessionValid = true;
            Internal.Logger.LogDebug("Session created and cached: {SessionId}", newSession.Substring(0, Math.Min(8, newSession.Length)));
            return newSession;
        }
        finally
        {
            Internal.SessionLock.Release();
        }
    }

    /// <summary>Forces creation of a new session, invalidating any cached session.</summary>
    /// <param name="cancellationToken">Cancellation token for the session creation request.</param>
    /// <returns>A new session ID that has been cached and marked valid.</returns>
    /// <exception cref="SwarmSessionException">Thrown when GetNewSession fails or returns invalid data.</exception>
    /// <remarks>Always calls GetNewSession regardless of cache state; most callers should prefer GetOrCreateSessionAsync.</remarks>
    public async Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        await Internal.SessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Internal.Logger.LogInformation("Forcing session refresh");
            Internal.IsSessionValid = false;
            string newSession = await CreateNewSessionAsync(cancellationToken).ConfigureAwait(false);
            Internal.CurrentSession = newSession;
            Internal.IsSessionValid = true;
            Internal.Logger.LogInformation("Session refreshed: {SessionId}", newSession.Substring(0, Math.Min(8, newSession.Length)));
            return newSession;
        }
        finally
        {
            Internal.SessionLock.Release();
        }
    }

    /// <summary>Marks the current cached session as invalid without creating a new one. This should be called when SwarmUI returns an error with error_id="invalid_session_id".</summary>
    /// <remarks>Called by SwarmHttpClient when it detects a session rejection; marking the session invalid is atomic and the next GetOrCreateSessionAsync call creates a new session.</remarks>
    public void InvalidateSession()
    {
        Internal.Logger.LogWarning("Session invalidated by server");
        Internal.IsSessionValid = false;
        // We don't clear CurrentSession to allow inspection for debugging
    }

    /// <summary>Gets the current cached session ID without creating a new one. Returns null if no session is cached or if the cached session is marked invalid.</summary>
    /// <value>The current session ID string, or null if no valid session is available.</value>
    /// <remarks>Primarily for debugging and monitoring; the value may be stale even when non-null, and most code should use GetOrCreateSessionAsync.</remarks>
    public string? CurrentSessionId => Internal.IsSessionValid ? Internal.CurrentSession : null;

    /// <summary>Disposes of managed resources used by the SessionManager.</summary>
    /// <remarks>Releases the SessionLock semaphore; dispose the manager when no longer needed.</remarks>
    public void Dispose()
    {
        Internal.SessionLock?.Dispose();
    }

    /// <summary>Creates a new session by calling SwarmUI's GetNewSession API endpoint.</summary>
    /// <param name="cancellationToken">Cancellation token for the API request.</param>
    /// <returns>A new session ID string obtained from SwarmUI.</returns>
    /// <exception cref="SwarmSessionException">Thrown when the API call fails, returns invalid JSON, or doesn't include a session_id field.</exception>
    /// <remarks>Used only by GetOrCreateSessionAsync and RefreshSessionAsync. The GetNewSession endpoint does not require a session_id, which allows SessionManager and SwarmHttpClient to break their circular dependency.</remarks>
    private async Task<string> CreateNewSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            JObject response = await HttpClient.PostJsonAsync<JObject>("GetNewSession", null, cancellationToken).ConfigureAwait(false);
            string? sessionId = response["session_id"]?.ToString();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                Internal.Logger.LogError("GetNewSession returned invalid response: {Response}", response);
                throw new SwarmSessionException("GetNewSession API returned a response without a valid session_id field");
            }
            return sessionId;
        }
        catch (SwarmSessionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Internal.Logger.LogError(ex, "Failed to create new session");
            throw new SwarmSessionException("Failed to obtain session from SwarmUI. Verify the server is running and accessible.", ex);
        }
    }
}
