using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Common;

/// <summary>Represents a real-time update message received during streaming generation.</summary>
/// <remarks>The Type field acts as a discriminator for which properties are populated. Emitted values: "status", "progress", "image", "discard", "error", "complete". Every stream ends with exactly one "complete" update carrying a <see cref="CompletionInfo"/>; server keep-alive pings are consumed by the transport and never surface here.</remarks>
public class GenerationUpdate
{
    /// <summary>Discriminator indicating which kind of update this message represents (status, progress, image, discard, error, complete).</summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>Server status information for messages with Type == "status". Counters are scoped to this session, not the whole server.</summary>
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

    /// <summary>Batch indices to discard or mark as failed for messages with Type == "discard". May be empty.</summary>
    [JsonProperty("discard_indices")]
    public List<int>? DiscardIndices { get; set; }

    /// <summary>Error information for messages with Type == "error". Error updates end the batch but not necessarily the stream; a "complete" update still follows.</summary>
    [JsonProperty("error")]
    public ErrorInfo? Error { get; set; }

    /// <summary>Terminal summary for messages with Type == "complete". Always the final update of a stream.</summary>
    [JsonProperty("completion")]
    public CompletionInfo? Completion { get; set; }
}

/// <summary>Terminal summary of a generation stream.</summary>
public class CompletionInfo
{
    /// <summary>True when at least one image was produced and no error frames were received.</summary>
    [JsonProperty("succeeded")]
    public bool Succeeded { get; set; }

    /// <summary>Number of image updates emitted during the stream (including grid composites and intermediates, which carry negative batch indices).</summary>
    [JsonProperty("images_received")]
    public int ImagesReceived { get; set; }

    /// <summary>Batch indices the server marked as discarded, aggregated across the stream.</summary>
    [JsonProperty("discarded_indices")]
    public List<int> DiscardedIndices { get; set; } = [];

    /// <summary>All errors reported during the stream, in order.</summary>
    [JsonProperty("errors")]
    public List<ErrorInfo> Errors { get; set; } = [];
}

/// <summary>Contains error message information when generation fails.</summary>
public class ErrorInfo
{
    /// <summary>Human-readable error message explaining what went wrong.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Machine-readable error id when the server provided one (e.g. "invalid_session_id"); null for plain generation errors.</summary>
    [JsonProperty("error_id")]
    public string? ErrorId { get; set; }
}

/// <summary>Provides session-scoped status including queue, model loading, and active generations.</summary>
public class StatusInfo
{
    /// <summary>Number of generation requests waiting in queue for this session.</summary>
    [JsonProperty("waiting_gens")]
    public int WaitingGens { get; set; }

    /// <summary>Number of models currently loading into GPU memory.</summary>
    [JsonProperty("loading_models")]
    public int LoadingModels { get; set; }

    /// <summary>Number of backend GPU servers waiting to become available.</summary>
    [JsonProperty("waiting_backends")]
    public int WaitingBackends { get; set; }

    /// <summary>Number of generation jobs currently being processed for this session.</summary>
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
    /// <summary>Batch index this progress update refers to.</summary>
    [JsonProperty("batch_index")]
    public string BatchIndex { get; set; } = string.Empty;

    /// <summary>Overall generation progress as a fraction between 0.0 and 1.0.</summary>
    [JsonProperty("overall_percent")]
    public float OverallPercent { get; set; }

    /// <summary>Current phase progress as a fraction between 0.0 and 1.0.</summary>
    [JsonProperty("current_percent")]
    public float CurrentPercent { get; set; }

    /// <summary>Optional base64-encoded preview image (data URI); may be null when no preview is available.</summary>
    [JsonProperty("preview")]
    public string? Preview { get; set; }
}

/// <summary>Contains final generated image data and metadata for a completed image.</summary>
/// <remarks>Used for updates with Type == "image".</remarks>
public class ImageInfo
{
    /// <summary>The image, as either a server-relative path (e.g. "View/user/raw/2026-08/img.png" or "Output/img.png") or a base64 data URL ("data:image/png;base64,...").</summary>
    /// <remarks>Which form arrives depends on the request's donotsave flag AND server-side user settings — handle both at all times. Join relative paths against the server base URL to download.</remarks>
    [JsonProperty("image")]
    public string Image { get; set; } = string.Empty;

    /// <summary>Batch index identifying which slot this image corresponds to. String-typed by the server; may be "-1" (auto-generated grid composite) or "-10" and below (intermediate/non-real outputs).</summary>
    [JsonProperty("batch_index")]
    public string BatchIndex { get; set; } = string.Empty;

    /// <summary>Request id the server attached to this image, when present.</summary>
    [JsonProperty("request_id")]
    public string? RequestId { get; set; }

    /// <summary>JSON-encoded metadata string containing generation parameters in SwarmUI's metadata format. May be null or empty.</summary>
    [JsonProperty("metadata")]
    public string? Metadata { get; set; }

    /// <summary>True when <see cref="Image"/> is a base64 data URL rather than a server-relative file path.</summary>
    [JsonIgnore]
    public bool IsDataUrl => Image.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

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
