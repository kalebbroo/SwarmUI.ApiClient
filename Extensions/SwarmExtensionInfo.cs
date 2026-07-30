using System.Collections.Generic;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Describes a SwarmUI server extension that this client exposes typed endpoints for.</summary>
/// <remarks>Extensions are installed on the SwarmUI server independently of this library. Calls to an extension endpoint fail when the corresponding extension is missing from the target server.</remarks>
public sealed class SwarmExtensionInfo
{
    /// <summary>Extension name as SwarmUI registers it.</summary>
    public required string Name { get; init; }

    /// <summary>Human readable name for logs and diagnostics.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Repository the extension is distributed from.</summary>
    public required string RepositoryUrl { get; init; }

    /// <summary>SwarmUI API endpoint names the extension adds to the server.</summary>
    public required IReadOnlyList<string> Endpoints { get; init; }
}
