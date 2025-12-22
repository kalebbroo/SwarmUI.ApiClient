using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's <c>ListT2IParams</c> and <c>TriggerRefresh</c> endpoints.</summary>
/// <remarks>Contains the full set of configurable text-to-image parameters, groups, models, wildcards, and UI-specific parameter edit data. See <c>T2IAPI.md</c> for the full JSON schema.</remarks>
public class T2IParamsResponse
{
    /// <summary>List of all base T2I parameters that can be configured when generating images.
    /// Each entry describes a single parameter, including its ID, type, default value,
    /// valid range, grouping, and various UI hints (advanced flag, view type, etc.).</summary>
    [JsonProperty("list")]
    public List<T2IParamDefinition>? Parameters { get; set; }

    /// <summary>Group definitions that organize parameters into logical sections in the UI.
    /// Groups can be nested via the <see cref="T2IParamGroup.Parent"/> property.</summary>
    [JsonProperty("groups")]
    public List<T2IParamGroup>? Groups { get; set; }

    /// <summary>Mapping of model subtypes to available model identifiers.
    /// For example, the key "Stable-Diffusion" might map to a list of SD checkpoints,
    /// while "LoRA" maps to available LoRA files. The exact keys depend on the
    /// SwarmUI server configuration.</summary>
    [JsonProperty("models")]
    public Dictionary<string, List<string>>? ModelsBySubtype { get; set; }

    /// <summary>List of wildcard identifiers available on the server. Wildcards are textual
    /// placeholders that expand to one of several possible values during prompt
    /// generation (for example, to randomize style or subject).</summary>
    [JsonProperty("wildcards")]
    public List<string>? Wildcards { get; set; }

    /// <summary>Optional UI-specific parameter edit data. The structure of this object is
    /// intentionally left flexible as it is considered internal to SwarmUI's own
    /// interface and may change between versions. Consumers that do not need to
    /// reproduce the exact UI behaviour can usually ignore this field.</summary>
    [JsonProperty("param_edits")]
    public Dictionary<string, object>? ParamEdits { get; set; }
}

/// <summary>Describes a single configurable text-to-image parameter supported by SwarmUI.
/// Includes metadata such as ID, display name, description, data type, valid range,
/// grouping, and UI behaviour flags.</summary>
public class T2IParamDefinition
{
    /// <summary>User-facing display name for the parameter.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Internal parameter identifier used in raw input maps and metadata.
    /// For example, <c>"prompt"</c>, <c>"model"</c>, <c>"cfgscale"</c>, etc.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable description explaining what the parameter controls.
    /// Suitable for tooltips or documentation.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Base data type of the parameter, such as <c>"text"</c>, <c>"integer"</c>,
    /// <c>"float"</c>, <c>"boolean"</c>, etc. Used by UIs to select appropriate
    /// input controls.</summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Optional subtype that further refines the parameter type, commonly the
    /// model architecture family (for example, <c>"Stable-Diffusion"</c>).
    /// May be null when not applicable.</summary>
    [JsonProperty("subtype")]
    public string? Subtype { get; set; }

    /// <summary>Default value for the parameter, as represented in the underlying JSON.
    /// The value type depends on <see cref="Type"/>.</summary>
    [JsonProperty("default")]
    public object? Default { get; set; }

    /// <summary>Minimum numeric value for numeric parameters. Null when not applicable.</summary>
    [JsonProperty("min")]
    public double? Min { get; set; }

    /// <summary>Maximum numeric value for numeric parameters. Null when not applicable.</summary>
    [JsonProperty("max")]
    public double? Max { get; set; }

    /// <summary>Maximum value recommended for UI sliders (<c>view_max</c> in the API docs).
    /// Allows SwarmUI to support internal values beyond what is practical in the UI.</summary>
    [JsonProperty("view_max")]
    public double? ViewMax { get; set; }

    /// <summary>Step size for numeric parameters, used by sliders and spin controls.</summary>
    [JsonProperty("step")]
    public double? Step { get; set; }

    /// <summary>Optional list of allowed discrete values for enum-like parameters.
    /// Null when the parameter is free-form or purely numeric.</summary>
    [JsonProperty("values")]
    public List<string>? Values { get; set; }

