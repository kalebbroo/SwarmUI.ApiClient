using System.Collections.Generic;
using SwarmUI.ApiClient.Extensions.APIBackends.Contracts;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;
using SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Extension-registered generation parameters, one slot per supported SwarmUI server extension.</summary>
/// <remarks>
/// <para>Attached to a request through <c>GenerationRequest.Extensions</c> — the single, clear place every
/// extension's own parameters live, instead of being declared directly on <c>GenerationRequest</c> itself (which
/// is reserved for parameters stock SwarmUI registers). Mirrors <see cref="ISwarmExtensions"/>, the same
/// one-property-per-extension shape already used for endpoint access.</para>
/// <para>Composes freely: set as many slots as the target server has extensions installed, in one request. Each
/// slot defaults to an empty (all-null) instance rather than null, so <c>request.Extensions.AudioLab.Foo = ...</c>
/// works immediately without first having to construct the slot — an empty slot contributes nothing to the wire
/// payload. Every property on every slot still serializes under its own top-level wire name, flattened into the
/// same JSON object as the core parameters by <c>GenerationEndpoint.CreateGenerationPayload</c>: this type changes
/// the C# shape only, never the wire format.</para>
/// </remarks>
public class GenerationRequestExtensionParams
{
    /// <summary>Parameters registered by the AudioLab extension (speech, music, and audio generation).</summary>
    public AudioLabGenerationParams AudioLab { get; set; } = new();

    /// <summary>Parameters registered by the SwarmUI-API-Backends extension (BFL, OpenAI, Ideogram, Google, Grok, and fal.ai-fronted providers).</summary>
    public APIBackendsGenerationParams APIBackends { get; set; } = new();

    /// <summary>Parameters registered by the HartsyInference backend extension.</summary>
    public HartsyInferenceGenerationParams HartsyInference { get; set; } = new();

    /// <summary>Parameters registered by the LLMAssistant extension's <c>&lt;llmprompt&gt;</c> tag handler.</summary>
    public LLMAssistantGenerationParams LLMAssistant { get; set; } = new();

    /// <summary>Every non-null slot, for the serializer to flatten into the generation payload in a fixed,
    /// deterministic order. A slot is normally never null (each defaults to an empty instance) -- the check
    /// only guards a caller who explicitly nulled one out, since the setters are public.</summary>
    internal IEnumerable<object> EnumerateParams()
    {
        if (AudioLab is not null)
        {
            yield return AudioLab;
        }
        if (APIBackends is not null)
        {
            yield return APIBackends;
        }
        if (HartsyInference is not null)
        {
            yield return HartsyInference;
        }
        if (LLMAssistant is not null)
        {
            yield return LLMAssistant;
        }
    }
}
