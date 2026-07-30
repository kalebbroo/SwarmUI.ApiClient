using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SwarmUI.ApiClient.Extensions.AudioLab;
using SwarmUI.ApiClient.Extensions.LLMAssistant;
using SwarmUI.ApiClient.Extensions.MagicPrompt;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Provides the extension endpoint groups exposed through <see cref="ISwarmClient.Extensions"/>.</summary>
public class SwarmExtensions : ISwarmExtensions
{
    /// <summary>Internal implementation data containing dependencies for the extension endpoint groups.</summary>
    public struct Impl
    {
        /// <summary>HTTP client wrapper shared by every extension endpoint.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>WebSocket client shared by extension endpoints with streaming operations.</summary>
        public ISwarmWebSocketClient WebSocketClient;

        /// <summary>Session manager shared by every extension endpoint.</summary>
        public ISessionManager SessionManager;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should go through the public properties.</summary>
    public Impl Internal;

    /// <inheritdoc />
    public IAudioLabEndpoint AudioLab { get; }

    /// <inheritdoc />
    public ILLMAssistantEndpoint LLMAssistant { get; }

    /// <inheritdoc />
    public IMagicPromptEndpoint MagicPrompt { get; }

    /// <inheritdoc />
    public IReadOnlyList<SwarmExtensionInfo> All { get; }

    /// <summary>Creates the extension endpoint groups with the shared client infrastructure.</summary>
    /// <param name="httpClient">HTTP client wrapper for extension HTTP operations. Must not be null.</param>
    /// <param name="webSocketClient">WebSocket client for extension streaming operations. Must not be null.</param>
    /// <param name="sessionManager">Session manager used for session injection. Must not be null.</param>
    /// <param name="loggerFactory">Optional logger factory used to create per endpoint loggers.</param>
    public SwarmExtensions(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, ISessionManager sessionManager, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(webSocketClient);
        ArgumentNullException.ThrowIfNull(sessionManager);
        Internal.HttpClient = httpClient;
        Internal.WebSocketClient = webSocketClient;
        Internal.SessionManager = sessionManager;
        AudioLab = new AudioLabEndpoint(httpClient, webSocketClient, sessionManager, loggerFactory?.CreateLogger<AudioLabEndpoint>());
        LLMAssistant = new LLMAssistantEndpoint(httpClient, webSocketClient, sessionManager, loggerFactory?.CreateLogger<LLMAssistantEndpoint>());
        MagicPrompt = new MagicPromptEndpoint(httpClient, sessionManager, loggerFactory?.CreateLogger<MagicPromptEndpoint>());
        All = new SwarmExtensionInfo[]
        {
            AudioLabEndpoint.ExtensionInfo,
            LLMAssistantEndpoint.ExtensionInfo,
            MagicPromptEndpoint.ExtensionInfo
        };
    }
}
