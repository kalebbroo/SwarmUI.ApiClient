using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Contracts.Responses;

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

    /// <summary>Mapping of model subtypes to the models available under each, for example <c>"Stable-Diffusion"</c>
    /// to the SD checkpoints and <c>"LoRA"</c> to the LoRA files. The exact keys depend on the server configuration.</summary>
    /// <remarks>Each entry carries the model's architecture class id alongside its name, which is what makes a
    /// dropdown filterable by what the selected base model accepts. Older servers sent plain name strings; those
    /// deserialize with a null <see cref="T2IParamModelEntry.ClassId"/>.</remarks>
    [JsonProperty("models")]
    [JsonConverter(typeof(ModelsBySubtypeConverter))]
    public Dictionary<string, List<T2IParamModelEntry>>? ModelsBySubtype { get; set; }

    /// <summary>Every architecture class the server knows, keyed by class id.</summary>
    /// <remarks>Resolve a model's <see cref="ModelInfo.Architecture"/> here to reach its compatibility class and
    /// native resolution without a second request.</remarks>
    [JsonProperty("model_classes")]
    public Dictionary<string, T2IModelClassInfo>? ModelClasses { get; set; }

    /// <summary>Every compatibility class the server knows, keyed by class id.</summary>
    /// <remarks>This is the table behind adapter compatibility: a LoRA, VAE or ControlNet fits a base model when
    /// their <see cref="ModelInfo.CompatClass"/> values match.</remarks>
    [JsonProperty("model_compat_classes")]
    public Dictionary<string, T2ICompatClassInfo>? ModelCompatClasses { get; set; }

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

