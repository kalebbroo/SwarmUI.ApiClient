using System;

namespace SwarmUI.ApiClient.Exceptions;

/// <summary>Base exception for all SwarmUI client errors.
/// Provides additional context about SwarmUI-specific error conditions.</summary>
/// <remarks>
/// This is the base exception class for the library.
///
/// USAGE PATTERN:
/// - Throw specific derived exceptions (SwarmSessionException, SwarmWebSocketException, etc.)
/// - Catch SwarmException at a high level to handle all library-specific errors
/// - Include ErrorId when SwarmUI API returns an error_id field
/// - Include InnerException when wrapping lower-level exceptions
///
/// PROPERTIES:
/// - ErrorId: The error identifier from SwarmUI (e.g., "invalid_session_id")
/// - Message: User-friendly error message
/// - InnerException: The underlying exception that caused this error (if any)
///
/// EXAMPLE:
/// try
/// {
///     await client.Generation.StreamGenerationAsync(request);
/// }
/// catch (SwarmSessionException ex)
/// {
///     // Handle session expiration specifically
///     await client.RefreshSessionAsync();
///     // Retry...
/// }
/// catch (SwarmException ex)
/// {
///     // Handle any other SwarmUI error
///     Console.WriteLine($"SwarmUI error ({ex.ErrorId}): {ex.Message}");
/// }
/// </remarks>
public class SwarmException : Exception
{
    /// <summary>Gets the error identifier from SwarmUI, if available.
    /// Common values: "invalid_session_id", "authentication_failed", etc.
    /// </summary>
    public string? ErrorId { get; }

    /// <summary>Initializes a new instance of the SwarmException class.</summary>
    /// <param name="message">The error message.</param>
    public SwarmException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the SwarmException class with an error ID.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorId">The SwarmUI error identifier.</param>
    public SwarmException(string message, string? errorId) : base(message)
    {
        ErrorId = errorId;
    }

    /// <summary>Initializes a new instance of the SwarmException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SwarmException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the SwarmException class with an error ID and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorId">The SwarmUI error identifier.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SwarmException(string message, string? errorId, Exception? innerException) : base(message, innerException)
    {
        ErrorId = errorId;
    }
}
