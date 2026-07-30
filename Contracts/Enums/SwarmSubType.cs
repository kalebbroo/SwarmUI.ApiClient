using System;

namespace SwarmUI.ApiClient.Contracts.Enums;

/// <summary>Model sub-types accepted by SwarmUI's <c>DoModelDownloadWS</c> endpoint, mirroring the eight categories SwarmUI registers in <c>Program.BuildModelLists()</c>.</summary>
/// <remarks>The enum value names use C# casing conventions. Use <see cref="SwarmSubTypeExtensions.AsApiType"/> to obtain the exact string SwarmUI accepts in API payloads (case-sensitive), or <see cref="SwarmSubTypeExtensions.AsFolder"/> to obtain SwarmUI's default on-disk folder name (which differs in casing for several entries).</remarks>
public enum SwarmSubType
{
    /// <summary>Base diffusion models (Stable Diffusion, Flux, Z-Image, etc.). API: "Stable-Diffusion", Folder: "Stable-Diffusion".</summary>
    StableDiffusion,
    /// <summary>Variational Auto-Encoders. API: "VAE", Folder: "VAE".</summary>
    VAE,
    /// <summary>Low-rank adaptation models. API: "LoRA", Folder: "Lora".</summary>
    LoRA,
    /// <summary>Textual inversion embeddings. API: "Embedding", Folder: "Embeddings".</summary>
    Embedding,
    /// <summary>ControlNet guidance models. API: "ControlNet", Folder: "controlnet".</summary>
    ControlNet,
    /// <summary>CLIP and T5 text encoders. API: "Clip", Folder: "text_encoders" (SwarmUI also scans "clip" as a secondary path).</summary>
    Clip,
    /// <summary>CLIP Vision encoders used by IP-Adapter and similar. API: "ClipVision", Folder: "clip_vision".</summary>
    ClipVision,
    /// <summary>Large language models registered by the SwarmUI-LLMAssistant extension. API: "LLM", Folder: "llm".</summary>
    LLM
}

/// <summary>Extension methods that translate <see cref="SwarmSubType"/> values to the exact strings SwarmUI uses in its API payloads and on-disk folder structure.</summary>
public static class SwarmSubTypeExtensions
{
    /// <summary>Returns the case-sensitive string SwarmUI's <c>DoModelDownloadWS</c> (and related model APIs) expects in the <c>type</c> field.</summary>
    public static string AsApiType(this SwarmSubType subType) => subType switch
    {
        SwarmSubType.StableDiffusion => "Stable-Diffusion",
        SwarmSubType.VAE => "VAE",
        SwarmSubType.LoRA => "LoRA",
        SwarmSubType.Embedding => "Embedding",
        SwarmSubType.ControlNet => "ControlNet",
        SwarmSubType.Clip => "Clip",
        SwarmSubType.ClipVision => "ClipVision",
        SwarmSubType.LLM => "LLM",
        _ => throw new ArgumentOutOfRangeException(nameof(subType), subType, "Unknown SwarmSubType value")
    };

    /// <summary>Returns SwarmUI's default on-disk folder name under <c>Models/</c> for this sub-type, as configured in <c>Settings.SDModelFolder</c> and siblings.</summary>
    /// <remarks>Folder casing differs from the API type string for several entries (for example API "LoRA" maps to folder "Lora"). Always use this method when composing filesystem or object-storage keys that must match what SwarmUI sees on disk.</remarks>
    public static string AsFolder(this SwarmSubType subType) => subType switch
    {
        SwarmSubType.StableDiffusion => "Stable-Diffusion",
        SwarmSubType.VAE => "VAE",
        SwarmSubType.LoRA => "Lora",
        SwarmSubType.Embedding => "Embeddings",
        SwarmSubType.ControlNet => "controlnet",
        SwarmSubType.Clip => "text_encoders",
        SwarmSubType.ClipVision => "clip_vision",
        SwarmSubType.LLM => "llm",
        _ => throw new ArgumentOutOfRangeException(nameof(subType), subType, "Unknown SwarmSubType value")
    };

    /// <summary>Parses a SwarmUI API type string (as returned by SwarmUI responses) into a <see cref="SwarmSubType"/> value. Comparison is case-sensitive to match SwarmUI's behavior.</summary>
    /// <returns><c>true</c> if the string matched a known sub-type; otherwise <c>false</c>.</returns>
    public static bool TryParseApiType(string apiType, out SwarmSubType subType)
    {
        switch (apiType)
        {
            case "Stable-Diffusion": subType = SwarmSubType.StableDiffusion; return true;
            case "VAE": subType = SwarmSubType.VAE; return true;
            case "LoRA": subType = SwarmSubType.LoRA; return true;
            case "Embedding": subType = SwarmSubType.Embedding; return true;
            case "ControlNet": subType = SwarmSubType.ControlNet; return true;
            case "Clip": subType = SwarmSubType.Clip; return true;
            case "ClipVision": subType = SwarmSubType.ClipVision; return true;
            case "LLM": subType = SwarmSubType.LLM; return true;
            default: subType = default; return false;
        }
    }
}
