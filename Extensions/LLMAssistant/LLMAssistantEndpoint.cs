using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant;

/// <summary>Implements the endpoints added by the LLM Assistant SwarmUI extension.</summary>
/// <remarks>Requires the LLM Assistant extension to be installed on the target SwarmUI server. None of these endpoints exist in stock SwarmUI.</remarks>
public class LLMAssistantEndpoint : ILLMAssistantEndpoint
{
    /// <summary>Metadata for the LLM Assistant extension backing this endpoint group.</summary>
    public static readonly SwarmExtensionInfo ExtensionInfo = new()
    {
        Name = "LLMAssistant",
        DisplayName = "LLM Assistant",
        RepositoryUrl = "https://github.com/HartsyAI/SwarmUI-LLMAssistant",
        Endpoints = new string[]
        {
            "LLMAssistantSendMessage",
            "LLMAssistantSendMessageWS",
            "LLMAssistantEditMessageWS",
            "LLMAssistantRegenerateWS",
            "LLMAssistantCreateThread",
            "LLMAssistantTestInstruction",
            "LLMAssistantUploadChatImage",
            "LLMAssistantCountTokens",
            "LLMAssistantGetSettings",
            "LLMAssistantSaveSettings",
            "LLMAssistantResetSettings",
            "LLMAssistantGetAuditLog",
            "LLMAssistantSetAuditLogEnabled",
            "LLMAssistantGetModels",
            "LLMAssistantUnloadModels",
            "LLMAssistantGetThreads",
            "LLMAssistantGetThread",
            "LLMAssistantDeleteThread",
            "LLMAssistantRenameThread",
            "LLMAssistantSetActiveLeaf",
            "LLMAssistantSetThreadToolsEnabled",
            "LLMAssistantDeleteMessage",
            "LLMAssistantEditMessage",
            "LLMAssistantExportThread",
            "LLMAssistantGetSessionState",
            "LLMAssistantSetSessionState",
            "LLMAssistantGetAssets",
            "LLMAssistantGetAsset",
            "LLMAssistantDeleteAsset",
            "LLMAssistantGetInstructions",
            "LLMAssistantSaveInstruction",
            "LLMAssistantDeleteInstruction",
            "LLMAssistantGetAssistants",
            "LLMAssistantGetAssistant",
            "LLMAssistantGetActiveAssistant",
            "LLMAssistantSaveAssistant",
            "LLMAssistantDeleteAssistant",
            "LLMAssistantSetActiveAssistant",
            "LLMAssistantUploadAssistantAvatar",
            "LLMAssistantGetStarterTemplates",
            "LLMAssistantGetTools",
            "LLMAssistantGetTool",
            "LLMAssistantSaveTool",
            "LLMAssistantDeleteTool",
            "LLMAssistantExecuteTool",
            "LLMAssistantGetToolConfig",
            "LLMAssistantSetToolConfig",
            "LLMAssistantGetImagePresets",
            "LLMAssistantGetCompanionContext",
            "LLMAssistantGetUserProfile",
            "LLMAssistantClearUserProfile"
        }
    };

