using System;

namespace SwarmUI.ApiClient.Models.Enums;

/// <summary>Model architecture families supported by SwarmUI, mirroring the catalog documented in <c>SwarmUI/docs/Model Support.md</c> and <c>Video Model Support.md</c>.</summary>
/// <remarks>A family groups all variants of one lineage (base, distilled, refiner, schnell, dev, edit, etc.) plus the LoRA, ControlNet, and VAE adapters that target it. Adding a new model that fits an existing family requires no enum change. New families published by SwarmUI require one enum value and a switch arm in <see cref="SwarmModelFamilyExtensions.GetAuthor"/>.</remarks>
public enum SwarmModelFamily
{
    /// <summary>Stable Diffusion 1.x (legacy). Author: Stability AI.</summary>
    SD1,
    /// <summary>Stable Diffusion 2.x (legacy). Author: Stability AI.</summary>
    SD2,
    /// <summary>Stable Diffusion XL — base and refiner. Author: Stability AI.</summary>
    SDXL,
    /// <summary>SD-Turbo, SDXL-Turbo, LCM, Lightning distilled variants. Author: Stability AI.</summary>
    SDTurbo,
    /// <summary>Stable Diffusion 3 medium. Author: Stability AI.</summary>
    SD3,
    /// <summary>Stable Diffusion 3.5 large, large-turbo, medium. Author: Stability AI.</summary>
    SD35,
    /// <summary>Stable Cascade (Würstchen) — paired stage_b and stage_c. Author: Stability AI.</summary>
    StableCascade,
    /// <summary>Flux.1 dev, schnell, redux, kontext, fill, canny, depth. Author: Black Forest Labs.</summary>
    Flux1,
    /// <summary>Flux.2 dev, klein-4b, klein-9b and turbo variants. Author: Black Forest Labs.</summary>
    Flux2,
    /// <summary>AuraFlow v0.1 / v0.2 / v0.3 (and Pony v7 derivatives). Author: Fal.AI.</summary>
    AuraFlow,
    /// <summary>Z-Image base and Z-Image Turbo. Author: Alibaba (Tongyi MAI).</summary>
    ZImage,
    /// <summary>Qwen-Image base, edit, distill, lightning. Author: Alibaba (Qwen).</summary>
    QwenImage,
    /// <summary>Ovis Image 7B. Author: Alibaba (AIDC-AI).</summary>
    Ovis,
    /// <summary>LongCat Image 6B. Author: Alibaba (Meituan).</summary>
    LongCat,
    /// <summary>Wan 2.1 / 2.2 text-to-video, image-to-video, FLF2V, Phantom, VACE. Author: Alibaba (Wan-AI).</summary>
    Wan,
    /// <summary>Hunyuan Image 2.1 base, distilled, refiner. Author: Tencent.</summary>
    HunyuanImage,
    /// <summary>Hunyuan Video text-to-video, image-to-video, fastvideo, 1.5 variants. Author: Tencent.</summary>
    HunyuanVideo,
    /// <summary>LTX-Video 0.9.x. Author: Lightricks.</summary>
    LTXV,
    /// <summary>LTX-2 (audio + video), LTX-2.3, refiner LoRA. Author: Lightricks.</summary>
    LTXV2,
    /// <summary>Kandinsky 5.0 Image (Lite). Author: Kandinsky Lab.</summary>
    Kandinsky5Image,
    /// <summary>Kandinsky 5.0 Video (Lite 2B, Pro 19B). Author: Kandinsky Lab.</summary>
    Kandinsky5Video,
    /// <summary>Chroma 1 HD and FP8. Author: Lodestone Rock.</summary>
    Chroma,
    /// <summary>Chroma 1 Radiance (pixel-space). Author: Lodestone Rock.</summary>
    ChromaRadiance,
    /// <summary>Zeta Chroma (pixel-space Z-Image derivative). Author: Lodestone Rock.</summary>
    ZetaChroma,
    /// <summary>Lumina 2.0 (NextDiT, Gemma 2B encoder). Author: Alpha-VLLM.</summary>
    Lumina,
    /// <summary>Anima 2B preview (anime-focused). Author: Circlestone Labs.</summary>
    Anima,
    /// <summary>ERNIE Image base and turbo. Author: Baidu.</summary>
    ERNIE,
    /// <summary>SSD-1B (Segmind Stable Diffusion 1B distillation). Author: Segmind.</summary>
    SSD1B,
    /// <summary>PixArt-Σ XL 2 (1024px, 2K). Author: PixArt.</summary>
    PixArtSigma,
    /// <summary>Sana 1.6B. Author: NVIDIA.</summary>
    Sana,
    /// <summary>Cosmos Predict2 2B/14B text-to-image. Author: NVIDIA.</summary>
    CosmosPredict2,
    /// <summary>HiDream-I1 full, dev, fast, edit (17B MMDiT). Author: HiDream AI.</summary>
    HiDreamI1,
    /// <summary>HiDream-O1 base, dev, edit (8B Pixel UiT, 2026). Author: HiDream AI.</summary>
    HiDreamO1,
    /// <summary>OmniGen 2 (multimodal). Author: VectorSpaceLab.</summary>
    OmniGen2,
    /// <summary>ACE-Step 1.5 audio (music generation). Author: StepFun.</summary>
    AceStep,
    /// <summary>Mochi 1 video preview. Author: Genmo.</summary>
    Mochi1,
    /// <summary>Ideogram 4. Author: Ideogram.</summary>
    Ideogram
}