    /// <summary>Optional list of example values shown in the UI to help users understand
    /// how to use the parameter effectively.</summary>
    [JsonProperty("examples")]
    public List<string>? Examples { get; set; }

    /// <summary>Indicates whether the parameter is visible in the default UI.
    /// Parameters that are not visible may be internal or controlled indirectly
    /// by other options.</summary>
    [JsonProperty("visible")]
    public bool Visible { get; set; }

    /// <summary>Indicates whether the parameter is considered "advanced" and should be
    /// hidden behind an advanced settings toggle in the UI.</summary>
    [JsonProperty("advanced")]
    public bool Advanced { get; set; }

    /// <summary>Optional feature flag that must be enabled on the server for this parameter
    /// to be relevant. Allows SwarmUI to expose parameters conditionally based on
    /// server capabilities.</summary>
    [JsonProperty("feature_flag")]
    public string? FeatureFlag { get; set; }

    /// <summary>Indicates whether the parameter can be toggled on/off as a unit in the UI.</summary>
    [JsonProperty("toggleable")]
    public bool Toggleable { get; set; }

    /// <summary>Relative priority used by the UI to order parameters within their groups.
    /// Lower numbers typically appear earlier.</summary>
    [JsonProperty("priority")]
    public int Priority { get; set; }

    /// <summary>Optional group identifier indicating which parameter group this parameter
    /// belongs to. Null when the parameter is not assigned to a group.</summary>
    [JsonProperty("group")]
    public string? GroupId { get; set; }

    /// <summary>Indicates whether the parameter value should always be retained between
    /// generations, even when using presets or resetting other controls.</summary>
    [JsonProperty("always_retain")]
    public bool AlwaysRetain { get; set; }

    /// <summary>When true, the parameter should not be persisted into saved presets.
    /// Typically used for temporary or environment-specific options.</summary>
    [JsonProperty("do_not_save")]
    public bool DoNotSave { get; set; }

    /// <summary>When true, the parameter should not be included in preview metadata.</summary>
    [JsonProperty("do_not_preview")]
    public bool DoNotPreview { get; set; }

    /// <summary>UI-specific hint describing how the parameter should be rendered, such as
    /// <c>"big"</c> for a large control. Exact values and semantics are UI-defined.</summary>
    [JsonProperty("view_type")]
    public string? ViewType { get; set; }

    /// <summary>Indicates whether the parameter should be hidden in certain UI layouts even
    /// when visible, usually because it is an implementation detail.</summary>
    [JsonProperty("extra_hidden")]
    public bool ExtraHidden { get; set; }
}

/// <summary>Describes a logical group of parameters used to organize the generate tab UI.
/// Groups can be nested via the <see cref="Parent"/> property and support basic
/// UI behaviours such as collapsing and toggling.</summary>
public class T2IParamGroup
{
    /// <summary>User-facing group name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Internal group identifier. Parameters reference this via their <c>group</c>
    /// property to indicate membership.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Indicates whether this group has an on/off toggle in the UI.</summary>
    [JsonProperty("toggles")]
    public bool Toggles { get; set; }

    /// <summary>Indicates whether the group is initially open (expanded) in the UI.</summary>
    [JsonProperty("open")]
    public bool Open { get; set; }

    /// <summary>Priority used to order groups relative to each other. Lower numbers typically
    /// appear earlier.</summary>
    [JsonProperty("priority")]
    public int Priority { get; set; }

    /// <summary>Human-readable description of the group, suitable for tooltips.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Indicates whether this group is considered "advanced" and should be hidden
    /// behind an advanced settings toggle.</summary>
    [JsonProperty("advanced")]
    public bool Advanced { get; set; }

    /// <summary>Indicates whether the group can be visually shrunk or collapsed in the UI.</summary>
    [JsonProperty("can_shrink")]
    public bool CanShrink { get; set; }

    /// <summary>Optional parent group ID when groups are nested. Null when this is a top-level
    /// group.</summary>
    [JsonProperty("parent")]
    public string? Parent { get; set; }
}