/// <summary>Deserializes SwarmUI's <c>models</c> map, tolerating both wire formats: entries as plain
/// name strings (older servers) or as <c>[name, compatClassId]</c> pairs (current servers).</summary>
internal sealed class ModelsBySubtypeConverter : JsonConverter<Dictionary<string, List<T2IParamModelEntry>>?>
{
    public override Dictionary<string, List<T2IParamModelEntry>>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, List<T2IParamModelEntry>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }
        JObject map = JObject.Load(reader);
        Dictionary<string, List<T2IParamModelEntry>> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (JProperty subtype in map.Properties())
        {
            List<T2IParamModelEntry> models = [];
            if (subtype.Value is JArray entries)
            {
                foreach (JToken entry in entries)
                {
                    (string? name, string? classId) = entry switch
                    {
                        JArray pair when pair.Count > 1 => (pair[0]?.ToString(), pair[1]?.ToString()),
                        JArray pair when pair.Count > 0 => (pair[0]?.ToString(), null),
                        _ => (entry.Type == JTokenType.String ? entry.ToString() : null, null)
                    };
                    if (!string.IsNullOrEmpty(name))
                    {
                        models.Add(new T2IParamModelEntry { Name = name, ClassId = string.IsNullOrEmpty(classId) ? null : classId });
                    }
                }
            }
            result[subtype.Name] = models;
        }
        return result;
    }

    public override void WriteJson(JsonWriter writer, Dictionary<string, List<T2IParamModelEntry>>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

/// <summary>One model offered by a model-typed parameter's dropdown.</summary>
public class T2IParamModelEntry
{
    /// <summary>Model name exactly as <see cref="ModelInfo.Name"/> spells it, file extension included.</summary>
    /// <remarks>Model-typed parameters that publish their own <see cref="T2IParamDefinition.Values"/> — the refiner
    /// model and the VAEs — strip <c>.safetensors</c> from those values, so the same model is spelled two ways in
    /// one response. Match on the list you are actually reading.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Architecture class id for this model, or null on a server old enough to send names alone.</summary>
    public string? ClassId { get; set; }
}

/// <summary>One architecture class, as listed in <see cref="T2IParamsResponse.ModelClasses"/>.</summary>
public class T2IModelClassInfo
{
    /// <summary>Class id, matching <see cref="ModelInfo.Architecture"/> and the key this entry is stored under.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable class name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Compatibility class this architecture belongs to. Null when the class declares none.</summary>
    [JsonProperty("compat_class")]
    public string? CompatClass { get; set; }

    /// <summary>Native width for this class, in pixels.</summary>
    [JsonProperty("standard_width")]
    public int StandardWidth { get; set; }

    /// <summary>Native height for this class, in pixels.</summary>
    [JsonProperty("standard_height")]
    public int StandardHeight { get; set; }
}

/// <summary>One compatibility class, as listed in <see cref="T2IParamsResponse.ModelCompatClasses"/>.</summary>
public class T2ICompatClassInfo
{
    /// <summary>Compatibility class id, matching <see cref="ModelInfo.CompatClass"/> and the key this entry is stored under.</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Short display code, for example <c>"SDv1"</c>.</summary>
    [JsonProperty("short_code")]
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>True when LoRAs for this class also patch the text encoder.</summary>
    [JsonProperty("loras_target_text_enc")]
    public bool LorasTargetTextEncoder { get; set; }

    /// <summary>True when this class generates video from text.</summary>
    [JsonProperty("is_text2video")]
    public bool IsText2Video { get; set; }

    /// <summary>True when this class generates video from an image.</summary>
    [JsonProperty("is_image2video")]
    public bool IsImage2Video { get; set; }

    /// <summary>True when this class generates audio.</summary>
    [JsonProperty("is_audio_model")]
    public bool IsAudioModel { get; set; }

    /// <summary>True when this class encodes audio and video into one latent.</summary>
    [JsonProperty("has_joint_av_latents")]
    public bool HasJointAvLatents { get; set; }

    /// <summary>Width and height for this class must be a multiple of this many pixels.</summary>
    [JsonProperty("resolution_precision")]
    public int ResolutionPrecision { get; set; }

    /// <summary>VAE family this class expects, or null when it does not constrain the VAE.</summary>
    [JsonProperty("vae_family")]
    public string? VaeFamily { get; set; }
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

    /// <summary>Minimum value recommended for UI sliders (<c>view_min</c> in the API docs), when the practical
    /// floor differs from <see cref="Min"/>.</summary>
    [JsonProperty("view_min")]
    public double? ViewMin { get; set; }

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

    /// <summary>Display labels for <see cref="Values"/>, in the same order and length.</summary>
    /// <remarks>Without these a dropdown can only show raw ids: <c>"StepSwapNoisy"</c> where the server means
    /// "Step-Swap Noisy (Modified Refiner)". An entry with no separate label repeats the value itself.</remarks>
    [JsonProperty("value_names")]
    public List<string>? ValueNames { get; set; }

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
    /// Lower numbers appear earlier.</summary>
    /// <remarks>Fractional, so a parameter can be slotted between two existing ones: 42 of them are, such as
    /// <c>initimagecreativity</c> at -4.5 and <c>fluxdisableguidance</c> at 6.2. Rounding to a whole number
    /// collapses those deliberate orderings into ties.</remarks>
    [JsonProperty("priority")]
    public double Priority { get; set; }

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

    /// <summary>When true, this parameter should not be carried over by a "reuse these settings" action.</summary>
    /// <remarks>Set for values that only make sense for the one generation that produced them.</remarks>
    [JsonProperty("nonreusable")]
    public bool NonReusable { get; set; }

    /// <summary>When true, the parameter accepts per-region section syntax in addition to a plain value.</summary>
    [JsonProperty("can_sectionalize")]
    public bool CanSectionalize { get; set; }

    /// <summary>Id of another parameter that must be set to something other than its default before this one
    /// applies, or null when this parameter stands alone.</summary>
    /// <remarks>For example the mask-edge parameters name <c>maskimage</c>, because they do nothing without a mask.</remarks>
    [JsonProperty("depend_non_default")]
    public string? DependNonDefault { get; set; }
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

    /// <summary>Priority used to order groups relative to each other. Lower numbers appear earlier.</summary>
    /// <remarks>Fractional like the parameter equivalent — the ControlNet groups sit at -0.9, -0.8 and -0.7 to hold
    /// their order.</remarks>
    [JsonProperty("priority")]
    public double Priority { get; set; }

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
