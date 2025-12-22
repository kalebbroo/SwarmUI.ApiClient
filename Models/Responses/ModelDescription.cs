using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Detailed description of a single model returned by SwarmUI's <c>DescribeModel</c> API.</summary>
/// <remarks>Inherits from <see cref="ModelInfo"/> and captures unmapped JSON fields in <see cref="ExtensionData"/> for forward compatibility.</remarks>
public class ModelDescription : ModelInfo
{
    /// <summary>Captures additional JSON properties that do not map to this type or its base <see cref="ModelInfo"/>.</summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? ExtensionData { get; set; }
}
