using System;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Represents the result of a client-side health check against a SwarmUI server.</summary>
/// <remarks>Indicates whether a simple session-creation probe succeeded and how long it took. Typical uses include pre-flight validation, basic monitoring, and diagnostics. See the library documentation for full usage examples.</remarks>
public class HealthCheckResponse
{
    /// <summary>Indicates whether the SwarmUI server is healthy and reachable.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Time taken to perform the health check operation, including network and server processing time.</summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>Error message if the health check failed; null when <see cref="IsHealthy"/> is true.</summary>
    public string? Error { get; set; }

    /// <summary>Optional SwarmUI server version information, when available.</summary>
    public string? ServerVersion { get; set; }
}
