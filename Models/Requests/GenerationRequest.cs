using System;
using System.Collections.Generic;

namespace SwarmUI.ApiClient.Models.Requests;

/// <summary>Request parameters for text-to-image generation via SwarmUI.</summary>
/// <remarks>Used with <c>GenerationEndpoint.StreamGenerationAsync</c> to initiate image generation. See <c>T2IAPI.md</c> for the full JSON schema and backend-specific behavior.</remarks>
public class GenerationRequest
{
    /// <summary>Number of images to generate (deprecated; prefer <see cref="BatchSize"/>).</summary>
    /// <remarks>Kept for API compatibility. For most use cases, set this to 1 and control parallelism via <see cref="BatchSize"/>.</remarks>
    public int Images { get; set; } = 1;

    /// <summary>Text description of what you want to generate. This is the primary input that guides the AI model.</summary>
    /// <value>Required. Cannot be null or empty.</value>
    /// <example>"a beautiful sunset over mountains, vibrant colors, dramatic clouds, 8k quality"</example>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Text description of what you DON'T want in the generated image. Used to guide the model away from undesired elements, artifacts, or styles.</summary>
    /// <example>"blurry, low quality, watermark, text, distorted"</example>
    public string? NegativePrompt { get; set; } = string.Empty;

    /// <summary>Width of generated images in pixels.</summary>
    /// <remarks>Valid ranges depend on the selected model and backend; very large dimensions may exceed available GPU memory.</remarks>
    public int Width { get; set; } = 1024;

    /// <summary>Height of generated images in pixels. Must be compatible with the selected model.</summary>
    public int Height { get; set; } = 768;

    /// <summary>Number of denoising steps to perform during generation.</summary>
    /// <remarks>More steps generally improve quality at the cost of speed. Recommended ranges depend on sampler and model; see the SwarmUI generation documentation.</remarks>
    public int Steps { get; set; } = 20;

    /// <summary>Classifier-free guidance scale controlling how strongly the model follows the prompt.</summary>
    /// <remarks>Higher values increase prompt adherence but can reduce creativity or introduce artifacts. Recommended ranges vary by model family (for example, Flux typically uses lower values).</remarks>
    public float CfgScale { get; set; } = 7.0f;

    /// <summary>Sampling algorithm used to denoise the image.</summary>
    /// <remarks>Available samplers depend on the backend and trade off quality, speed, and determinism. See backend documentation for supported values.</remarks>
    public string Sampler { get; set; } = "dpmpp_2m_sde";

    /// <summary>Noise schedule algorithm controlling how noise is removed across steps. Works in combination with the sampler.</summary>
    public string? Scheduler { get; set; } = "normal";

    /// <summary>Random seed for reproducibility. Using the same seed with identical parameters will produce the same image.</summary>
    public string? Seed { get; set; } = "-1";

    /// <summary>Optional style preset name to apply. Style presets are pre-configured prompt modifications that apply consistent artistic styles.</summary>
    public string? StylePreset { get; set; }

    /// <summary>Number of images to generate in parallel across available GPU backends.</summary>
    /// <remarks>Controls parallel generation in SwarmUI. Each image is an independent job with its own <c>batch_index</c>. Higher values increase concurrency and GPU memory usage.</remarks>
    public int BatchSize { get; set; } = 1;

    /// <summary>Whether to skip saving generated images to SwarmUI's output folder. When true, images are only returned via WebSocket and not persisted to disk on the server.</summary>
    public bool DoNotSave { get; set; } = true;

    /// <summary>Output image format specification. Common values: "PNG", "JPG", "WEBP_LOSSLESS", "WEBP_LOSSY".</summary>
    public string ImageFormat { get; set; } = "PNG";

    /// <summary>Name or path of the model to use for generation. This must match a model available in your SwarmUI instance.</summary>
    /// <remarks>Different models have different capabilities, styles, and recommended resolutions. See the SwarmUI models documentation for guidance.</remarks>
    public string? Model { get; set; }

    /// <summary>List of LoRA (Low-Rank Adaptation) models to apply.</summary>
    /// <remarks>Use LoRAs to adapt the base model toward specific styles, characters, or concepts. Weight typically ranges around 0.5–1.5 depending on the LoRA.</remarks>
    public List<LoraModel>? Loras { get; set; }

    /// <summary>Base64-encoded initial image for img2img generation. When provided, generation starts from this image instead of random noise.</summary>
    public string? InitImage { get; set; }

    /// <summary>Controls how much the output can differ from InitImage in img2img mode. Range: 0.0 to 1.0.</summary>
    /// <remarks>Also known as "denoising strength". Lower values keep the output closer to the input image; higher values allow more change.</remarks>
    public float InitImageCreativity { get; set; } = 0.7f;

    /// <summary>Flux-specific guidance scale parameter. Flux models use a different guidance mechanism than traditional CFG.</summary>
    public string? FluxGuidanceScale { get; set; }

    /// <summary>Sigma shift parameter for SD3, AuraFlow, Flux, and similar models. Controls balance between structural and detail steps.</summary>
    /// <remarks>SD3: 1.5-3 (default 3), AuraFlow: 1.73, Flux-Dev: ~1.15</remarks>
    public float? SigmaShift { get; set; }

    /// <summary>CLIP layer to stop at for SD1.5 models. -1 is default, some models prefer -2.</summary>
    public int? ClipStopAtLayer { get; set; }

    /// <summary>VAE tile size in pixels for reducing VRAM usage during decode.</summary>
    public int? VaeTileSize { get; set; }

    /// <summary>Minimum sigma value for Karras/Exponential schedulers.</summary>
    public float? SamplerSigmaMin { get; set; }

    /// <summary>Maximum sigma value for Karras/Exponential schedulers.</summary>
    public float? SamplerSigmaMax { get; set; }

    /// <summary>Rho value for Karras/Exponential schedulers.</summary>
    public float? SamplerRho { get; set; }

    /// <summary>When true, zeroes the negative prompt if empty. May yield better quality on SD3.</summary>
    public bool? ZeroNegative { get; set; }
}

/// <summary>Represents a LoRA (Low-Rank Adaptation) model to apply during generation.</summary>
/// <remarks>Multiple LoRAs can be combined; each has a <see cref="Weight"/> controlling how strongly it influences the result.</remarks>
public class LoraModel
{
    /// <summary>Name or path of the LoRA model file. Must match a LoRA available in your SwarmUI instance.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Strength multiplier for this LoRA's effect (typical range 0.5–1.5, default 1.0).</summary>
    /// <remarks>Some LoRAs are trained for specific weights; check the LoRA's documentation or experiment to find an appropriate value.</remarks>
    public float Weight { get; set; } = 1.0f;
}
