using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SwarmUI.ApiClient.Extensions.APIBackends;
using SwarmUI.ApiClient.Extensions.AudioLab;
using SwarmUI.ApiClient.Extensions.HartsyInference;
using SwarmUI.ApiClient.Extensions.LLMAssistant;
using SwarmUI.ApiClient.Extensions.MagicPrompt;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Provides the extension endpoint groups exposed through <see cref="ISwarmClient.Extensions"/>.</summary>
public class SwarmExtensions : ISwarmExtensions
{
    /// <inheritdoc />
    public IAudioLabEndpoint AudioLab { get; }

    /// <inheritdoc />
    public ILLMAssistantEndpoint LLMAssistant { get; }

    /// <inheritdoc />
    public IMagicPromptEndpoint MagicPrompt { get; }

    /// <inheritdoc />
    public IAPIBackendsEndpoint APIBackends { get; }

    /// <inheritdoc />
    public IHartsyInferenceEndpoint HartsyInference { get; }

    /// <inheritdoc />
    public IReadOnlyList<SwarmExtensionInfo> All { get; }

    /// <summary>Creates the extension endpoint groups with the shared client infrastructure.</summary>
    /// <param name="httpClient">HTTP client wrapper for extension HTTP operations.</param>
    /// <param name="webSocketClient">WebSocket client for extension streaming operations.</param>
    /// <param name="sessionKey">The pooled session key all extension calls authenticate with.</param>
    /// <param name="loggerFactory">Optional logger factory used to create per endpoint loggers.</param>
    /// <remarks>Built this way, <see cref="AudioLab"/> has no <see cref="SwarmClientOptions"/>/<see cref="ISessionManager"/>,
    /// so <c>AudioLab.CreateVoiceSession</c> throws <see cref="InvalidOperationException"/>; use the other
    /// constructor (what <see cref="SwarmClient"/> uses) for that.</remarks>
    public SwarmExtensions(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(webSocketClient);
        ArgumentNullException.ThrowIfNull(sessionKey);
        AudioLab = new AudioLabEndpoint(httpClient, webSocketClient, sessionKey, loggerFactory?.CreateLogger<AudioLabEndpoint>());
        LLMAssistant = new LLMAssistantEndpoint(httpClient, webSocketClient, sessionKey, loggerFactory?.CreateLogger<LLMAssistantEndpoint>());
        MagicPrompt = new MagicPromptEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<MagicPromptEndpoint>());
        APIBackends = new APIBackendsEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<APIBackendsEndpoint>());
        HartsyInference = new HartsyInferenceEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<HartsyInferenceEndpoint>());
        All = new SwarmExtensionInfo[]
        {
            AudioLabEndpoint.ExtensionInfo,
            LLMAssistantEndpoint.ExtensionInfo,
            MagicPromptEndpoint.ExtensionInfo,
            APIBackendsEndpoint.ExtensionInfo,
            HartsyInferenceEndpoint.ExtensionInfo
        };
    }

    /// <summary>Creates the extension endpoint groups with the shared client infrastructure, additionally
    /// threading <see cref="SwarmClientOptions"/>/<see cref="ISessionManager"/> through to <see cref="AudioLab"/>
    /// so <c>AudioLab.CreateVoiceSession</c> works.</summary>
    /// <param name="httpClient">HTTP client wrapper for extension HTTP operations.</param>
    /// <param name="webSocketClient">WebSocket client for extension streaming operations.</param>
    /// <param name="sessionKey">The pooled session key all extension calls authenticate with.</param>
    /// <param name="options">Client configuration options, threaded through to <see cref="AudioLab"/>.</param>
    /// <param name="sessionManager">Session pool, threaded through to <see cref="AudioLab"/> the same way.</param>
    /// <param name="loggerFactory">Optional logger factory used to create per endpoint loggers.</param>
    public SwarmExtensions(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, SwarmClientOptions options, ISessionManager sessionManager, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(webSocketClient);
        ArgumentNullException.ThrowIfNull(sessionKey);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sessionManager);
        AudioLab = new AudioLabEndpoint(httpClient, webSocketClient, sessionKey, options, sessionManager, loggerFactory);
        LLMAssistant = new LLMAssistantEndpoint(httpClient, webSocketClient, sessionKey, loggerFactory?.CreateLogger<LLMAssistantEndpoint>());
        MagicPrompt = new MagicPromptEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<MagicPromptEndpoint>());
        APIBackends = new APIBackendsEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<APIBackendsEndpoint>());
        HartsyInference = new HartsyInferenceEndpoint(httpClient, sessionKey, loggerFactory?.CreateLogger<HartsyInferenceEndpoint>());
        All = new SwarmExtensionInfo[]
        {
            AudioLabEndpoint.ExtensionInfo,
            LLMAssistantEndpoint.ExtensionInfo,
            MagicPromptEndpoint.ExtensionInfo,
            APIBackendsEndpoint.ExtensionInfo,
            HartsyInferenceEndpoint.ExtensionInfo
        };
    }
}
