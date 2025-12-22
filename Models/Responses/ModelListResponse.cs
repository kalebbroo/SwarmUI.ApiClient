using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's ListModels API endpoint containing available models.</summary>
/// <remarks>Provides a folder hierarchy and detailed model entries that can be used to build either simple dropdowns or richer model browsers. See the SwarmUI ListModels documentation for filtering and sorting options.</remarks>
public class ModelListResponse
{
    /// <summary>Folder names at the requested path level, used for hierarchical model navigation.</summary>
    [JsonProperty("folders")]
    public List<string> Folders { get; set; } = new List<string>();

    /// <summary>Model files and their metadata at the requested path level.</summary>
    [JsonProperty("files")]
    public List<ModelInfo> Files { get; set; } = new List<ModelInfo>();
}

/// <summary>Detailed information about a specific model file in SwarmUI.</summary>
/// <remarks>Matches SwarmUI's model description format and can be extended with custom metadata fields as needed.</remarks>
public class ModelInfo
{
    /// <summary>Display name of the model, typically the filename without extension.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Model type/category identifier, such as "Stable-Diffusion", "LoRA", or "VAE".</summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Human-readable description of what the model does or contains.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Version identifier for this model release (semantic version, date string, or arbitrary tag).</summary>
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>File system path or URL to the model file.</summary>
    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>Timestamp when this model was initially created or added to SwarmUI.</summary>
    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    /// <summary>Timestamp when this model file or its metadata was last modified.</summary>
    [JsonProperty("date_modified")]
    public DateTime DateModified { get; set; }

    /// <summary>Extensible dictionary of additional model-specific metadata.</summary>
    [JsonProperty("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
