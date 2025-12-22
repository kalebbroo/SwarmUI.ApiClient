using System;

namespace SwarmUI.ApiClient.Exceptions;

/// <summary>Exception thrown when authentication with SwarmUI fails.
/// This occurs when the authorization header is invalid or missing when required.</summary>
public class SwarmAuthenticationException : SwarmException
{
    /// <summary>Initializes a new instance of the SwarmAuthenticationException class.</summary>
    /// <param name="message">The error message.</param>
    public SwarmAuthenticationException(string message) : base(message, "authentication_failed")
    {
    }

    /// <summary>Initializes a new instance of the SwarmAuthenticationException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SwarmAuthenticationException(string message, Exception? innerException) : base(message, "authentication_failed", innerException)
    {
    }
}