/// <summary>Extension methods that derive metadata for a <see cref="SwarmModelFamily"/>.</summary>
public static class SwarmModelFamilyExtensions
{
    /// <summary>Returns the author (organization or lab) that publishes the given family. Each family maps to exactly one author.</summary>
    public static SwarmModelAuthor GetAuthor(this SwarmModelFamily family) => family switch
    {
        SwarmModelFamily.SD1 or SwarmModelFamily.SD2 or SwarmModelFamily.SDXL or SwarmModelFamily.SDTurbo
            or SwarmModelFamily.SD3 or SwarmModelFamily.SD35 or SwarmModelFamily.StableCascade => SwarmModelAuthor.StabilityAI,
        SwarmModelFamily.Flux1 or SwarmModelFamily.Flux2 => SwarmModelAuthor.BFL,
        SwarmModelFamily.AuraFlow => SwarmModelAuthor.FalAI,
        SwarmModelFamily.ZImage or SwarmModelFamily.QwenImage or SwarmModelFamily.Ovis
            or SwarmModelFamily.LongCat or SwarmModelFamily.Wan => SwarmModelAuthor.Alibaba,
        SwarmModelFamily.HunyuanImage or SwarmModelFamily.HunyuanVideo => SwarmModelAuthor.Tencent,
        SwarmModelFamily.LTXV or SwarmModelFamily.LTXV2 => SwarmModelAuthor.Lightricks,
        SwarmModelFamily.Kandinsky5Image or SwarmModelFamily.Kandinsky5Video => SwarmModelAuthor.KandinskyLab,
        SwarmModelFamily.Chroma or SwarmModelFamily.ChromaRadiance or SwarmModelFamily.ZetaChroma => SwarmModelAuthor.LodestoneRock,
        SwarmModelFamily.Lumina => SwarmModelAuthor.AlphaVLLM,
        SwarmModelFamily.Anima => SwarmModelAuthor.CirclestoneLabs,
        SwarmModelFamily.ERNIE => SwarmModelAuthor.Baidu,
        SwarmModelFamily.SSD1B => SwarmModelAuthor.Segmind,
        SwarmModelFamily.PixArtSigma => SwarmModelAuthor.PixArt,
        SwarmModelFamily.Sana or SwarmModelFamily.CosmosPredict2 => SwarmModelAuthor.NVIDIA,
        SwarmModelFamily.HiDreamI1 or SwarmModelFamily.HiDreamO1 => SwarmModelAuthor.HiDreamAI,
        SwarmModelFamily.OmniGen2 => SwarmModelAuthor.VectorSpaceLab,
        SwarmModelFamily.AceStep => SwarmModelAuthor.StepFun,
        SwarmModelFamily.Mochi1 => SwarmModelAuthor.Genmo,
        SwarmModelFamily.Ideogram => SwarmModelAuthor.Ideogram,
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown SwarmModelFamily value — add an author mapping in SwarmModelFamilyExtensions.GetAuthor")
    };
}
