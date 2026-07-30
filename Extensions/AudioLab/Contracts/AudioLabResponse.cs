using System;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Fields shared by every AudioLab response, covering both the standardized success and error envelopes the extension emits.</summary>
public class AudioLabResponse
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>Human readable message describing the outcome, when the server supplies one.</summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    /// <summary>Error message when the operation failed.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>Machine readable error code for categorizing failures, for example "missing_audio" or "no_provider".</summary>
    [JsonProperty("error_code")]
    public string? ErrorCode { get; set; }

    /// <summary>Exception type name when the server caught an unexpected exception.</summary>
    [JsonProperty("error_type")]
    public string? ErrorType { get; set; }

    /// <summary>Server timestamp for the response.</summary>
    [JsonProperty("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>Server side processing duration in seconds.</summary>
    [JsonProperty("processing_time")]
    public double ProcessingTime { get; set; }
}
