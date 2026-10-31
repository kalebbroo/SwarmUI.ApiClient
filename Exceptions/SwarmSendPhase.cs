namespace SwarmUI.ApiClient.Exceptions;

/// <summary>How far a WebSocket request got before it failed, so a caller can tell whether the server may have started the work.</summary>
/// <remarks>Set on <see cref="SwarmException.SendPhase"/> by <c>StreamFramesAsync</c> (and every stream built on it, such as
/// <c>StreamGenerationAsync</c>). Only <see cref="NotInvoked"/> proves the server never received the request; anything else
/// means it may be running, and replaying it elsewhere could run it twice.</remarks>
public enum SwarmSendPhase
{
    /// <summary>Failed before any of the request payload was handed to the socket: session creation, connect, or a session
    /// the server rejected. The server cannot have started the work.</summary>
    NotInvoked,

    /// <summary>The payload was being sent, or was sent but the server had not answered yet. The server may or may not have it.</summary>
    SendUnknown,

    /// <summary>The server answered at least one frame after the payload, so it received the request.</summary>
    Sent,
}
