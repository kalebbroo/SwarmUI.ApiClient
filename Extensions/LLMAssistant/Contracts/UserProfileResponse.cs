using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract carrying the caller's memory profile.</summary>
/// <remarks>Memory is strictly per-user: there is no shared layer and no way to address another user's profile. Contents are otherwise written by the model through its memory tool.</remarks>
public class UserProfileResponse : LLMAssistantResponse
{
    /// <summary>The caller's memory profile, or an empty template when unset.</summary>
    [JsonProperty("profile")]
    public JObject? Profile { get; set; }
}
