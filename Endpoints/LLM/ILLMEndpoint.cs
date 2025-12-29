using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Models.Responses;

namespace SwarmUI.ApiClient.Endpoints.LLM;

/// <summary>Provides access to SwarmUI LLM endpoints for text processing and enhancement.</summary>
/// <remarks>
/// NOTE: LLM endpoints are Hartsy-specific extensions and are not part of the standard SwarmUI distribution.
/// These endpoints require custom extensions to be installed and configured on the SwarmUI server.
///
/// LLM endpoints enable text enhancement via language models configured in SwarmUI,
/// such as prompt rewriting through the MagicPrompt extension.
/// </remarks>
public interface ILLMEndpoint
{
    /// <summary>Enhances a text prompt using the MagicPrompt extension.</summary>
    /// <param name="request">MagicPrompt request with text to enhance and model configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Enhanced text response from the LLM.</returns>
    /// <remarks>
    /// NOTE: This is a Hartsy-specific extension endpoint. Requires the Hartsy MagicPrompt extension
    /// to be installed and configured in SwarmUI. This endpoint does not exist in standard SwarmUI.
    /// The modelId must reference an LLM backend configured in SwarmUI settings.
    /// </remarks>
    Task<MagicPromptResponse> EnhancePromptAsync(MagicPromptRequest request, CancellationToken cancellationToken = default);
}
