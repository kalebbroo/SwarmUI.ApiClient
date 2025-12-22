using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's <c>ListLoadedModels</c> endpoint. Contains the set of models that are currently loaded on at least one backend. Each entry is a full <see cref="ModelDescription"/> object.</summary>
public class LoadedModelsResponse
{
    /// <summary>Collection of models that are currently loaded. The format of each entry matches the <c>DescribeModel</c> return value.</summary>
    [JsonProperty("models")]
    public List<ModelDescription>? Models { get; set; }
}
