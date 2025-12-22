using System;
using System.Collections.Generic;

namespace SwarmUI.ApiClient.Models.Requests;

/// <summary>Request for adding or editing a preset.</summary>
/// <remarks>Presets are saved parameter configurations for image generation. This model mirrors SwarmUI's <c>AddNewPreset</c> HTTP payload at a high level; see the SwarmUI documentation for full details.</remarks>
public class PresetRequest
{
    /// <summary>Human-readable title of the preset shown in the SwarmUI UI.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description of what the preset does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Parameter key-value pairs that make up this preset.</summary>
    /// <remarks>Keys should match SwarmUI parameter names (for example, <c>model</c>, <c>steps</c>, <c>cfgscale</c>). The <c>PresetsEndpoint</c> shapes this dictionary into the <c>raw.param_map</c> structure expected by SwarmUI's <c>AddNewPreset</c> endpoint.</remarks>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    /// <summary>Optional base64-encoded image string used as the preview thumbnail for this preset.</summary>
    /// <remarks>Passed directly to SwarmUI's <c>preview_image</c> field. When null or empty, the existing preview (if any) is left unchanged for edits or omitted for new presets.</remarks>
    public string? PreviewImage { get; set; }

    /// <summary>Indicates whether this request should edit an existing preset instead of creating a new one.</summary>
    /// <remarks>When <c>true</c>, SwarmUI expects <see cref="EditingName"/> to indicate which existing preset to modify. When <c>false</c>, a new preset is created using <see cref="Title"/> as the name.</remarks>
    public bool IsEdit { get; set; }

    /// <summary>Name of the existing preset to edit when <see cref="IsEdit"/> is <c>true</c>.</summary>
    /// <remarks>Should match the preset name as it appears in SwarmUI. Ignored when <see cref="IsEdit"/> is <c>false</c>.</remarks>
    public string? EditingName { get; set; }
}
