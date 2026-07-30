using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts;

namespace SwarmUI.ApiClient.Extensions.MagicPrompt;

/// <summary>Provides access to the endpoints added by the MagicPrompt SwarmUI extension.</summary>
/// <remarks>Requires the MagicPrompt extension to be installed and configured on the target SwarmUI server. None of these endpoints exist in stock SwarmUI.</remarks>
public interface IMagicPromptEndpoint : ISwarmExtensionEndpoint
{
    /// <summary>Enhances a text prompt using an LLM backend configured in MagicPrompt.</summary>
    /// <param name="request">MagicPrompt request with the text to enhance and model configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Enhanced text response from the LLM.</returns>
    /// <remarks>Calls the <c>MagicPromptPhoneHome</c> endpoint. The model id must reference an LLM backend configured in the MagicPrompt settings.</remarks>
    Task<MagicPromptResponse> EnhancePromptAsync(MagicPromptRequest request, CancellationToken cancellationToken = default);
}
