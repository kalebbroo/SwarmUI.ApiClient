using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Common;

/// <summary>Represents a real-time update message received during streaming generation.</summary>
/// <remarks>Used by WebSocket streaming APIs; the Type field acts as a discriminator for which properties are populated.</remarks>
public class GenerationUpdate
{
    /// <summary>Discriminator indicating which kind of update this message represents (status, progress, image, discard, error, keep_alive).</summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>Server status information for messages with Type == "status".</summary>
    [JsonProperty("status")]
    public StatusInfo? Status { get; set; }

    /// <summary>Backend GPU server status for messages with Type == "status".</summary>
    [JsonProperty("backend_status")]
    public BackendStatus? BackendStatus { get; set; }

    /// <summary>List of features supported by the server for messages with Type == "status".</summary>
    [JsonProperty("supported_features")]
    public List<string>? SupportedFeatures { get; set; }

    /// <summary>Real-time progress information for messages with Type == "progress".</summary>
    [JsonProperty("gen_progress")]
    public ProgressInfo? Progress { get; set; }

    /// <summary>Final generated image data and metadata for messages with Type == "image".</summary>
    [JsonProperty("image")]
    public ImageInfo? Image { get; set; }

    /// <summary>Batch indices to discard or mark as failed for messages with Type == "discard".</summary>
    [JsonProperty("discard_indices")]
    public List<int>? DiscardIndices { get; set; }

    /// <summary>Error information for messages with Type == "error".</summary>
    [JsonProperty("error")]
    public ErrorInfo? Error { get; set; }

    /// <summary>Keep-alive payload for messages with Type == "keep_alive".</summary>
    [JsonProperty("keep_alive")]
    public object? KeepAlive { get; set; }
}

/// <summary>Contains error message information when generation fails.</summary>
public class ErrorInfo
{
    /// <summary>Human-readable error message explaining what went wrong.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Provides overall server status including queue, model loading, and active generations.</summary>
public class StatusInfo
{
    /// <summary>Number of generation requests waiting in queue.</summary>
    [JsonProperty("waiting_gens")]
    public int WaitingGens { get; set; }

    /// <summary>Number of models currently loading into GPU memory.</summary>
    [JsonProperty("loading_models")]
    public int LoadingModels { get; set; }

    /// <summary>Number of backend GPU servers waiting to become available.</summary>
    [JsonProperty("waiting_backends")]
    public int WaitingBackends { get; set; }

    /// <summary>Number of generation jobs currently being processed across all backends.</summary>
    [JsonProperty("live_gens")]
    public int LiveGens { get; set; }
}

/// <summary>Provides status information about backend GPU servers.</summary>
public class BackendStatus
{
    /// <summary>Backend status string (e.g., "running", "loading", "error", "idle").</summary>
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>CSS-style class name for UI styling based on backend state.</summary>
    [JsonProperty("class")]
    public string Class { get; set; } = string.Empty;

    /// <summary>Human-readable message describing current backend activity.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Indicates whether any backend is currently loading a model.</summary>
    [JsonProperty("any_loading")]
    public bool AnyLoading { get; set; }
}

/// <summary>Provides real-time progress information during image generation.</summary>
/// <remarks>Used for updates with Type == "progress", including batch index, percentages, and optional preview image.</remarks>
public class ProgressInfo
{
    /// <summary>Batch index this progress update refers to (0-based).</summary>
    [JsonProperty("batch_index")]
    public string BatchIndex { get; set; } = string.Empty;

    /// <summary>Overall generation progress as a fraction between 0.0 and 1.0.</summary>
    [JsonProperty("overall_percent")]
    public float OverallPercent { get; set; }

    /// <summary>Current phase progress as a fraction between 0.0 and 1.0.</summary>
    [JsonProperty("current_percent")]
    public float CurrentPercent { get; set; }

    /// <summary>Optional base64-encoded preview image (data URI); may be null for early updates.</summary>
    [JsonProperty("preview")]
    public string? Preview { get; set; }
}

/// <summary>Contains final generated image data and metadata for a completed batch image.</summary>
/// <remarks>Used for updates with Type == "image"; BatchSize > 1 yields one ImageInfo per image.</remarks>
public class ImageInfo
{
    /// <summary>Base64-encoded data URI for the final image (e.g., "data:image/jpeg;base64,...").</summary>
    [JsonProperty("image")]
    public string Image { get; set; } = string.Empty;

    /// <summary>Batch index identifying which slot in the batch this image corresponds to.</summary>
    [JsonProperty("batch_index")]
    public string BatchIndex { get; set; } = string.Empty;

    /// <summary>JSON-encoded metadata string containing generation parameters in SwarmUI's metadata format.</summary>
    [JsonProperty("metadata")]
    public string Metadata { get; set; } = string.Empty;

    /// <summary>Parses the Metadata JSON string into a SwarmUIMetadata object.</summary>
    /// <returns>Parsed metadata, or null if Metadata is empty or invalid.</returns>
    public SwarmUIMetadata? GetParsedMetadata()
    {
        if (string.IsNullOrEmpty(Metadata))
        {
            return null;
        }
        return SwarmUIMetadata.FromJson(Metadata);
    }
}
