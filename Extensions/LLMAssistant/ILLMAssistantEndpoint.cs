using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant;

/// <summary>Provides access to the endpoints added by the LLM Assistant SwarmUI extension.</summary>
/// <remarks>Requires the LLM Assistant extension to be installed on the target SwarmUI server. None of these endpoints exist in stock SwarmUI.
///
/// The extension registers "LLM" as a SwarmUI model type, so listing LLM model files works through the stock <see cref="ISwarmClient.Models"/> endpoint with the LLM subtype. Use <see cref="GetModelsAsync"/> instead when you need the models a live LLM provider is actually serving.
///
/// Writes that touch shared configuration accept a scope of "personal" or "shared"; shared requires the server side <c>llm_shared_write</c> permission and is rejected without it.</remarks>
public interface ILLMAssistantEndpoint : ISwarmExtensionEndpoint
{
    /// <summary>Runs a one-shot completion without touching chat threads.</summary>
    /// <param name="request">Completion request. Message is required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Raw model output.</returns>
    /// <remarks>Calls the <c>LLMAssistantSendMessage</c> endpoint. Results are cached by prompt and instruction unless the request opts out.</remarks>
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sends a message into a thread and streams the reply.</summary>
    /// <param name="request">Stream request. Thread identifier is required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Streamed response frames.</returns>
    /// <remarks>Streams the <c>LLMAssistantSendMessageWS</c> endpoint. The server appends the user message, streams the reply, and persists it, so the thread stays the source of truth.</remarks>
    IAsyncEnumerable<ChatStreamUpdate> StreamMessageAsync(ChatStreamRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edits a message into a new branch and streams the resulting reply.</summary>
    /// <param name="request">Stream request. Thread identifier, message identifier, and content are required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Streamed response frames.</returns>
    /// <remarks>Streams the <c>LLMAssistantEditMessageWS</c> endpoint. The original message is preserved as a sibling branch rather than overwritten.</remarks>
    IAsyncEnumerable<ChatStreamUpdate> StreamEditMessageAsync(ChatStreamRequest request, CancellationToken cancellationToken = default);

    /// <summary>Regenerates an assistant reply as a new branch and streams it.</summary>
    /// <param name="request">Stream request. Thread identifier and the message identifier of the reply to regenerate are required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Streamed response frames.</returns>
    /// <remarks>Streams the <c>LLMAssistantRegenerateWS</c> endpoint. The previous reply stays switchable through the branch pager.</remarks>
    IAsyncEnumerable<ChatStreamUpdate> StreamRegenerateAsync(ChatStreamRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a chat thread.</summary>
    /// <param name="assistantId">Assistant to bind the thread to. Falls back to the caller's active assistant when null.</param>
    /// <param name="title">Initial title. The server generates one later when null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created thread.</returns>
    /// <remarks>Calls the <c>LLMAssistantCreateThread</c> endpoint. Create a thread before the first streamed message in a new conversation.</remarks>
    Task<ThreadResponse> CreateThreadAsync(string? assistantId = null, string? title = null, CancellationToken cancellationToken = default);

    /// <summary>Test-runs an unsaved instruction against a sample message.</summary>
    /// <param name="request">Instruction text and sample input, both required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The model's response to the sample input.</returns>
    /// <remarks>Calls the <c>LLMAssistantTestInstruction</c> endpoint. Nothing is persisted.</remarks>
    Task<ChatCompletionResponse> TestInstructionAsync(TestInstructionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Uploads an image attached to a chat message.</summary>
    /// <param name="request">Thread, message, and data URI, all required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored image's URL and dimensions.</returns>
    /// <remarks>Calls the <c>LLMAssistantUploadChatImage</c> endpoint. Store the returned URL on the message rather than the base64 payload.</remarks>
    Task<UploadChatImageResponse> UploadChatImageAsync(UploadChatImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Counts tokens for text or a message list.</summary>
    /// <param name="request">Text or messages to count.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Token count and whether it came from a real tokenizer.</returns>
    /// <remarks>Calls the <c>LLMAssistantCountTokens</c> endpoint.</remarks>
    Task<CountTokensResponse> CountTokensAsync(CountTokensRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's effective settings.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Merged settings and whether the caller may write the shared layer.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetSettings</c> endpoint.</remarks>
    Task<LLMSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Patches settings into the personal or shared layer.</summary>
    /// <param name="request">Settings to merge and the target scope.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resulting settings and the layer written.</returns>
    /// <remarks>Calls the <c>LLMAssistantSaveSettings</c> endpoint. The server ignores assistant and tool dictionaries in the payload.</remarks>
    Task<LLMSettingsResponse> SaveSettingsAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Resets settings for a layer.</summary>
    /// <param name="scope">Layer to reset. Null or "personal" clears the caller's overrides; "shared" restores factory defaults and requires the <c>llm_shared_write</c> permission.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resulting settings.</returns>
    /// <remarks>Calls the <c>LLMAssistantResetSettings</c> endpoint.</remarks>
    Task<LLMSettingsResponse> ResetSettingsAsync(string? scope = null, CancellationToken cancellationToken = default);

    /// <summary>Reads the shared-write audit log.</summary>
    /// <param name="max">Maximum entries to return. The server clamps this to 1-5000 and defaults to 200.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether logging is enabled, plus the most recent entries.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetAuditLog</c> endpoint. Requires the <c>llm_shared_write</c> permission.</remarks>
    Task<AuditLogResponse> GetAuditLogAsync(int? max = null, CancellationToken cancellationToken = default);

    /// <summary>Enables or disables audit logging.</summary>
    /// <param name="enabled">Whether logging should be on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resulting enabled state.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetAuditLogEnabled</c> endpoint. Requires the <c>llm_shared_write</c> permission and persists across restarts.</remarks>
    Task<AuditLogResponse> SetAuditLogEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Lists the models every registered LLM provider is serving.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Available models, plus a warning per provider that failed to respond.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetModels</c> endpoint. Check the warnings before treating the list as complete.</remarks>
    Task<LLMModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Unloads resident LLM models to free memory.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>How many providers freed something.</returns>
    /// <remarks>Calls the <c>LLMAssistantUnloadModels</c> endpoint. Useful before loading an image model.</remarks>
    Task<UnloadModelsResponse> UnloadModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the caller's chat threads.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The thread index.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetThreads</c> endpoint. Returns summaries rather than full thread blobs.</remarks>
    Task<ThreadListResponse> GetThreadsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one chat thread in full.</summary>
    /// <param name="threadId">Thread to read. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The thread blob.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetThread</c> endpoint.</remarks>
    Task<ThreadResponse> GetThreadAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a chat thread.</summary>
    /// <param name="threadId">Thread to delete. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the delete succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteThread</c> endpoint.</remarks>
    Task<LLMAssistantResponse> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>Renames a chat thread.</summary>
    /// <param name="threadId">Thread to rename. Required.</param>
    /// <param name="title">New title. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated thread.</returns>
    /// <remarks>Calls the <c>LLMAssistantRenameThread</c> endpoint. A manual rename is permanent: the automatic title generator will not overwrite it.</remarks>
    Task<ThreadResponse> RenameThreadAsync(string threadId, string title, CancellationToken cancellationToken = default);

    /// <summary>Points a thread's active branch at a specific message.</summary>
    /// <param name="threadId">Thread to update. Required.</param>
    /// <param name="messageId">Message to make the active leaf. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated thread.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetActiveLeaf</c> endpoint. This moves the active leaf rather than copying the thread, so the rendered conversation becomes the path to that message.</remarks>
    Task<ThreadResponse> SetActiveLeafAsync(string threadId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears a thread's tool-calling override.</summary>
    /// <param name="threadId">Thread to update. Required.</param>
    /// <param name="enabled">Override state, or null to clear the override so the thread inherits the assistant's default.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resulting override state.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetThreadToolsEnabled</c> endpoint.</remarks>
    Task<ThreadToolsEnabledResponse> SetThreadToolsEnabledAsync(string threadId, bool? enabled, CancellationToken cancellationToken = default);

    /// <summary>Deletes a message from a thread.</summary>
    /// <param name="threadId">Thread containing the message. Required.</param>
    /// <param name="messageId">Message to delete. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated thread.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteMessage</c> endpoint.</remarks>
    Task<ThreadResponse> DeleteMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>Rewrites a stored message in place.</summary>
    /// <param name="request">Thread, message, and replacement content.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated thread.</returns>
    /// <remarks>Calls the <c>LLMAssistantEditMessage</c> endpoint. To edit and regenerate as a new branch, use <see cref="StreamEditMessageAsync"/> instead.</remarks>
    Task<ThreadResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exports a thread as JSON or markdown.</summary>
    /// <param name="threadId">Thread to export. Required.</param>
    /// <param name="format">Either "json" (default) or "markdown".</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The exported content and a suggested filename.</returns>
    /// <remarks>Calls the <c>LLMAssistantExportThread</c> endpoint.</remarks>
    Task<ThreadExportResponse> ExportThreadAsync(string threadId, string format = "json", CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's session state.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The session state blob.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetSessionState</c> endpoint.</remarks>
    Task<SessionStateResponse> GetSessionStateAsync(CancellationToken cancellationToken = default);

    /// <summary>Merges a patch into the caller's session state.</summary>
    /// <param name="state">Patch to merge. Send a null value for a key to clear it.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resulting session state.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetSessionState</c> endpoint.</remarks>
    Task<SessionStateResponse> SetSessionStateAsync(JObject state, CancellationToken cancellationToken = default);

    /// <summary>Lists a thread's assets.</summary>
    /// <param name="threadId">Thread to read. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The thread's assets, each with full content.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetAssets</c> endpoint.</remarks>
    Task<AssetListResponse> GetAssetsAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>Reads one asset from a thread.</summary>
    /// <param name="threadId">Thread containing the asset. Required.</param>
    /// <param name="assetId">Asset to read. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The asset.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetAsset</c> endpoint.</remarks>
    Task<AssetResponse> GetAssetAsync(string threadId, string assetId, CancellationToken cancellationToken = default);

    /// <summary>Deletes an asset from a thread.</summary>
    /// <param name="threadId">Thread containing the asset. Required.</param>
    /// <param name="assetId">Asset to delete. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the delete succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteAsset</c> endpoint. Message content is left untouched, so any inline reference to the asset remains.</remarks>
    Task<LLMAssistantResponse> DeleteAssetAsync(string threadId, string assetId, CancellationToken cancellationToken = default);

    /// <summary>Lists the instructions visible to the caller.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Visible instructions.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetInstructions</c> endpoint.</remarks>
    Task<InstructionListResponse> GetInstructionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates an instruction.</summary>
    /// <param name="request">Instruction to store. Content is required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored identifier and the layer written.</returns>
    /// <remarks>Calls the <c>LLMAssistantSaveInstruction</c> endpoint.</remarks>
    Task<ScopedWriteResponse> SaveInstructionAsync(SaveInstructionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a custom instruction.</summary>
    /// <param name="id">Instruction to delete. Required.</param>
    /// <param name="scope">Layer to delete from. Auto-detected when null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the delete succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteInstruction</c> endpoint.</remarks>
    Task<LLMAssistantResponse> DeleteInstructionAsync(string id, string? scope = null, CancellationToken cancellationToken = default);

    /// <summary>Lists the assistants visible to the caller.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Visible assistants and the caller's active assistant.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetAssistants</c> endpoint.</remarks>
    Task<AssistantListResponse> GetAssistantsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one assistant.</summary>
    /// <param name="assistantId">Assistant to read. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The assistant definition.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetAssistant</c> endpoint.</remarks>
    Task<AssistantResponse> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's resolved active assistant.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The active assistant and its identifier.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetActiveAssistant</c> endpoint.</remarks>
    Task<AssistantResponse> GetActiveAssistantAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates an assistant.</summary>
    /// <param name="request">Assistant to store and the target scope.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored identifier and the layer written.</returns>
    /// <remarks>Calls the <c>LLMAssistantSaveAssistant</c> endpoint.</remarks>
    Task<ScopedWriteResponse> SaveAssistantAsync(SaveAssistantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an assistant.</summary>
    /// <param name="assistantId">Assistant to delete. Required. The default assistant cannot be deleted.</param>
    /// <param name="scope">Layer to delete from. Auto-detected when null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the delete succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteAssistant</c> endpoint.</remarks>
    Task<LLMAssistantResponse> DeleteAssistantAsync(string assistantId, string? scope = null, CancellationToken cancellationToken = default);

    /// <summary>Sets the caller's active assistant.</summary>
    /// <param name="assistantId">Assistant to activate. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the change succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetActiveAssistant</c> endpoint. This is always a personal preference, never a shared write.</remarks>
    Task<LLMAssistantResponse> SetActiveAssistantAsync(string assistantId, CancellationToken cancellationToken = default);

    /// <summary>Uploads an assistant avatar.</summary>
    /// <param name="request">Assistant and avatar data URI, both required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored avatar URL.</returns>
    /// <remarks>Calls the <c>LLMAssistantUploadAssistantAvatar</c> endpoint. Write the returned URL to the assistant's avatar field.</remarks>
    Task<UploadAssistantAvatarResponse> UploadAssistantAvatarAsync(UploadAssistantAvatarRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads the bundled starter assistant templates.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Bundled templates.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetStarterTemplates</c> endpoint.</remarks>
    Task<StarterTemplatesResponse> GetStarterTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the tools visible to the caller.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Visible tool definitions.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetTools</c> endpoint.</remarks>
    Task<ToolListResponse> GetToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one tool definition.</summary>
    /// <param name="toolId">Tool to read. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tool definition.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetTool</c> endpoint.</remarks>
    Task<ToolResponse> GetToolAsync(string toolId, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a tool definition.</summary>
    /// <param name="request">Tool to store and the target scope.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored identifier and the layer written.</returns>
    /// <remarks>Calls the <c>LLMAssistantSaveTool</c> endpoint.</remarks>
    Task<ScopedWriteResponse> SaveToolAsync(SaveToolRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a custom tool.</summary>
    /// <param name="toolId">Tool to delete. Required.</param>
    /// <param name="scope">Layer to delete from. Auto-detected when null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the delete succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantDeleteTool</c> endpoint. Built-in tools cannot be deleted.</remarks>
    Task<LLMAssistantResponse> DeleteToolAsync(string toolId, string? scope = null, CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's configuration for a tool.</summary>
    /// <param name="toolId">Tool to read configuration for. Required.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tool's configuration block.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetToolConfig</c> endpoint.</remarks>
    Task<ToolConfigResponse> GetToolConfigAsync(string toolId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the caller's configuration for a tool.</summary>
    /// <param name="request">Tool and replacement configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stored configuration.</returns>
    /// <remarks>Calls the <c>LLMAssistantSetToolConfig</c> endpoint.</remarks>
    Task<ToolConfigResponse> SetToolConfigAsync(SetToolConfigRequest request, CancellationToken cancellationToken = default);

    /// <summary>Invokes a tool directly, bypassing the model.</summary>
    /// <param name="request">Tool identifier and arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tool's result.</returns>
    /// <remarks>Calls the <c>LLMAssistantExecuteTool</c> endpoint. The per-tool <c>llm_tool_*</c> permission is still enforced.</remarks>
    Task<ExecuteToolResponse> ExecuteToolAsync(ExecuteToolRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists the caller's text-to-image presets as the assistant sees them.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Presets with the one-line summary the model uses when selecting one.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetImagePresets</c> endpoint.</remarks>
    Task<ImagePresetsResponse> GetImagePresetsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the context the companion overlay needs.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The caller's most recently generated image, or null when they have none.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetCompanionContext</c> endpoint. Requires the <c>llm_companion</c> permission.</remarks>
    Task<CompanionContextResponse> GetCompanionContextAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's memory profile.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The memory profile, or an empty template when unset.</returns>
    /// <remarks>Calls the <c>LLMAssistantGetUserProfile</c> endpoint.</remarks>
    Task<UserProfileResponse> GetUserProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the caller's memory profile.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether the clear succeeded.</returns>
    /// <remarks>Calls the <c>LLMAssistantClearUserProfile</c> endpoint. This is irreversible.</remarks>
    Task<LLMAssistantResponse> ClearUserProfileAsync(CancellationToken cancellationToken = default);
}
