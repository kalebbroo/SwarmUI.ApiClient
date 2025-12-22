using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's <c>TestPromptFill</c> endpoint. Contains the filled prompt text after wildcard and random expansions have been applied.</summary>
public class TestPromptFillResponse
{
    /// <summary>The resulting filled prompt returned by the server.</summary>
    [JsonProperty("result")]
    public string Result { get; set; } = string.Empty;
}
