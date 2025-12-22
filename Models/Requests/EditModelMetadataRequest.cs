using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Requests;

/// <summary>Request payload for SwarmUI's <c>EditModelMetadata</c> endpoint.</summary>
public class EditModelMetadataRequest
{
    /// <summary>Exact file path of the model to edit.</summary>
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>Model <c>title</c> metadata value.</summary>
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Model <c>author</c> metadata value.</summary>
    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>Model <c>architecture</c> metadata value (architecture ID).</summary>
    [JsonProperty("type")]
    public string Architecture { get; set; } = string.Empty;

    /// <summary>Model <c>description</c> metadata value.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Model <c>standard_width</c> metadata value.</summary>
    [JsonProperty("standard_width")]
    public int StandardWidth { get; set; }

    /// <summary>Model <c>standard_height</c> metadata value.</summary>
    [JsonProperty("standard_height")]
    public int StandardHeight { get; set; }

    /// <summary>Model <c>usage_hint</c> metadata value.</summary>
    [JsonProperty("usage_hint")]
    public string UsageHint { get; set; } = string.Empty;

    /// <summary>Model <c>date</c> metadata value.</summary>
    [JsonProperty("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>Model <c>license</c> metadata value.</summary>
    [JsonProperty("license")]
    public string License { get; set; } = string.Empty;

    /// <summary>Model <c>trigger_phrase</c> metadata value.</summary>
    [JsonProperty("trigger_phrase")]
    public string TriggerPhrase { get; set; } = string.Empty;

    /// <summary>Model <c>prediction_type</c> metadata value.</summary>
    [JsonProperty("prediction_type")]
    public string PredictionType { get; set; } = string.Empty;

    /// <summary>Model <c>tags</c> metadata value as a comma-separated list.</summary>
    [JsonProperty("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>Model <c>preview_image</c> metadata value as an image-data-string; null to leave unchanged.</summary>
    [JsonProperty("preview_image")]
    public string? PreviewImage { get; set; }

    /// <summary>Optional raw metadata text to inject into the preview image.</summary>
    [JsonProperty("preview_image_metadata")]
    public string? PreviewImageMetadata { get; set; }

    /// <summary>Model <c>is_negative_embedding</c> metadata flag.</summary>
    [JsonProperty("is_negative_embedding")]
    public bool IsNegativeEmbedding { get; set; }

    /// <summary>Model <c>lora_default_weight</c> metadata value.</summary>
    [JsonProperty("lora_default_weight")]
    public string LoraDefaultWeight { get; set; } = string.Empty;

    /// <summary>Model <c>lora_default_confinement</c> metadata value.</summary>
    [JsonProperty("lora_default_confinement")]
    public string LoraDefaultConfinement { get; set; } = string.Empty;

    /// <summary>Model subtype to apply the edit to (e.g., <c>Stable-Diffusion</c>, <c>LoRA</c>).</summary>
    [JsonProperty("subtype")]
    public string Subtype { get; set; } = "Stable-Diffusion";
}
