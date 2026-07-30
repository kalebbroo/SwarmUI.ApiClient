namespace SwarmUI.ApiClient.Extensions;

/// <summary>Marks an endpoint group as being backed by a SwarmUI server extension rather than the stock SwarmUI API.</summary>
/// <remarks>Every endpoint under <c>SwarmUI.ApiClient.Extensions</c> implements this interface so consumers can identify, log, and gate extension dependent features at runtime.</remarks>
public interface ISwarmExtensionEndpoint
{
    /// <summary>Metadata for the SwarmUI extension that must be installed on the server for this endpoint to work.</summary>
    SwarmExtensionInfo Extension { get; }
}
