using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Responses;

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

/// <summary>One model file as SwarmUI describes it.</summary>
/// <remarks><c>ListModels</c>, <c>DescribeModel</c> and <c>ListLoadedModels</c> all return this same flat object
/// (server-side <c>T2IModel.ToNetObject</c>), so every property here is populated by all three. Fields the server
/// adds later are still reachable through <see cref="ExtensionData"/> rather than being dropped.</remarks>
public class ModelInfo
{
    /// <summary>Path-style model name relative to its subtype folder, including the file extension — for example <c>"SDXL/sd_xl_base_1.0.safetensors"</c>.</summary>
    /// <remarks>Parameter dropdowns in <c>ListT2IParams</c> list the same models with the extension stripped, so a
    /// name taken from here does not always match a value taken from there.</remarks>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Display title from the model's metadata. Null when the model has no title set.</summary>
    [JsonProperty("title")]
    public string? Title { get; set; }

    /// <summary>Model author from the model's metadata.</summary>
    [JsonProperty("author")]
    public string? Author { get; set; }

    /// <summary>Description text from the model's metadata.</summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>Preview image, either a <c>data:</c> URL or a server path under <c>/ViewSpecial/</c>.</summary>
    [JsonProperty("preview_image")]
    public string? PreviewImage { get; set; }

    /// <summary>True when at least one backend currently holds this model in memory.</summary>
    [JsonProperty("loaded")]
    public bool Loaded { get; set; }

    /// <summary>Precise architecture id, for example <c>"flux.2-klein-9b/lora"</c>. Null when the model class could not be identified.</summary>
    /// <remarks>Narrower than <see cref="CompatClass"/>: two models can share a compatibility class while having different architecture ids. Use <see cref="CompatClass"/> for compatibility decisions.</remarks>
    [JsonProperty("architecture")]
    public string? Architecture { get; set; }

    /// <summary>Human-readable name of the model class, for example <c>"Stable Diffusion XL (Base)"</c>.</summary>
    [JsonProperty("class")]
    public string? ModelClass { get; set; }

    /// <summary>Compatibility class id, for example <c>"flux-1"</c>.</summary>
    /// <remarks>This is what decides whether an adapter fits a base model: SwarmUI's own UI treats a LoRA, VAE or
    /// ControlNet as compatible when its <c>compat_class</c> equals the base model's. Comparing
    /// <see cref="Architecture"/> instead rejects valid pairings, because variants of one lineage share a
    /// compatibility class but not an architecture id.</remarks>
    [JsonProperty("compat_class")]
    public string? CompatClass { get; set; }

    /// <summary>Native resolution as <c>"{width}x{height}"</c>. <see cref="StandardWidth"/> and <see cref="StandardHeight"/> carry the same numbers already parsed.</summary>
    [JsonProperty("resolution")]
    public string? Resolution { get; set; }

    /// <summary>Native width in pixels, falling back to the model class default. 0 when unknown.</summary>
    [JsonProperty("standard_width")]
    public int StandardWidth { get; set; }

    /// <summary>Native height in pixels, falling back to the model class default. 0 when unknown.</summary>
    [JsonProperty("standard_height")]
    public int StandardHeight { get; set; }

    /// <summary>License string from the model's metadata.</summary>
    [JsonProperty("license")]
    public string? License { get; set; }

    /// <summary>Release date from the model's metadata, as a free-form string rather than a parsed date.</summary>
    [JsonProperty("date")]
    public string? Date { get; set; }

    /// <summary>Prediction type from the model's metadata, for example <c>"epsilon"</c> or <c>"v-prediction"</c>.</summary>
    [JsonProperty("prediction_type")]
    public string? PredictionType { get; set; }

    /// <summary>Usage guidance the model's author attached to it.</summary>
    [JsonProperty("usage_hint")]
    public string? UsageHint { get; set; }

    /// <summary>Text that must appear in the prompt to activate this model, for LoRAs and embeddings that need one.</summary>
    [JsonProperty("trigger_phrase")]
    public string? TriggerPhrase { get; set; }

    /// <summary>Models this one was merged from, when recorded.</summary>
    [JsonProperty("merged_from")]
    public string? MergedFrom { get; set; }

    /// <summary>Free-form tags from the model's metadata.</summary>
    [JsonProperty("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>True when SwarmUI recognizes the file format and can load it.</summary>
    [JsonProperty("is_supported_model_format")]
    public bool IsSupportedModelFormat { get; set; }

    /// <summary>True when this embedding is meant for the negative prompt.</summary>
    [JsonProperty("is_negative_embedding")]
    public bool IsNegativeEmbedding { get; set; }

    /// <summary>Preferred LoRA weight as a string, empty when the model does not specify one.</summary>
    [JsonProperty("lora_default_weight")]
    public string LoraDefaultWeight { get; set; } = string.Empty;

    /// <summary>Preferred LoRA section confinement as a string, empty when the model does not specify one.</summary>
    [JsonProperty("lora_default_confinement")]
    public string LoraDefaultConfinement { get; set; } = string.Empty;

    /// <summary>True when the model file is present on the server rather than remote-only.</summary>
    [JsonProperty("local")]
    public bool Local { get; set; }

    /// <summary>File creation time as Unix milliseconds. 0 when unknown.</summary>
    [JsonProperty("time_created")]
    public long TimeCreated { get; set; }

    /// <summary>File modification time as Unix milliseconds. 0 when unknown.</summary>
    [JsonProperty("time_modified")]
    public long TimeModified { get; set; }

    /// <summary>Model file hash, empty when SwarmUI has not hashed it yet.</summary>
    [JsonProperty("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>Same value as <see cref="Hash"/>; the server sends both names.</summary>
    [JsonProperty("hash_sha256")]
    public string HashSha256 { get; set; } = string.Empty;

    /// <summary>Special storage format such as a quantization marker, empty for ordinary files.</summary>
    [JsonProperty("special_format")]
    public string SpecialFormat { get; set; } = string.Empty;

    /// <summary>Any field the server sends that this type does not map, so a newer server loses nothing.</summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? ExtensionData { get; set; }
}
