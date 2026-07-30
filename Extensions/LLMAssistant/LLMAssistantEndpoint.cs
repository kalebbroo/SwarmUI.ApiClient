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

    /// <summary>Internal implementation data containing dependencies.</summary>
    public struct Impl
    {
        /// <summary>HTTP client for making API requests with automatic session injection.</summary>
        public ISwarmHttpClient HttpClient;

        /// <summary>WebSocket client for streaming chat operations.</summary>
        public ISwarmWebSocketClient WebSocketClient;

        /// <summary>Session manager for obtaining session IDs (used indirectly via HttpClient).</summary>
        public ISessionManager SessionManager;

        /// <summary>Logger for endpoint operations.</summary>
        public ILogger<LLMAssistantEndpoint> Logger;
    }

    /// <summary>Internal implementation data for advanced scenarios; normal usage should use the public members.</summary>
    public Impl Internal;

    /// <inheritdoc />
    public SwarmExtensionInfo Extension => ExtensionInfo;

    /// <summary>Creates a new LLMAssistantEndpoint instance with the specified dependencies.</summary>
    /// <param name="httpClient">HTTP client for API requests. Must not be null.</param>
    /// <param name="webSocketClient">WebSocket client for streaming chat operations. Must not be null.</param>
    /// <param name="sessionManager">Session manager for session lifecycle. Must not be null.</param>
    /// <param name="logger">Optional logger for operations. Uses NullLogger if null.</param>
    public LLMAssistantEndpoint(ISwarmHttpClient httpClient, ISwarmWebSocketClient webSocketClient, ISessionManager sessionManager, ILogger<LLMAssistantEndpoint>? logger = null)
    {
        Internal.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Internal.WebSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        Internal.SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        Internal.Logger = logger ?? NullLogger<LLMAssistantEndpoint>.Instance;
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(request));
        }
        Internal.Logger.LogDebug("Requesting completion with model '{Model}' ({CharCount} chars)", request.Model ?? "(server default)", request.Message.Length);
        ChatCompletionResponse response = await Internal.HttpClient.PostJsonAsync<ChatCompletionRequest, ChatCompletionResponse>("LLMAssistantSendMessage", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Completion");
        return response;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatStreamUpdate> StreamMessageAsync(ChatStreamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        Internal.Logger.LogDebug("Streaming chat message into thread '{ThreadId}'", request.ThreadId);
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
        Internal.Logger.LogDebug("Streaming edit of message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
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
        Internal.Logger.LogDebug("Streaming regeneration of message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
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
        Internal.Logger.LogDebug("Creating chat thread for assistant '{AssistantId}'", assistantId ?? "(active)");
        ThreadResponse response = await Internal.HttpClient.PostJsonAsync<ThreadResponse>("LLMAssistantCreateThread", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Testing instruction against sample input with model '{Model}'", request.Model ?? "(server default)");
        ChatCompletionResponse response = await Internal.HttpClient.PostJsonAsync<TestInstructionRequest, ChatCompletionResponse>("LLMAssistantTestInstruction", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Uploading chat image for message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        UploadChatImageResponse response = await Internal.HttpClient.PostJsonAsync<UploadChatImageRequest, UploadChatImageResponse>("LLMAssistantUploadChatImage", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Chat image upload");
        return response;
    }

    /// <inheritdoc />
    public async Task<CountTokensResponse> CountTokensAsync(CountTokensRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Internal.Logger.LogDebug("Counting tokens");
        return await Internal.HttpClient.PostJsonAsync<CountTokensRequest, CountTokensResponse>("LLMAssistantCountTokens", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading LLM Assistant settings");
        return await Internal.HttpClient.PostJsonAsync<LLMSettingsResponse>("LLMAssistantGetSettings", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMSettingsResponse> SaveSettingsAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Internal.Logger.LogDebug("Saving LLM Assistant settings to '{Scope}' layer", request.Scope ?? "personal");
        LLMSettingsResponse response = await Internal.HttpClient.PostJsonAsync<SaveSettingsRequest, LLMSettingsResponse>("LLMAssistantSaveSettings", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Resetting LLM Assistant settings in '{Scope}' layer", scope ?? "personal");
        LLMSettingsResponse response = await Internal.HttpClient.PostJsonAsync<LLMSettingsResponse>("LLMAssistantResetSettings", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Reading LLM Assistant audit log");
        return await Internal.HttpClient.PostJsonAsync<AuditLogResponse>("LLMAssistantGetAuditLog", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuditLogResponse> SetAuditLogEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        JObject payload = new()
        {
            ["enabled"] = enabled
        };
        Internal.Logger.LogDebug("Setting LLM Assistant audit log enabled to {Enabled}", enabled);
        AuditLogResponse response = await Internal.HttpClient.PostJsonAsync<AuditLogResponse>("LLMAssistantSetAuditLogEnabled", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Audit log toggle");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing LLM models");
        LLMModelsResponse response = await Internal.HttpClient.PostJsonAsync<LLMModelsResponse>("LLMAssistantGetModels", payload: null, cancellationToken).ConfigureAwait(false);
        if (response.Warnings.Length > 0)
        {
            Internal.Logger.LogWarning("Retrieved {ModelCount} LLM model(s) with {WarningCount} provider warning(s): {Warnings}",
                response.Models.Count, response.Warnings.Length, string.Join("; ", response.Warnings));
        }
        else
        {
            Internal.Logger.LogInformation("Retrieved {ModelCount} LLM model(s)", response.Models.Count);
        }
        return response;
    }

    /// <inheritdoc />
    public async Task<UnloadModelsResponse> UnloadModelsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Unloading LLM models");
        UnloadModelsResponse response = await Internal.HttpClient.PostJsonAsync<UnloadModelsResponse>("LLMAssistantUnloadModels", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Unloaded models on {Freed}/{Providers} provider(s)", response.Freed, response.Providers);
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadListResponse> GetThreadsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing chat threads");
        ThreadListResponse response = await Internal.HttpClient.PostJsonAsync<ThreadListResponse>("LLMAssistantGetThreads", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {ThreadCount} chat thread(s)", response.Threads.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        Internal.Logger.LogDebug("Reading chat thread '{ThreadId}'", threadId);
        return await Internal.HttpClient.PostJsonAsync<ThreadResponse>("LLMAssistantGetThread", ThreadPayload(threadId), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        Internal.Logger.LogDebug("Deleting chat thread '{ThreadId}'", threadId);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteThread", ThreadPayload(threadId), cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Renaming chat thread '{ThreadId}'", threadId);
        ThreadResponse response = await Internal.HttpClient.PostJsonAsync<ThreadResponse>("LLMAssistantRenameThread", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Setting active leaf of thread '{ThreadId}' to message '{MessageId}'", threadId, messageId);
        ThreadResponse response = await Internal.HttpClient.PostJsonAsync<ThreadResponse>("LLMAssistantSetActiveLeaf", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Setting tool override on thread '{ThreadId}' to {Enabled}", threadId, enabled?.ToString() ?? "(inherit)");
        ThreadToolsEnabledResponse response = await Internal.HttpClient.PostJsonAsync<ThreadToolsEnabledResponse>("LLMAssistantSetThreadToolsEnabled", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Deleting message '{MessageId}' from thread '{ThreadId}'", messageId, threadId);
        ThreadResponse response = await Internal.HttpClient.PostJsonAsync<ThreadResponse>("LLMAssistantDeleteMessage", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Message delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<ThreadResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireThreadId(request.ThreadId);
        RequireMessageId(request.MessageId);
        Internal.Logger.LogDebug("Editing message '{MessageId}' in thread '{ThreadId}'", request.MessageId, request.ThreadId);
        ThreadResponse response = await Internal.HttpClient.PostJsonAsync<EditMessageRequest, ThreadResponse>("LLMAssistantEditMessage", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Exporting thread '{ThreadId}' as '{Format}'", threadId, format);
        ThreadExportResponse response = await Internal.HttpClient.PostJsonAsync<ThreadExportResponse>("LLMAssistantExportThread", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Thread export");
        return response;
    }

    /// <inheritdoc />
    public async Task<SessionStateResponse> GetSessionStateAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading LLM Assistant session state");
        return await Internal.HttpClient.PostJsonAsync<SessionStateResponse>("LLMAssistantGetSessionState", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionStateResponse> SetSessionStateAsync(JObject state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        JObject payload = new()
        {
            ["state"] = state
        };
        Internal.Logger.LogDebug("Patching LLM Assistant session state");
        SessionStateResponse response = await Internal.HttpClient.PostJsonAsync<SessionStateResponse>("LLMAssistantSetSessionState", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Session state patch");
        return response;
    }

    /// <inheritdoc />
    public async Task<AssetListResponse> GetAssetsAsync(string threadId, CancellationToken cancellationToken = default)
    {
        RequireThreadId(threadId);
        Internal.Logger.LogDebug("Listing assets for thread '{ThreadId}'", threadId);
        return await Internal.HttpClient.PostJsonAsync<AssetListResponse>("LLMAssistantGetAssets", ThreadPayload(threadId), cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Reading asset '{AssetId}' from thread '{ThreadId}'", assetId, threadId);
        return await Internal.HttpClient.PostJsonAsync<AssetResponse>("LLMAssistantGetAsset", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Deleting asset '{AssetId}' from thread '{ThreadId}'", assetId, threadId);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteAsset", payload, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Asset delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<InstructionListResponse> GetInstructionsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing instructions");
        return await Internal.HttpClient.PostJsonAsync<InstructionListResponse>("LLMAssistantGetInstructions", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveInstructionAsync(SaveInstructionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content cannot be null or empty", nameof(request));
        }
        Internal.Logger.LogDebug("Saving instruction '{Id}' to '{Scope}' layer", request.Id ?? "(new)", request.Scope ?? "personal");
        ScopedWriteResponse response = await Internal.HttpClient.PostJsonAsync<SaveInstructionRequest, ScopedWriteResponse>("LLMAssistantSaveInstruction", request, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Deleting instruction '{Id}'", id);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteInstruction", ScopedIdPayload("id", id, scope), cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Instruction delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<AssistantListResponse> GetAssistantsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing assistants");
        AssistantListResponse response = await Internal.HttpClient.PostJsonAsync<AssistantListResponse>("LLMAssistantGetAssistants", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {AssistantCount} assistant(s)", response.Assistants.Count);
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
        Internal.Logger.LogDebug("Reading assistant '{AssistantId}'", assistantId);
        return await Internal.HttpClient.PostJsonAsync<AssistantResponse>("LLMAssistantGetAssistant", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AssistantResponse> GetActiveAssistantAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading active assistant");
        return await Internal.HttpClient.PostJsonAsync<AssistantResponse>("LLMAssistantGetActiveAssistant", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveAssistantAsync(SaveAssistantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Assistant.HasValues)
        {
            throw new ArgumentException("Assistant data cannot be empty", nameof(request));
        }
        Internal.Logger.LogDebug("Saving assistant to '{Scope}' layer", request.Scope ?? "personal");
        ScopedWriteResponse response = await Internal.HttpClient.PostJsonAsync<SaveAssistantRequest, ScopedWriteResponse>("LLMAssistantSaveAssistant", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Assistant save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteAssistantAsync(string assistantId, string? scope = null, CancellationToken cancellationToken = default)
    {
        RequireAssistantId(assistantId);
        Internal.Logger.LogDebug("Deleting assistant '{AssistantId}'", assistantId);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteAssistant", ScopedIdPayload("assistantId", assistantId, scope), cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Setting active assistant to '{AssistantId}'", assistantId);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantSetActiveAssistant", payload, cancellationToken).ConfigureAwait(false);
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
        Internal.Logger.LogDebug("Uploading avatar for assistant '{AssistantId}'", request.AssistantId);
        UploadAssistantAvatarResponse response = await Internal.HttpClient.PostJsonAsync<UploadAssistantAvatarRequest, UploadAssistantAvatarResponse>("LLMAssistantUploadAssistantAvatar", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Avatar upload");
        return response;
    }

    /// <inheritdoc />
    public async Task<StarterTemplatesResponse> GetStarterTemplatesAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading starter assistant templates");
        return await Internal.HttpClient.PostJsonAsync<StarterTemplatesResponse>("LLMAssistantGetStarterTemplates", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ToolListResponse> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing tools");
        ToolListResponse response = await Internal.HttpClient.PostJsonAsync<ToolListResponse>("LLMAssistantGetTools", payload: null, cancellationToken).ConfigureAwait(false);
        Internal.Logger.LogInformation("Retrieved {ToolCount} tool(s)", response.Tools.Count);
        return response;
    }

    /// <inheritdoc />
    public async Task<ToolResponse> GetToolAsync(string toolId, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        Internal.Logger.LogDebug("Reading tool '{ToolId}'", toolId);
        return await Internal.HttpClient.PostJsonAsync<ToolResponse>("LLMAssistantGetTool", ToolPayload(toolId), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScopedWriteResponse> SaveToolAsync(SaveToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Tool.HasValues)
        {
            throw new ArgumentException("Tool data cannot be empty", nameof(request));
        }
        Internal.Logger.LogDebug("Saving tool to '{Scope}' layer", request.Scope ?? "personal");
        ScopedWriteResponse response = await Internal.HttpClient.PostJsonAsync<SaveToolRequest, ScopedWriteResponse>("LLMAssistantSaveTool", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool save");
        return response;
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> DeleteToolAsync(string toolId, string? scope = null, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        Internal.Logger.LogDebug("Deleting tool '{ToolId}'", toolId);
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantDeleteTool", ScopedIdPayload("toolId", toolId, scope), cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool delete");
        return response;
    }

    /// <inheritdoc />
    public async Task<ToolConfigResponse> GetToolConfigAsync(string toolId, CancellationToken cancellationToken = default)
    {
        RequireToolId(toolId);
        Internal.Logger.LogDebug("Reading config for tool '{ToolId}'", toolId);
        return await Internal.HttpClient.PostJsonAsync<ToolConfigResponse>("LLMAssistantGetToolConfig", ToolPayload(toolId), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ToolConfigResponse> SetToolConfigAsync(SetToolConfigRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireToolId(request.ToolId);
        Internal.Logger.LogDebug("Setting config for tool '{ToolId}'", request.ToolId);
        ToolConfigResponse response = await Internal.HttpClient.PostJsonAsync<SetToolConfigRequest, ToolConfigResponse>("LLMAssistantSetToolConfig", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool config save");
        return response;
    }

    /// <inheritdoc />
    public async Task<ExecuteToolResponse> ExecuteToolAsync(ExecuteToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireToolId(request.ToolId);
        Internal.Logger.LogDebug("Executing tool '{ToolId}'", request.ToolId);
        ExecuteToolResponse response = await Internal.HttpClient.PostJsonAsync<ExecuteToolRequest, ExecuteToolResponse>("LLMAssistantExecuteTool", request, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Tool execution");
        return response;
    }

    /// <inheritdoc />
    public async Task<ImagePresetsResponse> GetImagePresetsAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Listing image presets");
        return await Internal.HttpClient.PostJsonAsync<ImagePresetsResponse>("LLMAssistantGetImagePresets", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CompanionContextResponse> GetCompanionContextAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading companion context");
        return await Internal.HttpClient.PostJsonAsync<CompanionContextResponse>("LLMAssistantGetCompanionContext", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UserProfileResponse> GetUserProfileAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Reading user memory profile");
        return await Internal.HttpClient.PostJsonAsync<UserProfileResponse>("LLMAssistantGetUserProfile", payload: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LLMAssistantResponse> ClearUserProfileAsync(CancellationToken cancellationToken = default)
    {
        Internal.Logger.LogDebug("Clearing user memory profile");
        LLMAssistantResponse response = await Internal.HttpClient.PostJsonAsync<LLMAssistantResponse>("LLMAssistantClearUserProfile", payload: null, cancellationToken).ConfigureAwait(false);
        LogOutcome(response, "Memory profile clear");
        return response;
    }

    /// <summary>Streams one of the chat WebSocket endpoints.</summary>
    private IAsyncEnumerable<ChatStreamUpdate> StreamChatAsync(string endpoint, ChatStreamRequest request, CancellationToken cancellationToken)
    {
        return Internal.WebSocketClient.StreamMessagesAsync(endpoint, request, ParseChatUpdate, cancellationToken);
    }

    /// <summary>Parses a streamed chat frame, preserving the complete frame for fields without a typed member.</summary>
    private static ChatStreamUpdate ParseChatUpdate(JObject message)
    {
        ChatStreamUpdate update = message.ToObject<ChatStreamUpdate>() ?? new ChatStreamUpdate();
        update.Raw = message;
        return update;
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
            Internal.Logger.LogInformation("{Operation} succeeded", operation);
        }
        else
        {
            Internal.Logger.LogWarning("{Operation} failed: {Error}", operation, response.Error ?? "Unknown error");
        }
    }
}
