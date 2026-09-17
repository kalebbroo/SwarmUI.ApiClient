using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>Request parameters for text-to-image generation via SwarmUI.</summary>
/// <remarks>Every property carries its exact SwarmUI wire name via <see cref="JsonPropertyAttribute"/> — the payload is serialized from these attributes, so a property without one does not reach the server. SwarmUI silently drops unrecognized parameter names (it normalizes incoming keys to lowercase letters before matching), which is why each name here was verified against the server's registered parameter list. Extension-region parameters require the corresponding server extension to be installed.</remarks>
public partial class GenerationRequest
{
    /// <summary>Number of images to generate for this request. Each image is an independent job with its own <c>batch_index</c>.</summary>
    /// <remarks>Server limit: 1–10000.</remarks>
    [JsonProperty("images")]
    public int Images { get; set; } = 1;

    /// <summary>Per-backend batch size: how many images each backend generates simultaneously in one pass.</summary>
    /// <remarks>Total images produced = <see cref="Images"/> × <see cref="BatchSize"/> in SwarmUI's accounting; most callers should set <see cref="Images"/> and leave this at 1. Higher values increase GPU memory usage. Server limit: 1–100.</remarks>
    [JsonProperty("batchsize")]
    public int BatchSize { get; set; } = 1;

    /// <summary>Text description of what you want to generate. This is the primary input that guides the AI model.</summary>
    /// <value>Required. Cannot be null or empty.</value>
    /// <example>"a beautiful sunset over mountains, vibrant colors, dramatic clouds, 8k quality"</example>
    /// <remarks>For music models this carries the <b>lyrics</b>, not the style — genre goes in
    /// <see cref="Text2AudioStyle"/>. Getting those the wrong way round produces a plausible song that sings the
    /// genre tags, which no error will warn you about.</remarks>
    [JsonProperty("prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Text description of what you DON'T want in the generated image. Omitted from the payload when empty.</summary>
    /// <example>"blurry, low quality, watermark, text, distorted"</example>
    [JsonProperty("negativeprompt")]
    public string? NegativePrompt { get; set; } = string.Empty;

    /// <summary>Width of generated images in pixels.</summary>
    [JsonProperty("width")]
    public int Width { get; set; } = 1024;

    /// <summary>Height of generated images in pixels.</summary>
    [JsonProperty("height")]
    public int Height { get; set; } = 768;

    /// <summary>Number of denoising steps to perform during generation.</summary>
    [JsonProperty("steps")]
    public int Steps { get; set; } = 20;

    /// <summary>Classifier-free guidance scale controlling how strongly the model follows the prompt.</summary>
    [JsonProperty("cfgscale")]
    public float CfgScale { get; set; } = 7.0f;

    /// <summary>Sampling algorithm used to denoise the image.</summary>
    /// <remarks>Leave null for models with a built-in solver; some video models reject an explicit sampler.</remarks>
    [JsonProperty("sampler")]
    public string? Sampler { get; set; } = "dpmpp_2m_sde";

    /// <summary>Noise schedule algorithm controlling how noise is removed across steps. Works in combination with the sampler.</summary>
    [JsonProperty("scheduler")]
    public string? Scheduler { get; set; } = "normal";

    /// <summary>Random seed for reproducibility. -1 (default) requests a random seed and is omitted from the payload.</summary>
    [JsonProperty("seed")]
    public long Seed { get; set; } = -1;

    /// <summary>Whether to skip saving generated images to SwarmUI's output folder. When true, images are returned as base64 data URLs instead of server file paths.</summary>
    [JsonProperty("donotsave")]
    public bool DoNotSave { get; set; } = true;

    /// <summary>Whether to skip saving intermediate (non-final) images, such as segmentation masks or refiner stages.</summary>
    [JsonProperty("donotsaveintermediates")]
    public bool? DoNotSaveIntermediates { get; set; }

    /// <summary>Output image format specification. Common values: "PNG", "JPG", "WEBP_LOSSLESS", "WEBP_LOSSY".</summary>
    [JsonProperty("imageformat")]
    public string ImageFormat { get; set; } = "PNG";

    /// <summary>Name or path of the model to use for generation. Must match a model available in your SwarmUI instance. Omitted from the payload when empty.</summary>
    [JsonProperty("model")]
    public string? Model { get; set; }

    /// <summary>Names of SwarmUI presets to apply to this request. Presets are applied server-side on top of the explicit parameters.</summary>
    [JsonProperty("presets")]
    public List<string>? Presets { get; set; }

    /// <summary>List of LoRA (Low-Rank Adaptation) models to apply.</summary>
    /// <remarks>Serialized as parallel <c>loras</c>/<c>loraweights</c> JSON arrays (safe for names containing commas); weights keep full precision.</remarks>
    [JsonIgnore]
    public List<LoraModel>? Loras { get; set; }

    /// <summary>Base64-encoded initial image (or data URL) for img2img generation. When provided, generation starts from this image instead of random noise.</summary>
    [JsonProperty("initimage")]
    public string? InitImage { get; set; }

    /// <summary>Controls how much the output can differ from InitImage in img2img mode. Range: 0.0 to 1.0. Only sent when <see cref="InitImage"/> is set.</summary>
    /// <remarks>0.6 matches the server's own default.</remarks>
    [JsonProperty("initimagecreativity")]
    public float InitImageCreativity { get; set; } = 0.6f;

    /// <summary>Aspect ratio (e.g. "1:1", "16:9"). SwarmUI derives width/height from this when set to a non-custom value.</summary>
    [JsonProperty("aspectratio")]
    public string? AspectRatio { get; set; }

    /// <summary>Flux guidance scale for Flux-Dev and related models (distilled guidance, not CFG). 3.5 is the model default.</summary>
    [JsonProperty("fluxguidancescale")]
    public double? FluxGuidanceScale { get; set; }

    /// <summary>Sigma shift parameter for rectified-flow models (SD3, AuraFlow, Flux, HiDream).</summary>
    /// <remarks>SD3: 1.5-3 (default 3), AuraFlow: 1.73, Flux-Dev: ~1.15</remarks>
    [JsonProperty("sigmashift")]
    public double? SigmaShift { get; set; }

    /// <summary>CLIP layer to stop at for SD1.5 models. -1 is default, some models prefer -2.</summary>
    [JsonProperty("clipstopatlayer")]
    public int? ClipStopAtLayer { get; set; }

    /// <summary>VAE tile size in pixels for reducing VRAM usage during decode.</summary>
    [JsonProperty("vaetilesize")]
    public int? VaeTileSize { get; set; }

    /// <summary>Minimum sigma value for Karras/Exponential schedulers.</summary>
    [JsonProperty("samplersigmamin")]
    public double? SamplerSigmaMin { get; set; }

    /// <summary>Maximum sigma value for Karras/Exponential schedulers.</summary>
    [JsonProperty("samplersigmamax")]
    public double? SamplerSigmaMax { get; set; }

    /// <summary>Rho value for Karras/Exponential schedulers.</summary>
    [JsonProperty("samplerrho")]
    public double? SamplerRho { get; set; }

    /// <summary>When true, zeroes the negative prompt if empty. May yield better quality on SD3.</summary>
    [JsonProperty("zeronegative")]
    public bool? ZeroNegative { get; set; }

    #region Refine / Upscale
    /// <summary>Model for the refiner stage ("Refiner Model"). Naming one is what enables the stage.</summary>
    /// <remarks>There is no separate "use refiner" switch. The server treats <c>"(Use Base)"</c> as off, which is
    /// also this parameter's default, so leaving it unset and sending that string mean the same thing.</remarks>
    [JsonProperty("refinermodel")]
    public string? RefinerModel { get; set; }

    /// <summary>Fraction of the total steps the refiner runs for ("Refiner Control Percentage"). Server range 0–1, default 0.2.</summary>
    [JsonProperty("refinercontrolpercentage")]
    public float? RefinerControlPercentage { get; set; }

    /// <summary>How the refiner is applied ("Refiner Method"): PostApply, StepSwap, StepSwapNoisy.</summary>
    [JsonProperty("refinermethod")]
    public string? RefinerMethod { get; set; }

    /// <summary>Upscale factor applied between the base and refiner stages ("Refiner Upscale"). Server range 0.25–8.</summary>
    /// <remarks>The server treats 1 as off, which is also the default.</remarks>
    [JsonProperty("refinerupscale")]
    public float? RefinerUpscale { get; set; }

    /// <summary>Algorithm for that upscale ("Refiner Upscale Method"), for example pixel-lanczos, latent-bislerp, real-esrgan-x4plus.</summary>
    [JsonProperty("refinerupscalemethod")]
    public string? RefinerUpscaleMethod { get; set; }

    /// <summary>Step count the refiner's control percentage is calculated against ("Refiner Steps"). Server range 1–200.</summary>
    [JsonProperty("refinersteps")]
    public int? RefinerSteps { get; set; }

    /// <summary>CFG scale for the refiner stage alone ("Refiner CFG Scale"). Server range 0–100.</summary>
    [JsonProperty("refinercfgscale")]
    public float? RefinerCfgScale { get; set; }

    /// <summary>Sampler for the refiner stage alone ("Refiner Sampler").</summary>
    [JsonProperty("refinersampler")]
    public string? RefinerSampler { get; set; }

    /// <summary>Scheduler for the refiner stage alone ("Refiner Scheduler").</summary>
    [JsonProperty("refinerscheduler")]
    public string? RefinerScheduler { get; set; }

    /// <summary>VAE replacement for the refiner stage ("Refiner VAE"). The server treats <c>"None"</c> as off.</summary>
    [JsonProperty("refinervae")]
    public string? RefinerVae { get; set; }

    /// <summary>Whether the refiner stage tiles its generation ("Refiner Do Tiling"). The server treats false as off.</summary>
    [JsonProperty("refinerdotiling")]
    public bool? RefinerDoTiling { get; set; }

    /// <summary>HyperTile size for the refiner stage ("Refiner HyperTile"). Server range 1–1024.</summary>
    [JsonProperty("refinerhypertile")]
    public int? RefinerHyperTile { get; set; }
    #endregion

    #region Model add-ons
    /// <summary>VAE override for the whole generation ("VAE"). <c>"Automatic"</c> lets the model pick; <c>"None"</c> is off.</summary>
    [JsonProperty("vae")]
    public string? Vae { get; set; }

    /// <summary>Per-LoRA text-encoder weights ("LoRA Tenc Weights"), comma separated and positionally matched to the LoRA list.</summary>
    /// <remarks><see cref="Loras"/> fills the <c>loras</c> and <c>loraweights</c> arrays; this is the third, optional
    /// array alongside them, so its entry count must match.</remarks>
    [JsonProperty("loratencweights")]
    public string? LoraTencWeights { get; set; }

    /// <summary>Per-LoRA prompt-section confinement ("LoRA Section Confinement"), comma separated and positionally matched to the LoRA list.</summary>
    [JsonProperty("lorasectionconfinement")]
    public string? LoraSectionConfinement { get; set; }

    /// <summary>Whether the negative prompt pass also applies the LoRAs ("Negative Model Include LoRAs"). Server default true.</summary>
    [JsonProperty("negativemodelincludeloras")]
    public bool? NegativeModelIncludeLoras { get; set; }
    #endregion

    #region Text To Audio
    /// <summary>How long the generated audio clip should be, in seconds ("Text2Audio Duration"). Server range 1–1000.</summary>
    /// <remarks>Read as a ceiling by some models — short lyrics give a short song — and as a target by others, which
    /// may stretch to fit. AudioLab's backend reads this first and falls back to its own <see cref="MaxDuration"/>,
    /// so a request that sets both is steered by this one.</remarks>
    [JsonProperty("textaudioduration")]
    public float? Text2AudioDuration { get; set; }

    /// <summary>Style or genre of the generated audio ("Text2Audio Style"), for example "upbeat indie pop, female vocals".</summary>
    /// <remarks>This is where genre goes for ACE-Step, YuE2 and MiniMax Music 3; the <b>lyrics go in
    /// <see cref="Prompt"/></b>. Those three dropped their own lyrics parameters on 2026-09-16 to match this
    /// convention, so a request that puts genre in <see cref="Prompt"/> gets a song that sings the genre tags.</remarks>
    [JsonProperty("textaudiostyle")]
    public string? Text2AudioStyle { get; set; }

    /// <summary>Tempo in beats per minute ("Text2Audio BPM"). Server range 10–300.</summary>
    /// <remarks>Gated behind the <c>audio_ace_inputs</c> feature flag. Unlike AudioLab's retired <c>bpm</c>
    /// parameter there is no 0 meaning "let the model choose" — the floor is 10, so omit it instead.</remarks>
    [JsonProperty("textaudiobpm")]
    public long? Text2AudioBpm { get; set; }

    /// <summary>Musical key and scale ("Text2Audio Key Scale"), for example "C major". Gated behind <c>audio_ace_inputs</c>.</summary>
    [JsonProperty("textaudiokeyscale")]
    public string? Text2AudioKeyScale { get; set; }

    /// <summary>Beats per bar ("Text2Audio Time Signature"): 2, 3, 4, 6. Gated behind <c>audio_ace_inputs</c>.</summary>
    [JsonProperty("textaudiotimesignature")]
    public string? Text2AudioTimeSignature { get; set; }

    /// <summary>Language sung in the vocals ("Text2Audio Language"), for example "en" or "ja". Gated behind <c>audio_ace_inputs</c>.</summary>
    [JsonProperty("textaudiolanguage")]
    public string? Text2AudioLanguage { get; set; }

    /// <summary>Container for the returned audio ("Audio Format"): mp3, wav, flac, ogg.</summary>
    /// <remarks>Stock SwarmUI parameter. AudioLab registers its own <see cref="AudioOutputFormat"/> separately.</remarks>
    [JsonProperty("audioformat")]
    public string? AudioFormat { get; set; }
    #endregion

    #region Video
    /// <summary>Frame count for text-to-video ("Text-To-Video Frames"). Server range 1–1000.</summary>
    /// <remarks>Duration in seconds is this divided by <see cref="VideoFps"/>. Image-to-video is a separate
    /// parameter, <see cref="ImageToVideoFrames"/>; this one does not reach it.</remarks>
    [JsonProperty("textvideoframes")]
    public int? VideoFrames { get; set; }

    /// <summary>Frame count for image-to-video ("Video Frames"). Server range 1–1000, default 25.</summary>
    [JsonProperty("videoframes")]
    public int? ImageToVideoFrames { get; set; }

    /// <summary>Output frame rate ("Video FPS"). Server range 1–1024, default 24.</summary>
    [JsonProperty("videofps")]
    public int? VideoFps { get; set; }

    /// <summary>Container/codec for the returned video ("Video Format"): webp, gif, gif-hd, webm,
    /// h264-mp4, h265-mp4, prores.</summary>
    [JsonProperty("videoformat")]
    public string? VideoFormat { get; set; }
    #endregion

    #region API Backends extension — Black Forest Labs (Flux via API)
    // These parameters require the SwarmUI-API-Backends server extension.

    /// <summary>BFL content moderation level ("Safety Filter Level"). 0 = strictest, 6 = most permissive.</summary>
    [JsonProperty("safetyfilterlevel")]
    public int? SafetyTolerance { get; set; }

    /// <summary>BFL output image format ("Output Format"). jpeg or png.</summary>
    [JsonProperty("outputformat")]
    public string? OutputFormat { get; set; }

    /// <summary>BFL prompt guidance scale ("Prompt Guidance"). Controls how closely the output follows the prompt.</summary>
    [JsonProperty("promptguidance")]
    public double? Guidance { get; set; }

    /// <summary>Whether BFL should enhance/upsample the prompt before generation ("Prompt Enhancement").</summary>
    [JsonProperty("promptenhancement")]
    public bool? PromptUpsampling { get; set; }
    #endregion

    #region API Backends extension — OpenAI (DALL-E, GPT-Image)
    /// <summary>Image quality for GPT-Image models ("Quality"): auto/high/medium/low.</summary>
    /// <remarks>DALL-E 3's separate hd/standard quality parameter is "Generation Quality" (<c>generationquality</c>) server-side.</remarks>
    [JsonProperty("quality")]
    public string? OpenAIQuality { get; set; }

    /// <summary>DALL-E 3 visual style ("Visual Style"): vivid = hyper-real/dramatic, natural = more realistic.</summary>
    [JsonProperty("visualstyle")]
    public string? OpenAIStyle { get; set; }

    /// <summary>OpenAI output resolution ("Output Resolution"). Model-specific allowed values.</summary>
    [JsonProperty("outputresolution")]
    public string? OpenAISize { get; set; }

    /// <summary>GPT-Image background transparency ("Background"): auto/transparent/opaque.</summary>
    [JsonProperty("background")]
    public string? OpenAIBackground { get; set; }

    /// <summary>GPT-Image content moderation level ("Content Moderation"): auto/low.</summary>
    [JsonProperty("contentmoderation")]
    public string? OpenAIModeration { get; set; }

    /// <summary>GPT-Image output format ("Image Output Format"): png/jpeg/webp.</summary>
    [JsonProperty("imageoutputformat")]
    public string? OpenAIOutputFormat { get; set; }
    #endregion

    #region API Backends extension — Ideogram
    /// <summary>Ideogram aspect ratio ("Ideogram Aspect Ratio"), e.g. "1:1", "16:9". Defaults to 1:1 server-side.</summary>
    [JsonProperty("ideogramaspectratio")]
    public string? IdeogramAspectRatio { get; set; }

    /// <summary>Ideogram V3 rendering speed ("Rendering Speed"): DEFAULT, TURBO, QUALITY.</summary>
    [JsonProperty("renderingspeed")]
    public string? IdeogramRenderingSpeed { get; set; }

    /// <summary>Ideogram V4 rendering speed ("Ideogram V4 Rendering Speed").</summary>
    [JsonProperty("ideogramvrenderingspeed")]
    public string? IdeogramV4RenderingSpeed { get; set; }

    /// <summary>Ideogram MagicPrompt mode ("Magic Prompt Enhancement"): AUTO, ON, OFF.</summary>
    [JsonProperty("magicpromptenhancement")]
    public string? IdeogramMagicPrompt { get; set; }

    /// <summary>Ideogram color palette preset ("Color Theme"), e.g. "EMBER", "FRESH", "JUNGLE".</summary>
    [JsonProperty("colortheme")]
    public string? IdeogramColorPalette { get; set; }

    /// <summary>Ideogram style ("Generation Style"): GENERAL, REALISTIC, DESIGN, RENDER_3D, ANIME.</summary>
    [JsonProperty("generationstyle")]
    public string? IdeogramStyleType { get; set; }
    #endregion

    #region API Backends extension — Google (Gemini, Imagen)
    /// <summary>Google aspect ratio ("Google Aspect Ratio"): 1:1, 3:4, 4:3, 9:16, 16:9. Applies to Gemini and Imagen models.</summary>
    [JsonProperty("googleaspectratio")]
    public string? GoogleAspectRatio { get; set; }

    /// <summary>Gemini image resolution ("Gemini Image Resolution"): 1K, 2K, 4K.</summary>
    [JsonProperty("geminiimageresolution")]
    public string? GoogleGeminiImageSize { get; set; }

    /// <summary>Imagen image size ("Google Image Size"): 1K, 2K.</summary>
    [JsonProperty("googleimagesize")]
    public string? GoogleImagenSize { get; set; }

    /// <summary>Imagen person generation mode ("Person Generation"): dont_allow, allow_adult, allow_all.</summary>
    [JsonProperty("persongeneration")]
    public string? GoogleImagenPersonGeneration { get; set; }
    #endregion

    #region API Backends extension — Grok
    /// <summary>Grok aspect ratio ("Grok Aspect Ratio").</summary>
    [JsonProperty("grokaspectratio")]
    public string? GrokAspectRatio { get; set; }

    /// <summary>Grok output resolution ("Grok Output Resolution").</summary>
    [JsonProperty("grokoutputresolution")]
    public string? GrokOutputResolution { get; set; }
    #endregion

    /// <summary>Newtonsoft conditional serialization: omit seed when random (-1).</summary>
    public bool ShouldSerializeSeed() => Seed != -1;

    /// <summary>Newtonsoft conditional serialization: omit empty negative prompt.</summary>
    public bool ShouldSerializeNegativePrompt() => !string.IsNullOrEmpty(NegativePrompt);

    /// <summary>Newtonsoft conditional serialization: omit empty model.</summary>
    public bool ShouldSerializeModel() => !string.IsNullOrEmpty(Model);

    /// <summary>Newtonsoft conditional serialization: init image creativity only makes sense with an init image.</summary>
    public bool ShouldSerializeInitImageCreativity() => !string.IsNullOrEmpty(InitImage);

    /// <summary>Newtonsoft conditional serialization: omit empty preset list.</summary>
    public bool ShouldSerializePresets() => Presets is { Count: > 0 };
}

/// <summary>Represents a LoRA (Low-Rank Adaptation) model to apply during generation.</summary>
/// <remarks>Multiple LoRAs can be combined; each has a <see cref="Weight"/> controlling how strongly it influences the result.</remarks>
public class LoraModel
{
    /// <summary>Name or path of the LoRA model file. Must match a LoRA available in your SwarmUI instance.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Strength multiplier for this LoRA's effect (typical range 0.5–1.5, default 1.0).</summary>
    public float Weight { get; set; } = 1.0f;
}
