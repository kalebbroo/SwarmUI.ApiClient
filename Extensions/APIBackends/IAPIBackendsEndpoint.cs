using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Extensions.APIBackends.Contracts;

namespace SwarmUI.ApiClient.Extensions.APIBackends;

/// <summary>Provides access to the endpoints added by the API-Backends SwarmUI extension.</summary>
/// <remarks>Requires the SwarmUI-API-Backends extension on the target server. None of these endpoints exist in stock SwarmUI.</remarks>
public interface IAPIBackendsEndpoint : ISwarmExtensionEndpoint
{
    /// <summary>Lists what every API-backed model accepts: its provider family, modality, whether it takes an init image, whether it batches, and its feature flags.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Capabilities keyed by full model name.</returns>
    /// <remarks>Calls <c>APIBackendsListModelCapabilities</c>, which the server gates behind a view-capabilities permission.</remarks>
    Task<ModelCapabilitiesResponse> ListModelCapabilitiesAsync(CancellationToken cancellationToken = default);
}
