using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

/// <summary>Response contract for a token count.</summary>
public class CountTokensResponse : LLMAssistantResponse
{
    /// <summary>Number of tokens counted.</summary>
    [JsonProperty("count")]
    public int Count { get; set; }

    /// <summary>Whether the count came from a real tokenizer rather than an estimate.</summary>
    [JsonProperty("exact")]
    public bool Exact { get; set; }

    /// <summary>Which tokenizer or heuristic produced the count.</summary>
    [JsonProperty("source")]
    public string? Source { get; set; }
}
