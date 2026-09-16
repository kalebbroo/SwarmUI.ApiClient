using System;
using System.Collections.Generic;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>The value each parameter carries to mean "leave this feature off".</summary>
/// <remarks>
/// <para>SwarmUI calls this <c>IgnoreIf</c>: a parameter set to this value is dropped before the generation runs,
/// so sending it and omitting it are the same request. Naming a refiner model is what turns the refiner stage on,
/// and <c>"(Use Base)"</c> is how you turn it back off — there is no separate switch.</para>
/// <para><b>This table is a mirror, not wire data.</b> <c>T2IParamType.ToNet</c> does not emit <c>ignore_if</c>, so
/// nothing in a <c>ListT2IParams</c> response reveals it and no client can verify this at runtime. It was extracted
/// from the SwarmUI source on 2026-09-15 — <c>T2IParamTypes.cs</c> and <c>ComfyUIBackendExtension.cs</c> — and it
/// goes stale silently when the server changes. Emitting <c>ignore_if</c> from <c>ToNet</c> would be a one-line
/// change in SwarmUI core and would retire this type.</para>
/// <para>For 103 of these the off value is also the parameter's default, so comparing against
/// <see cref="Responses.T2IParamDefinition.Default"/> gives the same answer. For three it does not:
/// <c>maskblur</c> defaults to 4 and is off at 0, <c>easycachestart</c> defaults to 0.15 and is off at 0, and
/// <c>easycacheend</c> defaults to 0.95 and is off at 1. Code that treats "equals default" as "off" is wrong for
/// those three, which is the reason this is a table rather than a rule.</para>
/// </remarks>
public static class SwarmParamOffValues
{
    private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
    {
        ["automaticvae"] = "false",
        ["batchsize"] = "1",
        ["cascadelatentcompression"] = "32",
        ["clipgmodel"] = "",
        ["cliplmodel"] = "",
        ["clipvisionmodel"] = "",
        ["colorcorrectionbehavior"] = "None",
        ["colordepth"] = "8bit",
        ["continueaftererrors"] = "false",
        ["controlnetpreviewonly"] = "false",
        ["debugregionalprompting"] = "false",
        ["donotsave"] = "false",
        ["donotsaveintermediates"] = "false",
        ["easycacheend"] = "1",
        ["easycachemode"] = "disabled",
        ["easycachestart"] = "0",
        ["enableaitemplate"] = "false",
        ["enablereferencelatents"] = "none",
        ["endstepsearly"] = "0",
        ["fluxdisableguidance"] = "false",
        ["forwardrawbackenddata"] = "false",
        ["forwardswarmdata"] = "false",
        ["gemmamodel"] = "",
        ["gligenmodel"] = "None",
        ["globalregionfactor"] = "0.5",
        ["gptossmodel"] = "",
        ["images"] = "1",
        ["initimagenoise"] = "0",
        ["initimagerecompositemask"] = "true",
        ["initimageresettonorm"] = "0",
        ["internalbackendtype"] = "Any",
        ["ipadapterend"] = "1",
        ["ipadapterstart"] = "0",
        ["ipadapterweight"] = "1",
        ["justloadmodel"] = "false",
        ["llamamodel"] = "",
        ["llavamodel"] = "",
        ["maskblur"] = "0",
        ["maskcompositeunthresholded"] = "false",
        ["maskgrow"] = "0",
        ["mistralmodel"] = "",
        ["modelspecificenhancements"] = "true",
        ["negativemodel"] = "",
        ["negativeprompt"] = "",
        ["nointernalspecialhandling"] = "false",
        ["noloadmodels"] = "false",
        ["nopreviews"] = "false",
        ["normalizedattentionguidancescale"] = "0",
        ["noseedincrement"] = "false",
        ["nunchakucachethreshold"] = "0",
        ["outputintermediateimages"] = "false",
        ["personalnote"] = "",
        ["placeholderparamgroupstarred"] = "false",
        ["placeholderparamgroupuserone"] = "false",
        ["placeholderparamgroupuserthree"] = "false",
        ["placeholderparamgroupusertwo"] = "false",
        ["qwenmodel"] = "",
        ["refinerdotiling"] = "false",
        ["refinermodel"] = "(Use Base)",
        ["refinerupscale"] = "1",
        ["refinervae"] = "None",
        ["regionalobjectcleanupfactor"] = "0",
        ["removebackground"] = "false",
        ["renormcfg"] = "0",
        ["revisionstrength"] = "0",
        ["revisionzeroprompt"] = "false",
        ["sambbox"] = "",
        ["samnegativepoints"] = "[]",
        ["sampositivepoints"] = "[]",
        ["savesegmentmask"] = "false",
        ["seamlesstileable"] = "false",
        ["seedvrmodel"] = "None",
        ["seedvrpredownscale"] = "1",
        ["seedvrsplitlatent"] = "false",
        ["seedvrupscale"] = "1",
        ["segmentapplyafter"] = "Refiner",
        ["segmentsortorder"] = "left-right",
        ["shiftedlatentaverageinit"] = "false",
        ["smartimagepromptresizing"] = "true",
        ["stylemodelapplystart"] = "0",
        ["stylemodelmergestrength"] = "1",
        ["stylemodelmultiplystrength"] = "1",
        ["teacachemode"] = "disabled",
        ["teacachestart"] = "0",
        ["textencodedimage"] = "auto",
        ["torchcompile"] = "Disabled",
        ["trimvideoendframes"] = "0",
        ["trimvideostartframes"] = "0",
        ["txxlmodel"] = "",
        ["usecfgzerostar"] = "false",
        ["useinpaintingencode"] = "false",
        ["useipadapter"] = "None",
        ["usereferenceonly"] = "false",
        ["usesparseattention"] = "None",
        ["usestylemodel"] = "None",
        ["usetcfg"] = "false",
        ["vae"] = "None",
        ["variationseedstrength"] = "0",
        ["videoboomerang"] = "false",
        ["videoframeinterpolationmultiplier"] = "1",
        ["videopreviewtype"] = "animate",
        ["videovideocreativity"] = "1",
        ["webhooks"] = "Normal",
        ["wildcardseedbehavior"] = "Random",
        ["yolomodelinternal"] = "",
        ["zeronegative"] = "false",
    };

    /// <summary>Every parameter id that declares an off value, mapped to that value.</summary>
    public static IReadOnlyDictionary<string, string> All => Values;

    /// <summary>Gets the value that turns this parameter off.</summary>
    /// <param name="parameterId">Registered parameter id, for example <c>"refinermodel"</c>.</param>
    /// <param name="offValue">The off value when one is known.</param>
    /// <returns><c>true</c> when this parameter has an off value.</returns>
    public static bool TryGetOffValue(string parameterId, out string offValue) => Values.TryGetValue(parameterId, out offValue!);

    /// <summary>Whether a value would be dropped by the server as meaning "off".</summary>
    /// <param name="parameterId">Registered parameter id.</param>
    /// <param name="value">Value as it would be sent, compared as a string exactly as SwarmUI compares it.</param>
    /// <returns><c>true</c> when the server would ignore the parameter at this value. <c>false</c> for a parameter with no off value, which is never dropped for its value alone.</returns>
    public static bool IsOff(string parameterId, string? value) => value is not null && Values.TryGetValue(parameterId, out string? off) && string.Equals(off, value, StringComparison.Ordinal);
}
