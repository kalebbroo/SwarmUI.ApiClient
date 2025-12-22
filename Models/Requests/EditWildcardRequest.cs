using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Requests;

/// <summary>Request payload for SwarmUI's <c>EditWildcard</c> endpoint, used to modify a wildcard file and optional preview.</summary>
public class EditWildcardRequest
{
    /// <summary>Exact file path of the wildcard card to edit.</summary>
    [JsonProperty("card")]
    public string Card { get; set; } = string.Empty;

    /// <summary>Newline-separated list of wildcard options.</summary>
    [JsonProperty("options")]
    public string Options { get; set; } = string.Empty;

    /// <summary>Optional preview image in image-data-string format; null to leave unchanged.</summary>
    [JsonProperty("preview_image")]
    public string? PreviewImage { get; set; }

    /// <summary>Optional raw metadata text to inject into the preview image.</summary>
    [JsonProperty("preview_image_metadata")]
    public string? PreviewImageMetadata { get; set; }
}
