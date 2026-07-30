using System.Collections.Generic;
using SwarmUI.ApiClient.Extensions.MagicPrompt;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Groups every endpoint that depends on a SwarmUI server extension, keeping them separated from the stock SwarmUI API exposed directly on <see cref="ISwarmClient"/>.</summary>
/// <remarks>Each property maps to one extension and requires that extension to be installed on the target SwarmUI server. See <c>Extensions/README.md</c> for the supported extension list.</remarks>
public interface ISwarmExtensions
{
    /// <summary>Access to the MagicPrompt extension for LLM backed prompt enhancement.</summary>
    IMagicPromptEndpoint MagicPrompt { get; }

    /// <summary>Metadata for every extension this client supports, for diagnostics and capability reporting.</summary>
    IReadOnlyList<SwarmExtensionInfo> All { get; }
}
