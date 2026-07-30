using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying the context the companion overlay needs.</summary>
public class CompanionContextResponse : LLMAssistantResponse
{
    /// <summary>The caller's most recently generated image, or null when they have no image history.</summary>
    [JsonProperty("lastImage")]
    public CompanionImage? LastImage { get; set; }
}

/// <summary>The most recent generated image surfaced to the companion overlay.</summary>
public class CompanionImage
{
    /// <summary>Path of the image relative to the caller's output directory.</summary>
    [JsonProperty("src")]
    public string Src { get; set; } = string.Empty;

    /// <summary>Served URL for the image. Null when the output directory sits outside the server output path and no URL can be built.</summary>
    [JsonProperty("url")]
    public string? Url { get; set; }

    /// <summary>MIME type guessed from the file extension.</summary>
    [JsonProperty("mediaType")]
    public string? MediaType { get; set; }

    /// <summary>Raw generation metadata recorded for the image.</summary>
    [JsonProperty("metadata")]
    public string? Metadata { get; set; }
}
