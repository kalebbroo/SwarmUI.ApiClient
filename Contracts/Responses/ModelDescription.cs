using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Responses;

/// <summary>Detailed description of a single model returned by SwarmUI's <c>DescribeModel</c> API.</summary>
/// <remarks><c>DescribeModel</c> returns the same object <c>ListModels</c> puts in its <c>files</c> array, so this
/// adds nothing to <see cref="ModelInfo"/> and exists to name the endpoint's return type. Unmapped fields are
/// captured by <see cref="ModelInfo.ExtensionData"/>.</remarks>
public class ModelDescription : ModelInfo
{
}