    private readonly ISwarmHttpClient _httpClient;
    private readonly ISwarmWebSocketClient _webSocketClient;
    private readonly string _sessionKey;
    private readonly ILogger<LLMAssistantEndpoint> _logger;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new LLMAssistantEndpoint.</summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="webSocketClient">WebSocket client for streaming chat operations.</param>
    /// <param name="sessionKey">The pooled session key all calls from this endpoint instance authenticate with.</param>
    /// <param name="logger">Optional logger.</param>
    public LLMAssistantEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, string sessionKey, ILogger<LLMAssistantEndpoint>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        _logger = logger ?? NullLogger<LLMAssistantEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Requesting completion with model '{Model}' ({CharCount} chars)", request.Model ?? "(server default)", request.Message.Length);
        ChatCompletionResponse response = await _httpClient.PostJsonAsync<ChatCompletionResponse>("LLMAssistantSendMessage", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Completion");
        return response;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatStreamUpdate> StreamMessageAsync(ChatStreamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        _logger.LogDebug("Streaming chat message into thread '{ThreadId}'", request.ThreadId);
        return StreamChatAsync("LLMAssistantSendMessageWS", request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatStreamUpdate> StreamEditMessageAsync(ChatStreamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        if (string.IsNullOrEmpty(request.MessageId))
        {
            throw new ArgumentException("MessageId is required when editing a message", nameof(request));
        }
        if (request.Content is null)
        {
            throw new ArgumentException("Content is required when editing a message", nameof(request));
        }
        _logger.LogDebug("Streaming edit of message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        return StreamChatAsync("LLMAssistantEditMessageWS", request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatStreamUpdate> StreamRegenerateAsync(ChatStreamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        if (string.IsNullOrEmpty(request.MessageId))
        {
            throw new ArgumentException("MessageId is required when regenerating a reply", nameof(request));
        }
        _logger.LogDebug("Streaming regeneration of message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        return StreamChatAsync("LLMAssistantRegenerateWS", request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> CreateThreadAsync(string? assistantId = null, string? title = null, CancellationToken cancellationToken = default)
    {
        JObject payload = [];
        if (!string.IsNullOrEmpty(assistantId))
        {
            payload["assistantId"] = assistantId;
        }
        if (!string.IsNullOrEmpty(title))
        {
            payload["title"] = title;
        }
        _logger.LogDebug("Creating chat thread for assistant '{AssistantId}'", assistantId ?? "(active)");
        ThreadResponse response = await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantCreateThread", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread creation");
        return response;
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> TestInstructionAsync(TestInstructionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.InstructionText))
        {
            throw new ArgumentException("InstructionText cannot be null or empty", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.SampleInput))
        {
            throw new ArgumentException("SampleInput cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Testing instruction against sample input with model '{Model}'", request.Model ?? "(server default)");
        ChatCompletionResponse response = await _httpClient.PostJsonAsync<ChatCompletionResponse>("LLMAssistantTestInstruction", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Instruction test");
        return response;
    }

    /// <inheritdoc />
    public async Task<UploadChatImageResponse> UploadChatImageAsync(UploadChatImageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            throw new ArgumentException("MessageId cannot be null or empty", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.ImageData))
        {
            throw new ArgumentException("ImageData cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Uploading chat image for message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        UploadChatImageResponse response = await _httpClient.PostJsonAsync<UploadChatImageResponse>("LLMAssistantUploadChatImage", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Chat image upload");
        return response;
    }

    /// <inheritdoc />
    public async Task<CountTokensResponse> CountTokensAsync(CountTokensRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Counting tokens");
        return await _httpClient.PostJsonAsync<CountTokensResponse>("LLMAssistantCountTokens", request, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading LLM Assistant settings");
        return await _httpClient.PostJsonAsync<LLMSettingsResponse>("LLMAssistantGetSettings", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMSettingsResponse> SaveSettingsAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Saving LLM Assistant settings to '{Scope}' layer", request.Scope ?? "personal");
        LLMSettingsResponse response = await _httpClient.PostJsonAsync<LLMSettingsResponse>("LLMAssistantSaveSettings", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Settings save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMSettingsResponse> ResetSettingsAsync(string? scope = null, CancellationToken cancellationToken = default)
    {
        JObject payload = [];
        if (!string.IsNullOrEmpty(scope))
        {
            payload["scope"] = scope;
        }
        _logger.LogDebug("Resetting LLM Assistant settings in '{Scope}' layer", scope ?? "personal");
        LLMSettingsResponse response = await _httpClient.PostJsonAsync<LLMSettingsResponse>("LLMAssistantResetSettings", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Settings reset");
        return response;
    }

    /// <inheritdoc />
    public async Task<AuditLogResponse> GetAuditLogAsync(int? max = null, CancellationToken cancellationToken = default)
    {
        JObject payload = [];
        if (max.HasValue)
        {
            payload["max"] = max.Value;
        }
        _logger.LogDebug("Reading LLM Assistant audit log");
        return await _httpClient.PostJsonAsync<AuditLogResponse>("LLMAssistantGetAuditLog", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuditLogResponse> SetAuditLogEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["enabled"] = enabled
        };
        _logger.LogDebug("Setting LLM Assistant audit log enabled to {Enabled}", enabled);
        AuditLogResponse response = await _httpClient.PostJsonAsync<AuditLogResponse>("LLMAssistantSetAuditLogEnabled", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Audit log toggle");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing LLM models");
        LLMModelsResponse response = await _httpClient.PostJsonAsync<LLMModelsResponse>("LLMAssistantGetModels", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        if (response.Warnings.Length > 0)
        {
            _logger.LogWarning("Retrieved {ModelCount} LLM model(s) with {WarningCount} provider warning(s): {Warnings}",
                response.Models.Count, response.Warnings.Length, string.Join("; ", response.Warnings));
        }
        else
        {
            _logger.LogInformation("Retrieved {ModelCount} LLM model(s)", response.Models.Count);
        }
        return response;
    }

    /// <inheritdoc />
    public async Task<UnloadModelsResponse> UnloadModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Unloading LLM models");
        UnloadModelsResponse response = await _httpClient.PostJsonAsync<UnloadModelsResponse>("LLMAssistantUnloadModels", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Unloaded models on {Freed}/{Providers} provider(s)", response.Freed, response.Providers);
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadListResponse> GetThreadsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing chat threads");
        ThreadListResponse response = await _httpClient.PostJsonAsync<ThreadListResponse>("LLMAssistantGetThreads", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {ThreadCount} chat thread(s)", response.Threads.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        _logger.LogDebug("Reading chat thread '{ThreadId}'", threadId);
        return await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantGetThread", ThreadPayload(threadId), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        _logger.LogDebug("Deleting chat thread '{ThreadId}'", threadId);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteThread", ThreadPayload(threadId), _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> RenameThreadAsync(string threadId, string title, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        }
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["title"] = title
        };
        _logger.LogDebug("Renaming chat thread '{ThreadId}'", threadId);
        ThreadResponse response = await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantRenameThread", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread rename");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> SetActiveLeafAsync(string threadId, string messageId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        RequireMessageId(messageId);
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["messageId"] = messageId
        };
        _logger.LogDebug("Setting active leaf of thread '{ThreadId}' to message '{MessageId}'", threadId, messageId);
        ThreadResponse response = await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantSetActiveLeaf", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Active leaf change");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadToolsEnabledResponse> SetThreadToolsEnabledAsync(string threadId, bool? enabled, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        JObject payload = new()
        {
            ["threadId"] = threadId
        };
        if (enabled.HasValue)
        {
            payload["enabled"] = enabled.Value;
        }
        _logger.LogDebug("Setting tool override on thread '{ThreadId}' to {Enabled}", threadId, enabled?.ToString() ?? "(inherit)");
        ThreadToolsEnabledResponse response = await _httpClient.PostJsonAsync<ThreadToolsEnabledResponse>("LLMAssistantSetThreadToolsEnabled", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread tool override");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> DeleteMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        RequireMessageId(messageId);
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["messageId"] = messageId
        };
        _logger.LogDebug("Deleting message '{MessageId}' from thread '{ThreadId}'", messageId, threadId);
        ThreadResponse response = await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantDeleteMessage", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Message delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        RequireMessageId(request.MessageId);
        _logger.LogDebug("Editing message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        ThreadResponse response = await _httpClient.PostJsonAsync<ThreadResponse>("LLMAssistantEditMessage", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Message edit");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadExportResponse> ExportThreadAsync(string threadId, string format = "json", CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["format"] = format
        };
        _logger.LogDebug("Exporting thread '{ThreadId}' as '{Format}'", threadId, format);
        ThreadExportResponse response = await _httpClient.PostJsonAsync<ThreadExportResponse>("LLMAssistantExportThread", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread export");
        return response;
    }

    /// <inheritdoc />
    public async Task<SessionStateResponse> GetSessionStateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading LLM Assistant session state");
        return await _httpClient.PostJsonAsync<SessionStateResponse>("LLMAssistantGetSessionState", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionStateResponse> SetSessionStateAsync(JObject state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        JObject payload = new()
        {
            ["state"] = state
        };
        _logger.LogDebug("Patching LLM Assistant session state");
        SessionStateResponse response = await _httpClient.PostJsonAsync<SessionStateResponse>("LLMAssistantSetSessionState", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Session state patch");
        return response;
    }

    /// <inheritdoc />
    public async Task<AssetListResponse> GetAssetsAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        _logger.LogDebug("Listing assets for thread '{ThreadId}'", threadId);
        return await _httpClient.PostJsonAsync<AssetListResponse>("LLMAssistantGetAssets", ThreadPayload(threadId), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AssetResponse> GetAssetAsync(string threadId, string assetId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        if (string.IsNullOrWhiteSpace(assetId))
        {
            throw new ArgumentException("Asset ID cannot be null or empty", nameof(assetId));
        }
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["assetId"] = assetId
        };
        _logger.LogDebug("Reading asset '{AssetId}' from thread '{ThreadId}'", assetId, threadId);
        return await _httpClient.PostJsonAsync<AssetResponse>("LLMAssistantGetAsset", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteAssetAsync(string threadId, string assetId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        if (string.IsNullOrWhiteSpace(assetId))
        {
            throw new ArgumentException("Asset ID cannot be null or empty", nameof(assetId));
        }
        JObject payload = new()
        {
            ["threadId"] = threadId,
            ["assetId"] = assetId
        };
        _logger.LogDebug("Deleting asset '{AssetId}' from thread '{ThreadId}'", assetId, threadId);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteAsset", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Asset delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<InstructionListResponse> GetInstructionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing instructions");
        return await _httpClient.PostJsonAsync<InstructionListResponse>("LLMAssistantGetInstructions", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveInstructionAsync(SaveInstructionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Saving instruction '{Id}' to '{Scope}' layer", request.Id ?? "(new)", request.Scope ?? "personal");
        ScopedWriteResponse response = await _httpClient.PostJsonAsync<ScopedWriteResponse>("LLMAssistantSaveInstruction", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Instruction save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteInstructionAsync(string id, string? scope = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Instruction ID cannot be null or empty", nameof(id));
        }
        _logger.LogDebug("Deleting instruction '{Id}'", id);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteInstruction", ScopedIdPayload("id", id, scope), _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Instruction delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<AssistantListResponse> GetAssistantsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing assistants");
        AssistantListResponse response = await _httpClient.PostJsonAsync<AssistantListResponse>("LLMAssistantGetAssistants", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {AssistantCount} assistant(s)", response.Assistants.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<AssistantResponse> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
    {
        RequireAssistantId(assistantId);
        JObject payload = new()
        {
            ["assistantId"] = assistantId
        };
        _logger.LogDebug("Reading assistant '{AssistantId}'", assistantId);
        return await _httpClient.PostJsonAsync<AssistantResponse>("LLMAssistantGetAssistant", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AssistantResponse> GetActiveAssistantAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading active assistant");
        return await _httpClient.PostJsonAsync<AssistantResponse>("LLMAssistantGetActiveAssistant", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveAssistantAsync(SaveAssistantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Assistant.HasValues)
        {
            throw new ArgumentException("Assistant data cannot be empty", nameof(request));
        }
        _logger.LogDebug("Saving assistant to '{Scope}' layer", request.Scope ?? "personal");
        ScopedWriteResponse response = await _httpClient.PostJsonAsync<ScopedWriteResponse>("LLMAssistantSaveAssistant", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Assistant save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteAssistantAsync(string assistantId, string? scope = null, CancellationToken cancellationToken = default)
    {
        RequireAssistantId(assistantId);
        _logger.LogDebug("Deleting assistant '{AssistantId}'", assistantId);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteAssistant", ScopedIdPayload("assistantId", assistantId, scope), _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Assistant delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> SetActiveAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
    {
        RequireAssistantId(assistantId);
        JObject payload = new()
        {
            ["assistantId"] = assistantId
        };
        _logger.LogDebug("Setting active assistant to '{AssistantId}'", assistantId);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantSetActiveAssistant", payload, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Active assistant change");
        return response;
    }

    /// <inheritdoc />
    public async Task<UploadAssistantAvatarResponse> UploadAssistantAvatarAsync(UploadAssistantAvatarRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireAssistantId(request.AssistantId);
        if (string.IsNullOrWhiteSpace(request.ImageData))
        {
            throw new ArgumentException("ImageData cannot be null or empty", nameof(request));
        }
        _logger.LogDebug("Uploading avatar for assistant '{AssistantId}'", request.AssistantId);
        UploadAssistantAvatarResponse response = await _httpClient.PostJsonAsync<UploadAssistantAvatarResponse>("LLMAssistantUploadAssistantAvatar", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Avatar upload");
        return response;
    }

    /// <inheritdoc />
    public async Task<StarterTemplatesResponse> GetStarterTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading starter assistant templates");
        return await _httpClient.PostJsonAsync<StarterTemplatesResponse>("LLMAssistantGetStarterTemplates", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ToolListResponse> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing tools");
        ToolListResponse response = await _httpClient.PostJsonAsync<ToolListResponse>("LLMAssistantGetTools", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Retrieved {ToolCount} tool(s)", response.Tools.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<ToolResponse> GetToolAsync(string toolId, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        _logger.LogDebug("Reading tool '{ToolId}'", toolId);
        return await _httpClient.PostJsonAsync<ToolResponse>("LLMAssistantGetTool", ToolPayload(toolId), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveToolAsync(SaveToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Tool.HasValues)
        {
            throw new ArgumentException("Tool data cannot be empty", nameof(request));
        }
        _logger.LogDebug("Saving tool to '{Scope}' layer", request.Scope ?? "personal");
        ScopedWriteResponse response = await _httpClient.PostJsonAsync<ScopedWriteResponse>("LLMAssistantSaveTool", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteToolAsync(string toolId, string? scope = null, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        _logger.LogDebug("Deleting tool '{ToolId}'", toolId);
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteTool", ScopedIdPayload("toolId", toolId, scope), _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<ToolConfigResponse> GetToolConfigAsync(string toolId, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        _logger.LogDebug("Reading config for tool '{ToolId}'", toolId);
        return await _httpClient.PostJsonAsync<ToolConfigResponse>("LLMAssistantGetToolConfig", ToolPayload(toolId), _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ToolConfigResponse> SetToolConfigAsync(SetToolConfigRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireToolId(request.ToolId);
        _logger.LogDebug("Setting config for tool '{ToolId}'", request.ToolId);
        ToolConfigResponse response = await _httpClient.PostJsonAsync<ToolConfigResponse>("LLMAssistantSetToolConfig", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool config save");
        return response;
    }

    /// <inheritdoc />
    public async Task<ExecuteToolResponse> ExecuteToolAsync(ExecuteToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireToolId(request.ToolId);
        _logger.LogDebug("Executing tool '{ToolId}'", request.ToolId);
        ExecuteToolResponse response = await _httpClient.PostJsonAsync<ExecuteToolResponse>("LLMAssistantExecuteTool", request, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool execution");
        return response;
    }

    /// <inheritdoc />
    public async Task<ImagePresetsResponse> GetImagePresetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing image presets");
        return await _httpClient.PostJsonAsync<ImagePresetsResponse>("LLMAssistantGetImagePresets", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CompanionContextResponse> GetCompanionContextAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading companion context");
        return await _httpClient.PostJsonAsync<CompanionContextResponse>("LLMAssistantGetCompanionContext", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UserProfileResponse> GetUserProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading user memory profile");
        return await _httpClient.PostJsonAsync<UserProfileResponse>("LLMAssistantGetUserProfile", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> ClearUserProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing user memory profile");
        LLMAssistantResponse response = await _httpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantClearUserProfile", payload: null, _sessionKey, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Memory profile clear");
        return response;
    }

    /// <summary>Streams one of the chat WebSocket endpoints.</summary>
    private async IAsyncEnumerable<ChatStreamUpdate> StreamChatAsync(string endpoint, ChatStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        JObject payload = JObject.FromObject(request);
        await foreach (JObject frame in _webSocketClient.StreamFramesAsync(endpoint, payload, _sessionKey, cancellationToken).ConfigureAwait(false))
        {
            ChatStreamUpdate update = frame.ToObject<ChatStreamUpdate>() ?? new ChatStreamUpdate();
            update.Raw = frame;
            yield return update;
        }
    }

    /// <summary>Builds a payload carrying only a thread identifier.</summary>
    private static JObject ThreadPayload(string threadId)
    {
        return new JObject
        {
            ["threadId"] = threadId
        };
    }

    /// <summary>Builds a payload carrying only a tool identifier.</summary>
    private static JObject ToolPayload(string toolId)
    {
        return new JObject
        {
            ["toolId"] = toolId
        };
    }

    /// <summary>Builds a payload carrying an identifier and an optional settings layer scope.</summary>
    private static JObject ScopedIdPayload(string idField, string id, string? scope)
    {
        JObject payload = new()
        {
            [idField] = id
        };
        if (!string.IsNullOrEmpty(scope))
        {
            payload["scope"] = scope;
        }
        return payload;
    }

    /// <summary>Validates that a thread identifier was supplied.</summary>
    private static void RequireThreadId(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread ID cannot be null or empty", nameof(threadId));
        }
    }

    /// <summary>Validates that a message identifier was supplied.</summary>
    private static void RequireMessageId(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message ID cannot be null or empty", nameof(messageId));
        }
    }

    /// <summary>Validates that an assistant identifier was supplied.</summary>
    private static void RequireAssistantId(string assistantId)
    {
        if (string.IsNullOrWhiteSpace(assistantId))
        {
            throw new ArgumentException("Assistant ID cannot be null or empty", nameof(assistantId));
        }
    }

    /// <summary>Validates that a tool identifier was supplied.</summary>
    private static void RequireToolId(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            throw new ArgumentException("Tool ID cannot be null or empty", nameof(toolId));
        }
    }

    /// <summary>Logs whether an LLM Assistant operation succeeded.</summary>
    private void LogOutcome(LLMAssistantResponse response, string operation)
    {
        if (response.Success)
        {
            _logger.LogInformation("{Operation} succeeded", operation);
        }
        else
        {
            _logger.LogWarning("{Operation} failed: {Error}", operation, response.Error ?? "Unknown error");
        }
    }
}
