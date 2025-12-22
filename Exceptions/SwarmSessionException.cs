using System;

namespace SwarmUI.ApiClient.Exceptions;

/// <summary>Exception thrown when a SwarmUI session is invalid or expired.</summary>
/// <remarks>Typically created when the server returns <c>error_id="invalid_session_id"</c> or a new session cannot be obtained. Callers can invalidate the session and decide whether to retry. See CodingGuidelines.md (HTTP section) for recommended handling patterns.</remarks>
public class SwarmSessionException : SwarmException
{
    /// <summary>Initializes a new instance of the SwarmSessionException class.</summary>
    /// <param name="message">The error message.</param>
    public SwarmSessionException(string message) : base(message, "invalid_session_id")
    {
    }

    /// <summary>Initializes a new instance of the SwarmSessionException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SwarmSessionException(string message, Exception? innerException) : base(message, "invalid_session_id", innerException)
    {
    }
}
