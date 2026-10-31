namespace SwarmUI.ApiClient.Exceptions;

/// <summary>How far a WebSocket request got before it failed, so a caller can tell whether the server may have started the work.</summary>
/// <remarks>Set on <see cref="SwarmException.SendPhase"/> by <c>StreamFramesAsync</c> (and every stream built on it, such as
/// <c>StreamGenerationAsync</c>), and to <see cref="NotInvoked"/> on every session-creation failure. Only <see cref="NotInvoked"/>
/// proves the server cannot have started the work; anything else means it may be running, and replaying it elsewhere could
/// run it twice.
///
/// Two endings carry no phase, and both mean "may have started": a caller's cancellation (an
/// <see cref="System.OperationCanceledException"/>, not a <see cref="SwarmException"/>), and a stream the server or the
/// network closed without an error (no exception at all; <c>StreamGenerationAsync</c> reports it as
/// <c>ErrorInfo.StreamEndedEarlyErrorId</c>). The AudioLab voice session client has its own connection loop and does not
/// set a phase.</remarks>
public enum SwarmSendPhase
{
    /// <summary>The server cannot have started the work: session creation or the connect failed before any payload was
    /// handed to the socket, or the server rejected the session id on every attempt. SwarmUI checks the session before it
    /// looks up the route (<c>src/WebAPI/API.cs</c>), so a payload it answered with <c>invalid_session_id</c> ran nothing.</summary>
    NotInvoked,

    /// <summary>The payload was being sent, or was sent but the server had not answered yet. The server may or may not have it.</summary>
    SendUnknown,

    /// <summary>The server answered at least one frame after the payload, so it received the request.</summary>
    Sent,
}
