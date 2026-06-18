namespace SwarmUI.ApiClient.Models.Enums;

/// <summary>Organizations, labs, or individuals that author and publish the model architectures SwarmUI supports.</summary>
/// <remarks>Enum value names are filesystem-safe and intended for use as folder segments in object-storage paths and disk layouts. Each <see cref="SwarmModelFamily"/> maps to exactly one author via <see cref="SwarmModelFamilyExtensions.GetAuthor"/>.</remarks>
public enum SwarmModelAuthor
{
    /// <summary>Stability AI — Stable Diffusion 1/2/XL/3/3.5, SD-Turbo, Stable Cascade.</summary>
    StabilityAI,
    /// <summary>Black Forest Labs — Flux.1 and Flux.2 family.</summary>
    BFL,
    /// <summary>Fal.AI — AuraFlow.</summary>
    FalAI,
    /// <summary>Alibaba Group — Z-Image (Tongyi MAI), Qwen-Image, Ovis (AIDC-AI), LongCat (Meituan/Alibaba), Wan (Wan-AI).</summary>
    Alibaba,
    /// <summary>Tencent — Hunyuan Image and Hunyuan Video.</summary>
    Tencent,
    /// <summary>Lightricks — LTX-Video and LTX-2.</summary>
    Lightricks,
    /// <summary>Kandinsky Lab — Kandinsky 5 image and video models.</summary>
    KandinskyLab,
    /// <summary>Lodestone Rock — Chroma, Chroma Radiance, Zeta Chroma.</summary>
    LodestoneRock,
    /// <summary>Alpha-VLLM — Lumina 2.0.</summary>
    AlphaVLLM,
    /// <summary>Circlestone Labs — Anima.</summary>
    CirclestoneLabs,
    /// <summary>Baidu — ERNIE Image.</summary>
    Baidu,
    /// <summary>Segmind — SSD-1B.</summary>
    Segmind,
    /// <summary>PixArt — PixArt-Σ.</summary>
    PixArt,
    /// <summary>NVIDIA — Sana, Cosmos Predict2.</summary>
    NVIDIA,
    /// <summary>HiDream AI (Vivago) — HiDream-I1, HiDream-O1.</summary>
    HiDreamAI,
    /// <summary>VectorSpaceLab — OmniGen 2.</summary>
    VectorSpaceLab,
    /// <summary>StepFun — ACE-Step audio model.</summary>
    StepFun,
    /// <summary>Genmo — Mochi 1.</summary>
    Genmo,
    /// <summary>Ideogram — Ideogram 4 (open-weight).</summary>
    Ideogram
}
