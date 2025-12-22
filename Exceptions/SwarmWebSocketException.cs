using System;
using System.Net.WebSockets;

namespace SwarmUI.ApiClient.Exceptions;

/// <summary>Exception thrown when a WebSocket operation fails.
/// Provides additional context about the WebSocket state at the time of failure.</summary>
/// <remarks>
/// This exception is thrown for WebSocket specific errors:
/// - Connection failures
/// - Send/receive errors
/// - Unexpected disconnections
/// - Protocol errors
///
/// PROPERTIES:
/// - LastState: The WebSocket state when the error occurred
/// - Helps with debugging (was it Open? CloseSent? Aborted?)
/// </remarks>
public class SwarmWebSocketException : SwarmException
{
    /// <summary>Gets the WebSocket state at the time the error occurred, if available.</summary>
    public WebSocketState? LastState { get; }

    /// <summary>Initializes a new instance of the SwarmWebSocketException class.</summary>
    /// <param name="message">The error message.</param>
    public SwarmWebSocketException(string message) : base(message, "websocket_error")
    {
    }

    /// <summary>Initializes a new instance of the SwarmWebSocketException class with WebSocket state.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="lastState">The WebSocket state when the error occurred.</param>
    public SwarmWebSocketException(string message, WebSocketState? lastState) : base(message, "websocket_error")
    {
        LastState = lastState;
    }

    /// <summary>Initializes a new instance of the SwarmWebSocketException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="lastState">The WebSocket state when the error occurred.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SwarmWebSocketException(string message, WebSocketState? lastState, Exception? innerException) : base(message, "websocket_error", innerException)
    {
        LastState = lastState;
    }
}
