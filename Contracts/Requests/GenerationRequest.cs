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

    // The regions below fill a coverage gap: this file previously mapped only a curated subset of
    // SwarmUI's registered T2I parameters. They were generated by diffing a live ListT2IParams response
    // against the properties already declared here, so every stock/ComfyUI parameter the server
    // registers now has a real property — see the class remarks above for why that matters.

    #region Image Prompting
    /// <summary>How strong to apply ReVision image inputs. ("ReVision Strength").</summary>
    /// <remarks>Set to 0 to disable ReVision processing. Server range 0–10. Server default: <c>0</c>. Gated behind the <c>sdxl</c> feature flag.</remarks>
    [JsonProperty("revisionstrength")]
    public float? ReVisionStrength { get; set; }

    /// <summary>Zeroes the prompt and negative prompt for ReVision inputs. ("ReVision Zero Prompt").</summary>
    /// <remarks>Applies only to the base, the refiner will still get prompts. If you want zeros on both, just delete your prompt text. If not checked, empty prompts will be zeroed regardless. Server default: <c>false</c>. Gated behind the <c>sdxl</c> feature flag.</remarks>
    [JsonProperty("revisionzeroprompt")]
    public bool? ReVisionZeroPrompt { get; set; }

    /// <summary>Use the 'Reference-Only' technique to guide the generation towards the input image. ("Use Reference Only").</summary>
    /// <remarks>This currently has side effects that notably prevent Batch from being used properly. Server default: <c>false</c>. Gated behind the <c>supports_reference_only</c> feature flag.</remarks>
    [JsonProperty("usereferenceonly")]
    public bool? UseReferenceOnly { get; set; }

    /// <summary>When enabled, input images for the image prompt will be intelligently resized to a scale appropriate to the model. ("Smart Image Prompt Resizing").</summary>
    /// <remarks>If disabled, images will be either unscaled, or scaled to the current generation parameter size. It is almost always best to leave this on. Server default: <c>true</c>.</remarks>
    [JsonProperty("smartimagepromptresizing")]
    public bool? SmartImagePromptResizing { get; set; }

    /// <summary>How to feed prompt images as Reference Latents to the model. ("Enable Reference Latents").</summary>
    /// <remarks>None leaves images on the text encoder only (correct for the Krea 2 base model). Index Timestep Zero is for Ostris-style edit LoRAs. Index is for Identity Edit LoRAs. Allowed values: <c>none</c>, <c>index_timestep_zero</c>, <c>index</c>. Gated behind the <c>optional_reference_latent</c> feature flag.</remarks>
    [JsonProperty("enablereferencelatents")]
    public string? EnableReferenceLatents { get; set; }

    /// <summary>How to feed prompt images into the text encoder. ("Text Encoded Image").</summary>
    /// <remarks>Automatic uses the model's default (usually this is large or exact-size-as-input). None skips text-encoder images (reference latents can still apply). Small targets 384px, Large targets 1024px. Allowed values: <c>auto</c>, <c>none</c>, <c>small</c>, <c>large</c>.</remarks>
    [JsonProperty("textencodedimage")]
    public string? TextEncodedImage { get; set; }

    /// <summary>Select a Style model to use it for image-prompt input handling. ("Use Style Model").</summary>
    /// <remarks>Flux.1 Redux is an example of a style model. Place these models in `(Swarm)/Models/style_models`. Allowed values: <c>None</c>. Gated behind the <c>flux-dev</c> feature flag.</remarks>
    [JsonProperty("usestylemodel")]
    public string? UseStyleModel { get; set; }

    /// <summary>How strongly to merge in the effects of the style model. ("Style Model Merge Strength").</summary>
    /// <remarks>At 1, the style model is fully used. At 0, the style model is ignored. For Flux Redux, very low values (eg 0.1) are recommended. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>usestylemodel</c> is set.</remarks>
    [JsonProperty("stylemodelmergestrength")]
    public float? StyleModelMergeStrength { get; set; }

    /// <summary>How strongly to multiply the effects of the style model. ("Style Model Multiply Strength").</summary>
    /// <remarks>At 1, the style model is fully used. At 0, the style model is ignored. For Flux Redux, very low values (eg 0.1) are recommended. Server range 0–10. Server default: <c>1</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>usestylemodel</c> is set.</remarks>
    [JsonProperty("stylemodelmultiplystrength")]
    public float? StyleModelMultiplyStrength { get; set; }

    /// <summary>When to start applying the Style Model, as a fraction of steps (if enabled). ("Style Model Apply Start").</summary>
    /// <remarks>For example, 0.25 starts applying a quarter (25%) of the way through. This is probably off-scale due to scheduler behavior in ComfyUI internals. Very low values are recommend for practical usage. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>usestylemodel</c> is set.</remarks>
    [JsonProperty("stylemodelapplystart")]
    public float? StyleModelApplyStart { get; set; }

    /// <summary>Select an IP-Adapter model to use IP-Adapter for image-prompt input handling. ("Use IP-Adapter").</summary>
    /// <remarks>Models will automatically be downloaded when you first use them. Note if you use a custom model, you must also set your CLIP-Vision Model under Advanced Model Addons, otherwise CLIP Vision G will be presumed. See more docs here. Allowed values: <c>None</c>. Gated behind the <c>ipadapter</c> and <c>model_has_ipadapter</c> feature flags.</remarks>
    [JsonProperty("useipadapter")]
    public string? UseIpAdapter { get; set; }

    /// <summary>Weight to use with IP-Adapter (if enabled). ("IP-Adapter Weight").</summary>
    /// <remarks>Server range -1–3. Server default: <c>1</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("ipadapterweight")]
    public float? IpAdapterWeight { get; set; }

    /// <summary>When to start applying IP-Adapter, as a fraction of steps (if enabled). ("IP-Adapter Start").</summary>
    /// <remarks>For example, 0.25 starts applying a quarter (25%) of the way through. Must be less than IP-Adapter End. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("ipadapterstart")]
    public float? IpAdapterStart { get; set; }

    /// <summary>When to stop applying IP-Adapter, as a fraction of steps (if enabled). ("IP-Adapter End").</summary>
    /// <remarks>For example, 0.5 stops applying halfway (50%) through. Must be greater than IP-Adapter Start. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("ipadapterend")]
    public float? IpAdapterEnd { get; set; }

    /// <summary>How to shift the weighting of the IP-Adapter. ("IP-Adapter Weight Type").</summary>
    /// <remarks>This can produce subtle but useful different effects. Allowed values: <c>standard</c>, <c>prompt is more important</c>, <c>style transfer</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("ipadapterweighttype")]
    public string? IpAdapterWeightType { get; set; }

    /// <summary>Strength of the FaceID-PlusV2 CLIP-face shortcut mix (the official pipeline's 's_scale'). ("FaceID V2 Weight").</summary>
    /// <remarks>Higher = the CLIP appearance of the face crop contributes more on top of the ArcFace identity tokens. Only used with ip-adapter-faceid-plusv2 models; 1.0 is the official default. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("faceidvweight")]
    public float? FaceIDV2Weight { get; set; }

    #endregion

    #region Variation Seed
    /// <summary>Image-variation seed. ("Variation Seed").</summary>
    /// <remarks>Combined partially with the original seed to create a similar-but-different image for the same seed. -1 = random. Server range -1–4294967295. Server default: <c>-1</c>. Gated behind the <c>variation_seed</c> feature flag.</remarks>
    [JsonProperty("variationseed")]
    public long? VariationSeed { get; set; }

    /// <summary>How strongly to apply the variation seed. ("Variation Seed Strength").</summary>
    /// <remarks>0 = don't use, 1 = replace the base seed entirely. 0.5 is a good value. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>variation_seed</c> feature flag.</remarks>
    [JsonProperty("variationseedstrength")]
    public float? VariationSeedStrength { get; set; }

    #endregion

    #region Resolution
    /// <summary>Image Side Length, in pixels. ("Side Length").</summary>
    /// <remarks>This value is only used with Aspect Ratio not set to 'Custom'. If unchecked, the model native size is used. SDv1 uses 512, SDv2 uses 768, SDXL prefers 1024. Some models allow variation within a range (eg 512 to 768) but almost always want a multiple of 64. Flux is very open to differing values. Server range 64–16384. Server default: <c>1024</c>.</remarks>
    [JsonProperty("sidelength")]
    public int? SideLength { get; set; }

    #endregion

    #region Sampling
    /// <summary>How deeply to compress latents when using Stable Cascade. ("Cascade Latent Compression").</summary>
    /// <remarks>Default is 32, you can get slightly faster but lower quality results by using 42. Server range 1–100. Server default: <c>32</c>. Gated behind the <c>cascade</c> feature flag.</remarks>
    [JsonProperty("cascadelatentcompression")]
    public int? CascadeLatentCompression { get; set; }

    /// <summary>Which text encoders to use for Stable Diffusion 3 (SD3) models. ("SD3 TextEncs").</summary>
    /// <remarks>Can use CLIP pairs, or T5, or both. Both is the standard way to run SD3, but CLIP only uses fewer system resources. Allowed values: <c>CLIP Only</c>, <c>T5 Only</c>, <c>CLIP + T5</c>. Gated behind the <c>sd3</c> feature flag.</remarks>
    [JsonProperty("sdtextencs")]
    public string? SD3TextEncs { get; set; }

    /// <summary>Disables Flux Guidance Scale. ("Flux Disable Guidance").</summary>
    /// <remarks>Some models prefer this. Usually you don't need this. Server default: <c>false</c>. Gated behind the <c>flux-dev</c> feature flag.</remarks>
    [JsonProperty("fluxdisableguidance")]
    public bool? FluxDisableGuidance { get; set; }

    /// <summary>Makes the generated image seamlessly tileable (like a 3D texture would be). ("Seamless Tileable").</summary>
    /// <remarks>Optionally, can be tileable on only the X axis (horizontal) or Y axis (vertical). Only compatible with UNet models (such as SDXL), and not with DiT models (such as Flux). Allowed values: <c>false</c>, <c>true</c>, <c>X-Only</c>, <c>Y-Only</c>. Gated behind the <c>seamless</c> feature flag.</remarks>
    [JsonProperty("seamlesstileable")]
    public string? SeamlessTileable { get; set; }

    #endregion

    #region Init Image
    /// <summary>Merges the init image towards the latent norm. ("Init Image Reset To Norm").</summary>
    /// <remarks>This essentially lets you boost 'init image creativity' past 1.0. Set to 0 to disable. Server range 0–1. Server default: <c>0</c>.</remarks>
    [JsonProperty("initimageresettonorm")]
    public float? InitImageResetToNorm { get; set; }

    /// <summary>Adds non-latent image noise to the Init Image. ("Init Image Noise").</summary>
    /// <remarks>This is simple Gaussian noise directly on top of the image. This tends to encourage more complex/creative generations from diffusion models. Especially helpful when the init is a flat color reference. At 0, no noise is added. At 1, heavy noise is added. You can overload up to 10 to more fully hide the source image if needed. Server range 0–10. Server default: <c>0</c>.</remarks>
    [JsonProperty("initimagenoise")]
    public float? InitImageNoise { get; set; }

    /// <summary>Mask-image, white pixels are changed, black pixels are not changed, gray pixels are half-changed. ("Mask Image").</summary>
    [JsonProperty("maskimage")]
    public string? MaskImage { get; set; }

    /// <summary>If enabled, the image will be shrunk to just the mask, and then grow by this value many pixels. ("Mask Shrink Grow").</summary>
    /// <remarks>After that, the generation process will run in full, and the image will be composited back into the original image at the end. This allows for refining small details of an image more effectively. This is also known as 'Inpaint Only Masked'. Larger values increase the surrounding context the generation receives, lower values contain it tighter and allow the AI to create more detail. Server range 0–512. Server default: <c>8</c>. Does nothing unless <c>maskimage</c> is set.</remarks>
    [JsonProperty("maskshrinkgrow")]
    public int? MaskShrinkGrow { get; set; }

    /// <summary>If enabled, the mask will be blurred by this blur factor. ("Mask Blur").</summary>
    /// <remarks>This makes the transition for the new image smoother. Set to 0 to disable. Server range 0–64. Server default: <c>4</c>. Does nothing unless <c>maskimage</c> is set.</remarks>
    [JsonProperty("maskblur")]
    public int? MaskBlur { get; set; }

    /// <summary>If enabled, the mask will be grown by this size (approx equivalent to length in pixels). ("Mask Grow").</summary>
    /// <remarks>This helps improve overlap with generated masks. Set to 0 to disable. Server range 0–256. Server default: <c>0</c>. Does nothing unless <c>maskimage</c> is set.</remarks>
    [JsonProperty("maskgrow")]
    public int? MaskGrow { get; set; }

    /// <summary>If enabled and a mask is in use, this will recomposite the masked generated onto the original image for a cleaner result. ("Init Image Recomposite Mask").</summary>
    /// <remarks>If disabled, VAE artifacts may build up across repeated inpaint operations. Defaults enabled. Server default: <c>true</c>. Does nothing unless <c>maskimage</c> is set.</remarks>
    [JsonProperty("initimagerecompositemask")]
    public bool? InitImageRecompositeMask { get; set; }

    /// <summary>Uses VAE Encode logic specifically designed for certain inpainting models. ("Use Inpainting Encode").</summary>
    /// <remarks>Notably this includes the RunwayML Stable-Diffusion-v1 Inpainting model. This covers the masked area with gray. Server default: <c>false</c>.</remarks>
    [JsonProperty("useinpaintingencode")]
    public bool? UseInpaintingEncode { get; set; }

    /// <summary>If enabled, feeds this prompt to an unsampler before resampling with your main prompt. ("Unsampler Prompt").</summary>
    /// <remarks>This is powerful for controlled image editing. For example, use unsampler prompt 'a photo of a man wearing a black hat', and give main prompt 'a photo of a man wearing a sombrero', to change what type of hat a person is wearing.</remarks>
    [JsonProperty("unsamplerprompt")]
    public string? UnsamplerPrompt { get; set; }

    #endregion

    #region SeedVR
    /// <summary>Which SeedVR2 model to restore with. ("SeedVR Model").</summary>
    /// <remarks>Server default: <c>None</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("seedvrmodel")]
    public string? SeedVRModel { get; set; }

    /// <summary>Optional downscale of the image immediately before upscaling it back. ("SeedVR Pre-Downscale").</summary>
    /// <remarks>Setting to '1' disables this behavior. Pre-downscaling to degrade the image can help improve quality (reduces oversharpening). Server range 0–1. Server default: <c>1</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("seedvrpredownscale")]
    public float? SeedVRPreDownscale { get; set; }

    /// <summary>Optional upscale of the image before SeedVR2 runs over it. ("SeedVR Upscale").</summary>
    /// <remarks>Setting to '1' disables the upscale, and just restores at the current size. Server range 0.25–8. Server default: <c>1</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("seedvrupscale")]
    public float? SeedVRUpscale { get; set; }

    /// <summary>How to upscale the image before SeedVR2 runs over it, if upscaling is used. ("SeedVR Upscale Method").</summary>
    /// <remarks>Allowed values: <c>pixel-lanczos</c>, <c>pixel-bicubic</c>, <c>pixel-area</c>, <c>pixel-bilinear</c>, <c>pixel-nearest-exact</c>, <c>latent-bislerp</c>, <c>latent-bicubic</c>, <c>latent-area</c>, <c>latent-bilinear</c>, <c>latent-nearest-exact</c>, <c>real-esrgan-x4plus</c>, <c>real-esrgan-x2plus</c>, <c>real-esrgan-anime6b</c>, <c>seedvr2</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>seedvrupscale</c> is set.</remarks>
    [JsonProperty("seedvrupscalemethod")]
    public string? SeedVRUpscaleMethod { get; set; }

    /// <summary>How to match the colors of a SeedVR2 restore back to the image it was given. ("SeedVR Color Correction Behavior").</summary>
    /// <remarks>'None' = Do not attempt color correction, only align the geometry. 'CIELAB' = Transfer the color in CIELAB space, preserving detail. 'Wavelet' = Transfer the low-frequency color, keeping the upscaled high-frequency detail. 'AdaIN' = Match the per-channel mean and standard deviation. Allowed values: <c>none</c>, <c>lab</c>, <c>wavelet</c>, <c>adain</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("seedvrcolorcorrectionbehavior")]
    public string? SeedVRColorCorrectionBehavior { get; set; }

    /// <summary>If enabled, samples a SeedVR2 video restore as chunks of frames instead of all at once, sized to fit in free VRAM. ("SeedVR Split Latent").</summary>
    /// <remarks>Chunking reduces VRAM consumption. Does nothing to a single image, or to a video that already fits. Server default: <c>false</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("seedvrsplitlatent")]
    public bool? SeedVRSplitLatent { get; set; }

    /// <summary>How many latent frames of overlap to keep between 'SeedVR Split Latent' chunks. ("SeedVR Temporal Video Overlap").</summary>
    /// <remarks>Higher overlap hides the chunk seams better but takes longer. Server range 0–4096. Server default: <c>0</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>seedvrsplitlatent</c> is set.</remarks>
    [JsonProperty("seedvrtemporalvideooverlap")]
    public int? SeedVRTemporalVideoOverlap { get; set; }

    #endregion

    #region ControlNet
    /// <summary>The image to use as the input to ControlNet guidance. ("ControlNet Image Input").</summary>
    /// <remarks>This image will be preprocessed by the chosen preprocessor. If ControlNet is enabled, but this input is not, Init Image will be used instead. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetimageinput")]
    public string? ControlNetImageInput { get; set; }

    /// <summary>The preprocessor to use on the ControlNet input image. ("ControlNet Preprocessor").</summary>
    /// <remarks>If toggled off, will be automatically selected. Use 'None' to disable preprocessing. Allowed values: <c>None</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetpreprocessor")]
    public string? ControlNetPreprocessor { get; set; }

    /// <summary>For Union ControlNets, you can optionally manually specify the union controlnet type. ("ControlNet Union Type").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>openpose</c>, <c>depth</c>, <c>hed/pidi/scribble/ted</c>, <c>canny/lineart/anime_lineart/mlsd</c>, <c>normal</c>, <c>segment</c>, <c>tile</c>, <c>repaint</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetuniontype")]
    public string? ControlNetUnionType { get; set; }

    /// <summary>The ControlNet model to use. ("ControlNet Model").</summary>
    /// <remarks>Server default: <c>(None)</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetmodel")]
    public string? ControlNetModel { get; set; }

    /// <summary>Higher values make the ControlNet apply more strongly. ("ControlNet Strength").</summary>
    /// <remarks>Weaker values let the prompt overrule the ControlNet. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetstrength")]
    public float? ControlNetStrength { get; set; }

    /// <summary>When to start applying controlnet, as a fraction of steps. ("ControlNet Start").</summary>
    /// <remarks>For example, 0.5 starts applying halfway through. Must be less than End. Excluding early steps reduces the controlnet's impact on overall image structure. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetstart")]
    public float? ControlNetStart { get; set; }

    /// <summary>When to stop applying controlnet, as a fraction of steps. ("ControlNet End").</summary>
    /// <remarks>For example, 0.5 stops applying halfway through. Must be greater than Start. Excluding later steps reduces the controlnet's impact on finer details. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetend")]
    public float? ControlNetEnd { get; set; }

    #endregion

    #region ControlNet Two
    /// <summary>The image to use as the input to ControlNet guidance. ("ControlNet Two Image Input").</summary>
    /// <remarks>This image will be preprocessed by the chosen preprocessor. If ControlNet is enabled, but this input is not, Init Image will be used instead. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwoimageinput")]
    public string? ControlNetTwoImageInput { get; set; }

    /// <summary>The preprocessor to use on the ControlNet input image. ("ControlNet Two Preprocessor").</summary>
    /// <remarks>If toggled off, will be automatically selected. Use 'None' to disable preprocessing. Allowed values: <c>None</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwopreprocessor")]
    public string? ControlNetTwoPreprocessor { get; set; }

    /// <summary>For Union ControlNets, you can optionally manually specify the union controlnet type. ("ControlNet Two Union Type").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>openpose</c>, <c>depth</c>, <c>hed/pidi/scribble/ted</c>, <c>canny/lineart/anime_lineart/mlsd</c>, <c>normal</c>, <c>segment</c>, <c>tile</c>, <c>repaint</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwouniontype")]
    public string? ControlNetTwoUnionType { get; set; }

    /// <summary>The ControlNet model to use. ("ControlNet Two Model").</summary>
    /// <remarks>Server default: <c>(None)</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwomodel")]
    public string? ControlNetTwoModel { get; set; }

    /// <summary>Higher values make the ControlNet apply more strongly. ("ControlNet Two Strength").</summary>
    /// <remarks>Weaker values let the prompt overrule the ControlNet. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwostrength")]
    public float? ControlNetTwoStrength { get; set; }

    /// <summary>When to start applying controlnet, as a fraction of steps. ("ControlNet Two Start").</summary>
    /// <remarks>For example, 0.5 starts applying halfway through. Must be less than End. Excluding early steps reduces the controlnet's impact on overall image structure. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwostart")]
    public float? ControlNetTwoStart { get; set; }

    /// <summary>When to stop applying controlnet, as a fraction of steps. ("ControlNet Two End").</summary>
    /// <remarks>For example, 0.5 stops applying halfway through. Must be greater than Start. Excluding later steps reduces the controlnet's impact on finer details. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnettwoend")]
    public float? ControlNetTwoEnd { get; set; }

    #endregion

    #region ControlNet Three
    /// <summary>The image to use as the input to ControlNet guidance. ("ControlNet Three Image Input").</summary>
    /// <remarks>This image will be preprocessed by the chosen preprocessor. If ControlNet is enabled, but this input is not, Init Image will be used instead. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreeimageinput")]
    public string? ControlNetThreeImageInput { get; set; }

    /// <summary>The preprocessor to use on the ControlNet input image. ("ControlNet Three Preprocessor").</summary>
    /// <remarks>If toggled off, will be automatically selected. Use 'None' to disable preprocessing. Allowed values: <c>None</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreepreprocessor")]
    public string? ControlNetThreePreprocessor { get; set; }

    /// <summary>For Union ControlNets, you can optionally manually specify the union controlnet type. ("ControlNet Three Union Type").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>openpose</c>, <c>depth</c>, <c>hed/pidi/scribble/ted</c>, <c>canny/lineart/anime_lineart/mlsd</c>, <c>normal</c>, <c>segment</c>, <c>tile</c>, <c>repaint</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreeuniontype")]
    public string? ControlNetThreeUnionType { get; set; }

    /// <summary>The ControlNet model to use. ("ControlNet Three Model").</summary>
    /// <remarks>Server default: <c>(None)</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreemodel")]
    public string? ControlNetThreeModel { get; set; }

    /// <summary>Higher values make the ControlNet apply more strongly. ("ControlNet Three Strength").</summary>
    /// <remarks>Weaker values let the prompt overrule the ControlNet. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreestrength")]
    public float? ControlNetThreeStrength { get; set; }

    /// <summary>When to start applying controlnet, as a fraction of steps. ("ControlNet Three Start").</summary>
    /// <remarks>For example, 0.5 starts applying halfway through. Must be less than End. Excluding early steps reduces the controlnet's impact on overall image structure. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreestart")]
    public float? ControlNetThreeStart { get; set; }

    /// <summary>When to stop applying controlnet, as a fraction of steps. ("ControlNet Three End").</summary>
    /// <remarks>For example, 0.5 stops applying halfway through. Must be greater than Start. Excluding later steps reduces the controlnet's impact on finer details. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetthreeend")]
    public float? ControlNetThreeEnd { get; set; }

    #endregion

    #region Image To Video
    /// <summary>The model to use for video generation. ("Video Model").</summary>
    /// <remarks>Select an image-to-video conversion model, note that text-to-video models do not work. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videomodel")]
    public string? VideoModel { get; set; }

    /// <summary>If using a video model pair (eg Wan 2.2) for Image-To-Video, this is the second model to use. ("Video Swap Model").</summary>
    /// <remarks>Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoswapmodel")]
    public string? VideoSwapModel { get; set; }

    /// <summary>If using a video model pair (eg Wan 2.2), For Image-To-Video, this is the percentage of steps given to the Swap model. ("Video Swap Percent").</summary>
    /// <remarks>For example, at Steps=20 Swap=0.75, the base will run 5 steps then the swap model will run 15. Wan 2.2 generally uses 50% or higher. Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>video</c> feature flag. Does nothing unless <c>videoswapmodel</c> is set.</remarks>
    [JsonProperty("videoswappercent")]
    public float? VideoSwapPercent { get; set; }

    /// <summary>How many steps to use for the video model. ("Video Steps").</summary>
    /// <remarks>Higher step counts yield better quality, but much longer generation time. 20 is sufficient as a basis, but some video models need higher steps to achieve coherence. Server range 1–200. Server default: <c>20</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videosteps")]
    public int? VideoSteps { get; set; }

    /// <summary>The CFG Scale to use for video generation. ("Video CFG").</summary>
    /// <remarks>With SVD, videos start with this CFG on the first frame, and then reduce to MinCFG (normally 1) by the end frame. SVD prefers 2.5 Cosmos takes normal CFGs (around 7). LTXV prefers around 3 for its CFG. Wan prefers around 6. Server range 1–500. Server default: <c>7</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videocfg")]
    public float? VideoCfg { get; set; }

    /// <summary>What resolution/aspect the video should use. ("Video Resolution").</summary>
    /// <remarks>'Image Aspect, Model Res' uses the aspect-ratio of the image, but the pixel-count size of the model standard resolution. 'Model Preferred' means use the model's exact resolution (eg 1024x576). 'Image' means your input image resolution (ie the standard Resolution parameters control this, usually). Allowed values: <c>Image Aspect, Model Res</c>, <c>Model Preferred</c>, <c>Image</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoresolution")]
    public string? VideoResolution { get; set; }

    /// <summary>Optional advanced method to start the video diffusion late. ("Video2Video Creativity").</summary>
    /// <remarks>This is equivalent to Init Image Creativity. Set below 1 to skip some fraction of steps. This only makes sense if the base input is a video. 'Video Frame's param must have same frame length as the input video. If set to 1, video2video logic is not applied, and the input is treated as a single image. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videovideocreativity")]
    public float? Video2VideoCreativity { get; set; }

    /// <summary>An image to use as the 'end frame' of a video. ("Video End Image").</summary>
    /// <remarks>Only some models support end frames (Wan FLF2V, LTX-V), most don't. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoendimage")]
    public string? VideoEndImage { get; set; }

    #endregion

    #region Swarm Internal
    /// <summary>When enabled, the normal width parameter is used, and this value is multiplied by the width to derive the image height. ("Alt Resolution Height Multiplier").</summary>
    /// <remarks>Server range 0–10. Server default: <c>1</c>.</remarks>
    [JsonProperty("altresolutionheightmultiplier")]
    public float? AltResolutionHeightMultiplier { get; set; }

    /// <summary>Optional advanced way to manually specify raw resolutions, useful for grids. ("Raw Resolution").</summary>
    /// <remarks>When enabled, this overrides the default width/height params. Server default: <c>1024x1024</c>.</remarks>
    [JsonProperty("rawresolution")]
    public string? RawResolution { get; set; }

    /// <summary>If checked, intermediate images (eg before a refiner or segment stage) will be output separately alongside the final image. ("Output Intermediate Images").</summary>
    /// <remarks>If unchecked, only the final image will be output. Server default: <c>false</c>.</remarks>
    [JsonProperty("outputintermediateimages")]
    public bool? OutputIntermediateImages { get; set; }

    /// <summary>If checked, tells the server to just load a model and not bother with outputs. ("Just Load Model").</summary>
    /// <remarks>Server default: <c>false</c>.</remarks>
    [JsonProperty("justloadmodel")]
    public bool? JustLoadModel { get; set; }

    /// <summary>If checked, tells the server that previews are not desired. ("No Previews").</summary>
    /// <remarks>May make generations slightly faster in some cases. Server default: <c>false</c>.</remarks>
    [JsonProperty("nopreviews")]
    public bool? NoPreviews { get; set; }

    /// <summary>If checked, tells the server that if this request would cause a backend to load a model, to just skip doing that. ("No Load Models").</summary>
    /// <remarks>The backend will be marked as if the model is loaded, instantly without processing. Server default: <c>false</c>.</remarks>
    [JsonProperty("noloadmodels")]
    public bool? NoLoadModels { get; set; }

    /// <summary>If checked, tells the server that it should not do any internal special handling in this request. ("No Internal Special Handling").</summary>
    /// <remarks>A key example is in ComfyUI usage, inputs and outputs stored to comfy dirs will not be removed. Server default: <c>false</c>.</remarks>
    [JsonProperty("nointernalspecialhandling")]
    public bool? NoInternalSpecialHandling { get; set; }

    /// <summary>If an error occurs while generating images, continue (as much as possible) with further actions (such as generating more images within a queue). ("Continue After Errors").</summary>
    /// <remarks>Server default: <c>false</c>.</remarks>
    [JsonProperty("continueaftererrors")]
    public bool? ContinueAfterErrors { get; set; }

    /// <summary>What webhooks are enabled for this generation job. ("Webhooks").</summary>
    /// <remarks>Allowed values: <c>None</c>, <c>Normal</c>, <c>Manual</c>, <c>Manual At End</c>.</remarks>
    [JsonProperty("webhooks")]
    public string? Webhooks { get; set; }

    /// <summary>Which SwarmUI backend type should be used for this request. ("[Internal] Backend Type").</summary>
    /// <remarks>Allowed values: <c>Any</c>, <c>swarmswarmbackend</c>, <c>autoscalingbackend</c>, <c>localllama</c>, <c>simpleremotellm</c>, <c>comfyui_api</c>, <c>comfyui_selfstart</c>, <c>auto_webui_api</c>, <c>auto_webui_selfstart</c>, <c>hartsyinference</c>, <c>cloud_backends</c>, <c>llmassistant-anthropic</c>, <c>llmassistant-openai</c>, <c>llmassistant-hartsy-local</c>, <c>dynamic_api_backend</c>, <c>sdcpp</c>.</remarks>
    [JsonProperty("internalbackendtype")]
    public string? InternalBackendType { get; set; }

    /// <summary>Manually force a specific exact backend (by ID #) to be used for this generation. ("Exact Backend ID").</summary>
    /// <remarks>Allowed values: <c>0</c>, <c>2</c>.</remarks>
    [JsonProperty("exactbackendid")]
    public string? ExactBackendId { get; set; }

    /// <summary>Wildcard selection seed. ("Wildcard Seed").</summary>
    /// <remarks>If enabled, this seed will be used for selecting entries from wildcards. If disabled, the image seed will be used. -1 = random. Server range -1–4294967295. Server default: <c>-1</c>.</remarks>
    [JsonProperty("wildcardseed")]
    public long? WildcardSeed { get; set; }

    /// <summary>How Wildcard Seed should behave. ("Wildcard Seed Behavior").</summary>
    /// <remarks>If 'Random', seed is a random seed. If 'Index', the seed is a 0-based index into the wildcard list. (Eg if you have 5 entries, seed 0 gets the first entry, seed 4 gets the last entry, seed 5 goes back to the first entry again.) Allowed values: <c>Random</c>, <c>Index</c>.</remarks>
    [JsonProperty("wildcardseedbehavior")]
    public string? WildcardSeedBehavior { get; set; }

    /// <summary>If checked, the seed will not be incremented when Images is above 1. ("No Seed Increment").</summary>
    /// <remarks>Useful for example to test different wildcards for the same seed rapidly. Server default: <c>false</c>.</remarks>
    [JsonProperty("noseedincrement")]
    public bool? NoSeedIncrement { get; set; }

    /// <summary>Optional field to type in any personal text note you want. ("Personal Note").</summary>
    /// <remarks>This will be stored in the image metadata.</remarks>
    [JsonProperty("personalnote")]
    public string? PersonalNote { get; set; }

    /// <summary>Specifies the color depth (in bits per channel) to use. ("Color Depth").</summary>
    /// <remarks>Only works for 'PNG' image file format currently. '8-bit' is normal (8 bits per red, 8 for green, 8 for blue, making 24 bits total per pixel). and '16-bit' encodes additional high-precision (HDR-like) data. Note that overprecision data is unlikely to be meaningful, as currently available models haven't been trained for that. Allowed values: <c>8bit</c>, <c>16bit</c>.</remarks>
    [JsonProperty("colordepth")]
    public string? ColorDepth { get; set; }

    /// <summary>Override the Outpath-Format user setting. ("Override Outpath Format").</summary>
    /// <remarks>Full details in the docs here. Server default: <c>raw/[year]-[month]-[day]/[hour][minute][request_time_inc]-[prompt]-[model]</c>.</remarks>
    [JsonProperty("overrideoutpathformat")]
    public string? OverrideOutpathFormat { get; set; }

    /// <summary>If checked, enables model-specific enhancements. ("Model Specific Enhancements").</summary>
    /// <remarks>For example, on SDXL, smarter res-cond will be used. Also, some video models will automatically use tiled VAE when this is enabled, even if you didn't manually enable tiled VAE. If unchecked, will prefer more 'raw' behavior. Server default: <c>true</c>.</remarks>
    [JsonProperty("modelspecificenhancements")]
    public bool? ModelSpecificEnhancements { get; set; }

    /// <summary>If checked, tells the server to forward any raw backend data (eg comfy websocket data) to the caller. ("Forward Raw Backend Data").</summary>
    /// <remarks>This is for advanced usage (eg API calls), not normal users. Server default: <c>false</c>.</remarks>
    [JsonProperty("forwardrawbackenddata")]
    public bool? ForwardRawBackendData { get; set; }

    /// <summary>If checked, tells the server to forward Swarm internal helper data. ("Forward Swarm Data").</summary>
    /// <remarks>This is for advanced usage (eg API calls), not normal users. Server default: <c>false</c>.</remarks>
    [JsonProperty("forwardswarmdata")]
    public bool? ForwardSwarmData { get; set; }

    /// <summary>What custom workflow to use in ComfyUI (built in the Comfy Workflow Editor tab). ("ComfyUI Custom Workflow").</summary>
    /// <remarks>Generally, do not use this directly. Allowed values: <c>Examples/Basic SDXL</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("comfyuicustomworkflow")]
    public string? ComfyUICustomWorkflow { get; set; }

    #endregion

    #region LLM Prompt Processing
    /// <summary>Cache LLM responses for identical prompts. ("LLM Use Cache").</summary>
    /// <remarks>Useful for batch generation. Server default: <c>true</c>.</remarks>
    [JsonProperty("llmusecache")]
    public bool? LlmUseCache { get; set; }

    /// <summary>Generate a consistent wildcard seed per batch for reproducible results. ("LLM Generate Wildcard Seed").</summary>
    /// <remarks>Server default: <c>false</c>.</remarks>
    [JsonProperty("llmgeneratewildcardseed")]
    public bool? LlmGenerateWildcardSeed { get; set; }

    /// <summary>Which LLM model to use for prompt processing. ("LLM Model ID").</summary>
    /// <remarks>Server default: <c>default</c>.</remarks>
    [JsonProperty("llmmodelid")]
    public string? LlmModelId { get; set; }

    /// <summary>Which instruction set to use for prompt processing. ("LLM Instructions").</summary>
    /// <remarks>Allowed values: <c>chat</c>, <c>vision</c>, <c>caption</c>, <c>prompt</c>, <c>randomprompt</c>, <c>instructiongen</c>, <c>companion</c>.</remarks>
    [JsonProperty("llminstructions")]
    public string? LlmInstructions { get; set; }

    /// <summary>Which assistant's instructions (and per-model variants) to use for &lt;llmprompt&gt; processing. ("LLM Assistant ID").</summary>
    /// <remarks>Default = active assistant. Allowed values: <c>default</c>, <c>jarvis</c>, <c>assistant-mu564gsu-pmesmbp</c>.</remarks>
    [JsonProperty("llmassistantid")]
    public string? LlmAssistantId { get; set; }

    #endregion

    #region Advanced Video
    /// <summary>Whether to boomerang (aka pingpong) the video. ("Video Boomerang").</summary>
    /// <remarks>If true, the video will play and then play again in reverse to enable smooth looping. Server default: <c>false</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoboomerang")]
    public bool? VideoBoomerang { get; set; }

    /// <summary>If generating a video with a model that supports audio input, this is the audio input. ("Video Audio Input").</summary>
    /// <remarks>Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoaudioinput")]
    public string? VideoAudioInput { get; set; }

    /// <summary>How to display previews for generating videos. ("Video Preview Type").</summary>
    /// <remarks>'Animate' shows a low-res animated video preview. 'iterate' shows one frame at a time while it goes. 'one' displays just the first frame. 'none' disables previews. Allowed values: <c>animate</c>, <c>iterate</c>, <c>one</c>, <c>none</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("videopreviewtype")]
    public string? VideoPreviewType { get; set; }

    /// <summary>How many frames to interpolate between each frame in the video. ("Video Frame Interpolation Multiplier").</summary>
    /// <remarks>Higher values are smoother, but make take significant time to save the output, and may have quality artifacts. Server range 1–10. Server default: <c>1</c>. Gated behind the <c>frameinterps</c> feature flag.</remarks>
    [JsonProperty("videoframeinterpolationmultiplier")]
    public int? VideoFrameInterpolationMultiplier { get; set; }

    /// <summary>How to interpolate frames in the video. ("Video Frame Interpolation Method").</summary>
    /// <remarks>'RIFE' or 'FILM' are two different decent interpolation model options. Allowed values: <c>RIFE</c>, <c>FILM</c>, <c>GIMM-VFI</c>. Gated behind the <c>frameinterps</c> feature flag. Does nothing unless <c>videoframeinterpolationmultiplier</c> is set.</remarks>
    [JsonProperty("videoframeinterpolationmethod")]
    public string? VideoFrameInterpolationMethod { get; set; }

    #endregion

    #region Video Extend
    /// <summary>The model to use for video extending. ("Video Extend Model").</summary>
    /// <remarks>Select an image-to-video model, note that text-to-video models do not work. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoextendmodel")]
    public string? VideoExtendModel { get; set; }

    /// <summary>If using a video model pair (eg Wan 2.2) for Image-To-Video, this is the second model to use. ("Video Extend Swap Model").</summary>
    /// <remarks>Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoextendswapmodel")]
    public string? VideoExtendSwapModel { get; set; }

    /// <summary>If using a video model pair (eg Wan 2.2), For Image-To-Video, this is the percentage of steps given to the Swap model. ("Video Extend Swap Percent").</summary>
    /// <remarks>For example, at Steps=20 Swap=0.75, the base will run 5 steps then the swap model will run 15. Wan 2.2 generally uses 50% or higher. Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>video</c> feature flag. Does nothing unless <c>videoextendswapmodel</c> is set.</remarks>
    [JsonProperty("videoextendswappercent")]
    public float? VideoExtendSwapPercent { get; set; }

    /// <summary>How many frames at the end of the video should be repeated into the start of next video. ("Video Extend Frame Overlap").</summary>
    /// <remarks>This is a balancing act, more frames gets better motion clarity, but also wastes more performance on redundant calculations. Make sure this is a valid frame count for your video model, eg a multiple of 4 plus 1 for Wan (5, 9, 13, 17, ...). Should be no more than 1/3rd the frame count of your shortest extend window. For models not trained on extend behavior, '1' may be optimal. Server range 1–128. Server default: <c>9</c>.</remarks>
    [JsonProperty("videoextendframeoverlap")]
    public int? VideoExtendFrameOverlap { get; set; }

    /// <summary>What format to save extended videos in. ("Video Extend Format").</summary>
    /// <remarks>Webp video is simple and efficient, but has compatibility issues. Gif is simple and compatible, while gif-hd is higher quality via ffmpeg. h264-mp4 is a standard video file that works anywhere, but doesn't get treated like an image file. h265-mp4 is a smaller file size but may not work for all devices. prores is a specialty format. Allowed values: <c>webp</c>, <c>gif</c>, <c>gif-hd</c>, <c>webm</c>, <c>h264-mp4</c>, <c>h265-mp4</c>, <c>prores</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoextendformat")]
    public string? VideoExtendFormat { get; set; }

    #endregion

    #region Advanced Model Addons
    /// <summary>Whether to automatically select the VAE based on the main model and your user settings. ("Automatic VAE").</summary>
    /// <remarks>Only applied if a VAE is not specified. Server default: <c>false</c>.</remarks>
    [JsonProperty("automaticvae")]
    public bool? AutomaticVae { get; set; }

    /// <summary>Optionally use a PiD (Pixel Diffusion Decoder) model. ("Pixel Decoder Model").</summary>
    /// <remarks>Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("pixeldecodermodel")]
    public string? PixelDecoderModel { get; set; }

    /// <summary>Which CLIP-L model to use as a text encoder, for SD3/Flux style 'diffusion_models' folder models. ("CLIP-L Model").</summary>
    [JsonProperty("cliplmodel")]
    public string? ClipLModel { get; set; }

    /// <summary>Which CLIP-G model to use as a text encoder, for SD3 style 'diffusion_models' folder models. ("CLIP-G Model").</summary>
    [JsonProperty("clipgmodel")]
    public string? ClipGModel { get; set; }

    /// <summary>Which CLIP-Vision model to use as an image encoder, for certain image-input tasks. ("CLIP-Vision Model").</summary>
    [JsonProperty("clipvisionmodel")]
    public string? ClipVisionModel { get; set; }

    /// <summary>Which T5-XXL model to use as a text encoder, for SD3/Flux style 'diffusion_models' folder models. ("T5-XXL Model").</summary>
    /// <remarks>Also used for Wan's umt5, and Hunyuan Image's ByT5 small glyph XL.</remarks>
    [JsonProperty("txxlmodel")]
    public string? T5XxlModel { get; set; }

    /// <summary>Which LLaVA model to use as a text encoder, for Hunyuan Video 'diffusion_models' folder models. ("LLaVA Model").</summary>
    [JsonProperty("llavamodel")]
    public string? LLaVAModel { get; set; }

    /// <summary>Which LLaMA model to use as a text encoder, for HiDream-style 'diffusion_models' folder models. ("LLaMA Model").</summary>
    [JsonProperty("llamamodel")]
    public string? LLaMAModel { get; set; }

    /// <summary>Which Qwen LLM to use as a text encoder, for OmniGen/QwenImage-style 'diffusion_models' folder models. ("Qwen Model").</summary>
    [JsonProperty("qwenmodel")]
    public string? QwenModel { get; set; }

    /// <summary>Which Mistral LLM to use as a text encoder, for Flux.2-style 'diffusion_models' folder models. ("Mistral Model").</summary>
    [JsonProperty("mistralmodel")]
    public string? MistralModel { get; set; }

    /// <summary>Which Gemma LLM to use as a text encoder, for models that use Gemma (such as Lumina2, LTX2). ("Gemma Model").</summary>
    [JsonProperty("gemmamodel")]
    public string? GemmaModel { get; set; }

    /// <summary>Which GPT-OSS LLM to use as a text encoder, for Lens-style 'diffusion_models' folder models. ("GPT-OSS Model").</summary>
    [JsonProperty("gptossmodel")]
    public string? GptOssModel { get; set; }

    /// <summary>Torch.Compile is a way to dynamically accelerate AI models. ("Torch Compile").</summary>
    /// <remarks>It wastes a bit of time (around a minute) on the first call compiling a graph of the generation, and then all subsequent generations run faster thanks to the compiled graph. Torch.Compile depends on Triton, which is difficult to install on Windows, easier on Linux. Allowed values: <c>Disabled</c>, <c>inductor</c>, <c>cudagraphs</c>.</remarks>
    [JsonProperty("torchcompile")]
    public string? TorchCompile { get; set; }

    /// <summary>Override which attention implementation the model uses. ("Model Attention Backend").</summary>
    /// <remarks>'pytorch attention' is the standard default. 'comfy kitchen attention' is a new sage-like attention impl from Comfy directly that has better performance, but may not work on all machines. Allowed values: <c>pytorch attention</c>.</remarks>
    [JsonProperty("modelattentionbackend")]
    public string? ModelAttentionBackend { get; set; }

    /// <summary>Apply block-sparse attention to speed up generation with large inputs (especially video model such as H3). ("Use Sparse Attention").</summary>
    /// <remarks>Sol-Attn (adaptive tau) (TODO: Explain this) a training-free adaptive threshold (good general default). 'Top-K (SLA)' (TODO: Explain this) 'VSA' is Video Sparse Attention, (TODO: Explain this) only for VSA trained models. Allowed values: <c>None</c>, <c>sol</c>, <c>topk</c>, <c>vsa</c>.</remarks>
    [JsonProperty("usesparseattention")]
    public string? UseSparseAttention { get; set; }

    /// <summary>Override the prediction type set in the model. ("Override Prediction Type").</summary>
    /// <remarks>This is almost never a good idea to touch. Allowed values: <c>v</c>, <c>v-zsnr</c>, <c>epsilon</c>, <c>x0</c>, <c>lcm</c>, <c>sd3</c>.</remarks>
    [JsonProperty("overridepredictiontype")]
    public string? OverridePredictionType { get; set; }

    /// <summary>Override the hardware device that text encoders run on. ("Set CLIP Device").</summary>
    /// <remarks>Allowed values: <c>cpu</c>. Gated behind the <c>set_clip_device</c> feature flag.</remarks>
    [JsonProperty("setclipdevice")]
    public string? SetClipDevice { get; set; }

    /// <summary>What main checkpoint model should be used for the negative (Unconditional) portion of generation. ("Negative Model").</summary>
    [JsonProperty("negativemodel")]
    public string? NegativeModel { get; set; }

    #endregion

    #region Regional Prompting
    /// <summary>When using regionalized prompts, this factor controls how strongly the global prompt overrides the regional prompts. ("Global Region Factor").</summary>
    /// <remarks>0 means ignore global prompt, 1 means ignore regional, 0.5 means half-n-half. Server range 0–1. Server default: <c>0.5</c>.</remarks>
    [JsonProperty("globalregionfactor")]
    public float? GlobalRegionFactor { get; set; }

    /// <summary>When using an 'object' prompt, how much to cleanup the end result by. ("Regional Object Cleanup Factor").</summary>
    /// <remarks>This is the 'init image creativity' of the final cleanup step. Set to 0 to disable. Server range 0–1. Server default: <c>0</c>.</remarks>
    [JsonProperty("regionalobjectcleanupfactor")]
    public float? RegionalObjectCleanupFactor { get; set; }

    /// <summary>When using regionalized prompts with distinct 'object' values, this overrides the model used to inpaint those objects. ("Regional Object Inpainting Model").</summary>
    [JsonProperty("regionalobjectinpaintingmodel")]
    public string? RegionalObjectInpaintingModel { get; set; }

    /// <summary>If checked, when masks are recomposited (eg from a '&lt;segment:&gt;'), it will be recomposited with the exact raw mask. ("Mask Composite Unthresholded").</summary>
    /// <remarks>If false, it will boolean threshold the mask first. The boolean threshold is 'more correct' and leads to better content replacement, whereas disabling threshold (by checking this option) may lead to better looking refinements. Server default: <c>false</c>.</remarks>
    [JsonProperty("maskcompositeunthresholded")]
    public bool? MaskCompositeUnthresholded { get; set; }

    /// <summary>If checked, outputs masks from regional prompting for debug reasons. ("Debug Regional Prompting").</summary>
    /// <remarks>Server default: <c>false</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("debugregionalprompting")]
    public bool? DebugRegionalPrompting { get; set; }

    /// <summary>Optionally use a GLIGEN model. ("GLIGEN Model").</summary>
    /// <remarks>GLIGEN is only compatible with SDv1 at time of writing. Allowed values: <c>None</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("gligenmodel")]
    public string? GligenModel { get; set; }

    #endregion

    #region Segment Refining
    /// <summary>If checked, any usage of '&lt;segment:&gt;' syntax in prompts will save the generated mask in output. ("Save Segment Mask").</summary>
    /// <remarks>Server default: <c>false</c>.</remarks>
    [JsonProperty("savesegmentmask")]
    public bool? SaveSegmentMask { get; set; }

    /// <summary>Amount of blur to apply to the segment mask before using it. ("Segment Mask Blur").</summary>
    /// <remarks>This is for '&lt;segment:&gt;' syntax usage. Defaults to 10. Server range 0–64. Server default: <c>10</c>.</remarks>
    [JsonProperty("segmentmaskblur")]
    public int? SegmentMaskBlur { get; set; }

    /// <summary>Number of pixels of grow the segment mask by. ("Segment Mask Grow").</summary>
    /// <remarks>This is for '&lt;segment:&gt;' syntax usage. Defaults to 16. Server range 0–512. Server default: <c>16</c>.</remarks>
    [JsonProperty("segmentmaskgrow")]
    public int? SegmentMaskGrow { get; set; }

    /// <summary>How wide a segment mask should be oversized by. ("Segment Mask Oversize").</summary>
    /// <remarks>Larger values include more context to get more accurate inpaint, and smaller values get closer to get better details. Server range 0–512. Server default: <c>16</c>.</remarks>
    [JsonProperty("segmentmaskoversize")]
    public int? SegmentMaskOversize { get; set; }

    /// <summary>Maximum mask match value of a segment before clamping. ("Segment Threshold Max").</summary>
    /// <remarks>Lower values force more of the mask to be counted as maximum masking. Too-low values may include unwanted areas of the image. Higher values may soften the mask. Server range 0–1. Server default: <c>1</c>.</remarks>
    [JsonProperty("segmentthresholdmax")]
    public float? SegmentThresholdMax { get; set; }

    /// <summary>How to sort segments when using '&lt;segment:yolo-&gt;' syntax with indices. ("Segment Sort Order").</summary>
    /// <remarks>For example: &lt;segment:yolo-face_yolov8m-seg_60.pt-2&gt; with largest-smallest, will select the second largest face segment. Allowed values: <c>left-right</c>, <c>right-left</c>, <c>top-bottom</c>, <c>bottom-top</c>, <c>largest-smallest</c>, <c>smallest-largest</c>.</remarks>
    [JsonProperty("segmentsortorder")]
    public string? SegmentSortOrder { get; set; }

    /// <summary>When to apply the segment processing. ("Segment Apply After").</summary>
    /// <remarks>'Refiner' (default) applies segment processing after the refiner step. 'Base' applies segment processing after the base sampler but before the refiner, allowing the refiner to then blend and refine the segmented areas. Allowed values: <c>Base</c>, <c>Refiner</c>.</remarks>
    [JsonProperty("segmentapplyafter")]
    public string? SegmentApplyAfter { get; set; }

    /// <summary>Optional specific target resolution for segment. ("Segment Target Resolution").</summary>
    /// <remarks>This controls both aspect ratio, and size. This is just a target, the system may fail to exactly hit it. If the mask is on the edge of an image, the aspect may be squished. If unspecified, the aspect ratio of the detection will be used, and the resolution of the model. Server default: <c>1024x1024</c>.</remarks>
    [JsonProperty("segmenttargetresolution")]
    public string? SegmentTargetResolution { get; set; }

    #endregion

    #region Auto WebUI
    /// <summary>Sampler type (for AutoWebUI). ("[AutoWebUI] Sampler").</summary>
    /// <remarks>Allowed values: <c>Euler a</c>, <c>Euler</c>. Gated behind the <c>autowebui</c> feature flag.</remarks>
    [JsonProperty("autowebuisampler")]
    public string? AutoWebUISampler { get; set; }

    #endregion

    #region ComfyUI Advanced
    /// <summary>Parameter for internally tracking YOLOv8 models. ("YOLO Model Internal").</summary>
    /// <remarks>This is not for real usage, it is just to expose the list to the UI handler. Gated behind the <c>yolov8</c> feature flag.</remarks>
    [JsonProperty("yolomodelinternal")]
    public string? YoloModelInternal { get; set; }

    #endregion

    #region Dynamic Thresholding
    /// <summary>Mimic Scale value (target for the CFG Scale recentering). ("[DT] Mimic Scale").</summary>
    /// <remarks>Server range 0–100. Server default: <c>7</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtmimicscale")]
    public float? DtMimicScale { get; set; }

    /// <summary>thresholding percentile. ("[DT] Threshold Percentile").</summary>
    /// <remarks>'1' disables, '0.95' is decent value for enabled. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtthresholdpercentile")]
    public float? DtThresholdPercentile { get; set; }

    /// <summary>Mode for the CFG Scale scheduler. ("[DT] CFG Scale Mode").</summary>
    /// <remarks>Allowed values: <c>Constant</c>, <c>Linear Down</c>, <c>Half Cosine Down</c>, <c>Cosine Down</c>, <c>Linear Up</c>, <c>Half Cosine Up</c>, <c>Cosine Up</c>, <c>Power Up</c>, <c>Power Down</c>, <c>Linear Repeating</c>, <c>Cosine Repeating</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtcfgscalemode")]
    public string? DtCfgScaleMode { get; set; }

    /// <summary>CFG Scale minimum value (for non-constant CFG mode). ("[DT] CFG Scale Minimum").</summary>
    /// <remarks>Server range 0–100. Server default: <c>0</c>. Gated behind the <c>dynamic_thresholding</c> feature flag. Does nothing unless <c>dtcfgscalemode</c> is set.</remarks>
    [JsonProperty("dtcfgscaleminimum")]
    public float? DtCfgScaleMinimum { get; set; }

    /// <summary>Mode for the Mimic Scale scheduler. ("[DT] Mimic Scale Mode").</summary>
    /// <remarks>Allowed values: <c>Constant</c>, <c>Linear Down</c>, <c>Half Cosine Down</c>, <c>Cosine Down</c>, <c>Linear Up</c>, <c>Half Cosine Up</c>, <c>Cosine Up</c>, <c>Power Up</c>, <c>Power Down</c>, <c>Linear Repeating</c>, <c>Cosine Repeating</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtmimicscalemode")]
    public string? DtMimicScaleMode { get; set; }

    /// <summary>Mimic Scale minimum value (for non-constant mimic mode). ("[DT] Mimic Scale Minimum").</summary>
    /// <remarks>Server range 0–100. Server default: <c>0</c>. Gated behind the <c>dynamic_thresholding</c> feature flag. Does nothing unless <c>dtmimicscalemode</c> is set.</remarks>
    [JsonProperty("dtmimicscaleminimum")]
    public float? DtMimicScaleMinimum { get; set; }

    /// <summary>If either scale scheduler is 'Power', this is the power factor. ("[DT] Scheduler Value").</summary>
    /// <remarks>If using 'repeating', this is the number of repeats per image. Otherwise, it does nothing. Server range -999999–999999. Server default: <c>4</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtschedulervalue")]
    public float? DtSchedulerValue { get; set; }

    /// <summary>Whether to separate the feature channels. ("[DT] Separate Feature Channels").</summary>
    /// <remarks>Normally leave this on. I think it should be off for RCFG? Server default: <c>true</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtseparatefeaturechannels")]
    public bool? DtSeparateFeatureChannels { get; set; }

    /// <summary>Whether to scale relative to the mean value or to zero. ("[DT] Scaling Startpoint").</summary>
    /// <remarks>Use 'MEAN' normally. If you want RCFG logic, use 'ZERO'. Allowed values: <c>MEAN</c>, <c>ZERO</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtscalingstartpoint")]
    public string? DtScalingStartpoint { get; set; }

    /// <summary>Whether to use standard deviation ('STD') or thresholded absolute values ('AD'). ("[DT] Variability Measure").</summary>
    /// <remarks>Normally use 'AD'. Use 'STD' if wanting RCFG logic. Allowed values: <c>AD</c>, <c>STD</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtvariabilitymeasure")]
    public string? DtVariabilityMeasure { get; set; }

    /// <summary>'phi' interpolation factor. ("[DT] Interpolate Phi").</summary>
    /// <remarks>Interpolates between original value and DT value, such that 0.0 = use original, and 1.0 = use DT. (This exists because RCFG is bad and so half-removing it un-breaks it - better to just not do RCFG). Server range 0–1. Server default: <c>1</c>. Gated behind the <c>dynamic_thresholding</c> feature flag.</remarks>
    [JsonProperty("dtinterpolatephi")]
    public float? DtInterpolatePhi { get; set; }

    #endregion

    #region FreeU
    /// <summary>Which models to apply FreeU to, as base, refiner, or both. ("[FreeU] Apply To").</summary>
    /// <remarks>Irrelevant when not using refiner. Allowed values: <c>Both</c>, <c>Base</c>, <c>Refiner</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeuapplyto")]
    public string? FreeUApplyTo { get; set; }

    /// <summary>Which version of FreeU to use. ("[FreeU] Version").</summary>
    /// <remarks>1 is the version in the original paper, 2 is a variation of it developed by the same original author of FreeU. Allowed values: <c>1</c>, <c>2</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeuversion")]
    public string? FreeUVersion { get; set; }

    /// <summary>Block1 multiplier value for FreeU. ("[FreeU] Block One").</summary>
    /// <remarks>Paper recommends 1.1. Server range 0–10. Server default: <c>1.1</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeublockone")]
    public float? FreeUBlockOne { get; set; }

    /// <summary>Block2 multiplier value for FreeU. ("[FreeU] Block Two").</summary>
    /// <remarks>Paper recommends 1.2. Server range 0–10. Server default: <c>1.2</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeublocktwo")]
    public float? FreeUBlockTwo { get; set; }

    /// <summary>Skip1 multiplier value for FreeU. ("[FreeU] Skip One").</summary>
    /// <remarks>Paper recommends 0.9. Server range 0–10. Server default: <c>0.9</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeuskipone")]
    public float? FreeUSkipOne { get; set; }

    /// <summary>Skip2 multiplier value for FreeU. ("[FreeU] Skip Two").</summary>
    /// <remarks>Paper recommends 0.2. Server range 0–10. Server default: <c>0.2</c>. Gated behind the <c>freeu</c> feature flag.</remarks>
    [JsonProperty("freeuskiptwo")]
    public float? FreeUSkipTwo { get; set; }

    #endregion

    #region Advanced Sampling
    /// <summary>CFG Scale for Cond2-Negative in InstructPix2Pix (Edit) models. ("IP2P CFG 2").</summary>
    /// <remarks>Server range 1–100. Server default: <c>1.5</c>.</remarks>
    [JsonProperty("ippcfg")]
    public float? IP2PCfg2 { get; set; }

    /// <summary>If VAE Tile Size is enabled, this controls how much overlap between tiles there should be. ("VAE Tile Overlap").</summary>
    /// <remarks>Higher overlap improves quality but takes longer. Server range 0–4096. Server default: <c>64</c>. Does nothing unless <c>vaetilesize</c> is set.</remarks>
    [JsonProperty("vaetileoverlap")]
    public int? VaeTileOverlap { get; set; }

    /// <summary>If VAE Tile Size is enabled, decodes videos through the VAE using video frame tiles of this size. ("VAE Temporal Tile Size").</summary>
    /// <remarks>VAE Tiling reduces VRAM consumption, but takes longer and may impact quality. Server range 8–4096. Server default: <c>64</c>. Does nothing unless <c>vaetilesize</c> is set.</remarks>
    [JsonProperty("vaetemporaltilesize")]
    public int? VaeTemporalTileSize { get; set; }

    /// <summary>If VAE Tile Size is enabled, this controls how much overlap between video frames there should be. ("VAE Temporal Tile Overlap").</summary>
    /// <remarks>Higher overlap improves quality but takes longer. Server range 4–4096. Server default: <c>8</c>. Does nothing unless <c>vaetilesize</c> is set.</remarks>
    [JsonProperty("vaetemporaltileoverlap")]
    public int? VaeTemporalTileOverlap { get; set; }

    /// <summary>How to process the mask, for masked-generation such as Init Image with a Mask Image, or Segment blocks. ("Mask Behavior").</summary>
    /// <remarks>'Differential' = 'Differential Diffusion' technique, wherein the mask values are used as offsets for timestep of when to apply the mask or not. 'Simple Latent' = the most basic latent masking technique. Allowed values: <c>Differential</c>, <c>Simple Latent</c>.</remarks>
    [JsonProperty("maskbehavior")]
    public string? MaskBehavior { get; set; }

    /// <summary>Experimental: How to correct color when compositing a masked image. ("Color Correction Behavior").</summary>
    /// <remarks>'None' = Do not attempt color correction. 'Uniform' = Compute a fixed offset HSV correction for all pixels. 'Linear' = Compute a linear correction that depends on each pixel's S and V. 'Linear2' = experimental improvement to regular Linear that seems better. This is useful for example when doing inpainting with Flux models, as the Flux VAE does not retain consistent colors - 'Linear2' may help correct for this… Allowed values: <c>None</c>, <c>Uniform</c>, <c>Linear</c>, <c>Linear2</c>.</remarks>
    [JsonProperty("colorcorrectionbehavior")]
    public string? ColorCorrectionBehavior { get; set; }

    /// <summary>If enabled, removes the background from the generated image. ("Remove Background").</summary>
    /// <remarks>This internally uses RemBG. Server default: <c>false</c>.</remarks>
    [JsonProperty("removebackground")]
    public bool? RemoveBackground { get; set; }

    /// <summary>Multiplies the positive prompt's text conditioning by this value. ("Conditioning Multiplier").</summary>
    /// <remarks>Server range -100–100. Server default: <c>1</c>.</remarks>
    [JsonProperty("conditioningmultiplier")]
    public float? ConditioningMultiplier { get; set; }

    /// <summary>Multiplies the negative prompt's text conditioning by this value. ("Negative Conditioning Multiplier").</summary>
    /// <remarks>Server range -100–100. Server default: <c>1</c>.</remarks>
    [JsonProperty("negativeconditioningmultiplier")]
    public float? NegativeConditioningMultiplier { get; set; }

    /// <summary>Preferred data type for models, when a choice is available. ("Preferred DType").</summary>
    /// <remarks>(Notably primarily affects Flux.1 models currently). If disabled, will automatically decide. 'fp8_e43fn' is recommended for large models. 'Default' uses global default type, usually fp16 or bf16. Allowed values: <c>automatic</c>, <c>default</c>, <c>fp8_e4m3fn</c>, <c>fp8_e5m2</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("preferreddtype")]
    public string? PreferredDType { get; set; }

    /// <summary>Percentage of steps to cut off before the image is done generation. ("End Steps Early").</summary>
    /// <remarks>Server range 0–1. Server default: <c>0</c>. Gated behind the <c>endstepsearly</c> feature flag.</remarks>
    [JsonProperty("endstepsearly")]
    public float? EndStepsEarly { get; set; }

    /// <summary>If checked, shifts the empty latent to use a mean-average per-channel latent value (as calculated by Birchlabs). ("Shifted Latent Average Init").</summary>
    /// <remarks>If unchecked, default behavior of zero-init latents are used. This can potentially improve the color range or even general quality on SDv1, SDv2, and SDXL models. Note that the effect is very minor. Server default: <c>false</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("shiftedlatentaverageinit")]
    public bool? ShiftedLatentAverageInit { get; set; }

    /// <summary>When to use EasyCache. ("EasyCache Mode").</summary>
    /// <remarks>EasyCache is a trick to accelerate diffusion models, especially video models. That is: generation runs faster, but loses some quality. You can leave this disabled, enabled for all model sampling stages, or only enabled for certain model sampling stages. (This separation is so eg you can accelerate your video generation, without losing quality of an initial image). Allowed values: <c>disabled</c>, <c>all</c>, <c>base gen only</c>, <c>video only</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("easycachemode")]
    public string? EasyCacheMode { get; set; }

    /// <summary>What threshold to use with EasyCache. ("EasyCache Threshold").</summary>
    /// <remarks>Set to 0 to disable. Higher values skip more steps. Server range 0–1. Server default: <c>0.2</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>easycachemode</c> is set.</remarks>
    [JsonProperty("easycachethreshold")]
    public float? EasyCacheThreshold { get; set; }

    /// <summary>When to start applying EasyCache, as a fraction of steps (if enabled). ("EasyCache Start").</summary>
    /// <remarks>0 or 0.15 is the recommended default for most models. Server range 0–1. Server default: <c>0.15</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>easycachemode</c> is set.</remarks>
    [JsonProperty("easycachestart")]
    public float? EasyCacheStart { get; set; }

    /// <summary>When to stop applying EasyCache, as a fraction of steps (if enabled). ("EasyCache End").</summary>
    /// <remarks>1 or 0.95 is the recommended default for most models. Server range 0–1. Server default: <c>0.95</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>easycachemode</c> is set.</remarks>
    [JsonProperty("easycacheend")]
    public float? EasyCacheEnd { get; set; }

    /// <summary>When to use TeaCache. ("TeaCache Mode").</summary>
    /// <remarks>TeaCache is a trick to accelerate diffusion models, especially video models. That is: generation runs faster, but loses some quality. See here for more info. You can leave this disabled, enabled for all model sampling stages, or only enabled for certain model sampling stages. (This separation is so eg you can accelerate your video generation, without losing quality of an initial image). Allowed values: <c>disabled</c>, <c>all</c>, <c>base gen only</c>, <c>video only</c>. Gated behind the <c>teacache</c> feature flag.</remarks>
    [JsonProperty("teacachemode")]
    public string? TeaCacheMode { get; set; }

    /// <summary>What threshold to use with TeaCache. ("TeaCache Threshold").</summary>
    /// <remarks>See 'TeaCache Mode' parameter above. 0.4 might work well with Flux image generation, and 0.15 might work well with video generation. 0.25 is a good stable default for most purposes - decent acceleration but little visual change. Server range 0–1. Server default: <c>0.25</c>. Gated behind the <c>teacache</c> feature flag. Does nothing unless <c>teacachemode</c> is set.</remarks>
    [JsonProperty("teacachethreshold")]
    public float? TeaCacheThreshold { get; set; }

    /// <summary>When to start applying TeaCache, as a fraction of steps (if enabled). ("TeaCache Start").</summary>
    /// <remarks>0 is the recommended default for most models. Comfy-TeaCache node pack author recommends a slightly higher setting for some models (eg 0.1 for some Wan variants and HiDream Full, never above 0.2). Server range 0–1. Server default: <c>0</c>. Gated behind the <c>teacache</c> feature flag. Does nothing unless <c>teacachemode</c> is set.</remarks>
    [JsonProperty("teacachestart")]
    public float? TeaCacheStart { get; set; }

    /// <summary>What threshold to use with Nunchaku block caching. ("Nunchaku Cache Threshold").</summary>
    /// <remarks>This makes Nunchaku gens faster at the cost of quality. Only applicable to Nunchaku models. Generally 0 to 0.2 is the reasonable range, above that you can start noticing quality drop. Server range 0–1. Server default: <c>0</c>. Gated behind the <c>nunchaku</c> feature flag.</remarks>
    [JsonProperty("nunchakucachethreshold")]
    public float? NunchakuCacheThreshold { get; set; }

    #endregion

    #region Alternate Guidance
    /// <summary>If checked, enables AITemplate for ComfyUI generations (UNet only). ("Enable AITemplate").</summary>
    /// <remarks>Only compatible with some GPUs. Server default: <c>false</c>. Gated behind the <c>aitemplate</c> feature flag.</remarks>
    [JsonProperty("enableaitemplate")]
    public bool? EnableAITemplate { get; set; }

    /// <summary>Scale for Self-Attention Guidance. ("Self-Attention Guidance Scale").</summary>
    /// <remarks>Self-Attention Guidance (SAG) uses the intermediate self-attention maps of diffusion models to enhance their stability and efficacy. Specifically, SAG adversarially blurs only the regions that diffusion models attend to at each iteration and guides them accordingly. Defaults to 0.5. This is only expected to work on older unet-based models (eg SDXL) and not on newer models. Server range -2–5. Server default: <c>0.5</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("selfattentionguidancescale")]
    public float? SelfAttentionGuidanceScale { get; set; }

    /// <summary>Blur-sigma for Self-Attention Guidance. ("Self-Attention Guidance Sigma Blur").</summary>
    /// <remarks>Defaults to 2.0. Server range 0–10. Server default: <c>2</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>selfattentionguidancescale</c> is set.</remarks>
    [JsonProperty("selfattentionguidancesigmablur")]
    public float? SelfAttentionGuidanceSigmaBlur { get; set; }

    /// <summary>Scale for Perturbed-Attention Guidance (PAG). ("Perturbed-Attention Guidance Scale").</summary>
    /// <remarks>PAG is designed to progressively enhance the structure of synthesized samples throughout the denoising process by considering the self-attention mechanisms' ability to capture structural information. It involves generating intermediate samples with degraded structure by substituting selected self-attention maps in diffusion U-Net with an identity matrix, and guiding the denoising process away from these degraded… Server range 0–100. Server default: <c>3</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("perturbedattentionguidancescale")]
    public float? PerturbedAttentionGuidanceScale { get; set; }

    /// <summary>If enabled, use Comfy's native version of RescaleCFG. ("Rescale CFG Multiplier").</summary>
    /// <remarks>This is only expected to work on certain vpred models. This is, generally, pointless. The value specified is the multiplier rate. Server range 0–1. Server default: <c>0.7</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("rescalecfgmultiplier")]
    public float? RescaleCfgMultiplier { get; set; }

    /// <summary>If enabled, use 'Renorm CFG', a technique developed for use with Lumina 2. ("Renorm CFG").</summary>
    /// <remarks>At 0, this does nothing. Lumina 2 reference code sets this to 1. This parameter only works on some models, and will corrupt others. Server range 0–100. Server default: <c>0</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("renormcfg")]
    public float? RenormCfg { get; set; }

    /// <summary>If enabled, use 'CFG Zero Star' (CFG-Zero*, defined in this paper). ("Use CFG Zero Star").</summary>
    /// <remarks>This may slightly improve quality on modern 'Flow' models when using CFG. Server default: <c>false</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("usecfgzerostar")]
    public bool? UseCfgZeroStar { get; set; }

    /// <summary>If enabled, use 'TCFG' (Tangential Damping Classifier-Free Guidance, defined in this paper). ("Use TCFG").</summary>
    /// <remarks>This may reduce CFG artifacts. Compatible with modern 'Flow' models. Server default: <c>false</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("usetcfg")]
    public bool? UseTcfg { get; set; }

    /// <summary>Scale for Normalized Attention Guidance, defined in this paper). ("Normalized Attention Guidance Scale").</summary>
    /// <remarks>Designed to when CFG Scale is set to 1 (CFG disabled), and gives back some negative prompting support. 5 is a reasonable starter value for using this. Defaults to 0 (disabled). Server range 0–50. Server default: <c>0</c>. Gated behind the <c>comfyui</c> feature flag.</remarks>
    [JsonProperty("normalizedattentionguidancescale")]
    public float? NormalizedAttentionGuidanceScale { get; set; }

    /// <summary>Alpha value for Normalized Attention Guidance, aka blending scale. ("Normalized Attention Guidance Alpha").</summary>
    /// <remarks>In other words, how strongly to mix NAG with the base generation. 1 means fully NAG, 0 means fully base, 0.5 means half-n-half. 0.5 is a safe default. Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>normalizedattentionguidancescale</c> is set.</remarks>
    [JsonProperty("normalizedattentionguidancealpha")]
    public float? NormalizedAttentionGuidanceAlpha { get; set; }

    /// <summary>Tau value for Normalized Attention Guidance. ("Normalized Attention Guidance Tau").</summary>
    /// <remarks>This is a more internal value which modifies the guidance scaling. Server range 0.5–10. Server default: <c>1.5</c>. Gated behind the <c>comfyui</c> feature flag. Does nothing unless <c>normalizedattentionguidancescale</c> is set.</remarks>
    [JsonProperty("normalizedattentionguidancetau")]
    public float? NormalizedAttentionGuidanceTau { get; set; }

    #endregion

    #region Segment Param Overrides
    /// <summary>Optionally specify a distinct model to use for 'segment' values. ("Segment Model").</summary>
    [JsonProperty("segmentmodel")]
    public string? SegmentModel { get; set; }

    /// <summary>Alternate Steps value for when calculating the segment stage. ("Segment Steps").</summary>
    /// <remarks>This replaces the 'Steps' total count before calculating the Segment Creativity. Server range 1–200. Server default: <c>40</c>.</remarks>
    [JsonProperty("segmentsteps")]
    public int? SegmentSteps { get; set; }

    /// <summary>For the segment model independently of the base model, how strongly to scale prompt input. ("Segment CFG Scale").</summary>
    /// <remarks>Higher CFG scales tend to produce more contrast, and lower CFG scales produce less contrast. Too-high values can cause corrupted/burnt images, too-low can cause nonsensical images. 7 is a good baseline. Normal usages vary between 4 and 9. Some model types, such as Turbo, expect CFG around 1. Server range 0–100. Server default: <c>7</c>.</remarks>
    [JsonProperty("segmentcfgscale")]
    public float? SegmentCfgScale { get; set; }

    #endregion

    #region Video Obscure Options
    /// <summary>Trim this many frames from the start of a video output. ("Trim Video Start Frames").</summary>
    /// <remarks>This will shorten a video, and is just a fix for video models that corrupt start frames (such as early Wan versions). Server range 0–1000. Server default: <c>0</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("trimvideostartframes")]
    public int? TrimVideoStartFrames { get; set; }

    /// <summary>Trim this many frames from the end of a video output. ("Trim Video End Frames").</summary>
    /// <remarks>This will shorten a video, and is just a fix for video models that corrupt end frames (such as early Wan versions). Server range 0–1000. Server default: <c>0</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("trimvideoendframes")]
    public int? TrimVideoEndFrames { get; set; }

    /// <summary>The minimum CFG to use for video generation. ("Video Min CFG").</summary>
    /// <remarks>Videos start with max CFG on first frame, and then reduce to this CFG. Set to -1 to disable. Only used for SVD. Server range -1–100. Server default: <c>1.0</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videomincfg")]
    public float? VideoMinCfg { get; set; }

    /// <summary>Which trained 'motion bucket' to use for the video model. ("Video Motion Bucket").</summary>
    /// <remarks>Higher values induce more motion. Most values should stay in the 100-200 range. 127 is a good baseline, as it is the most common value in SVD's training set. Only used for SVD. Server range 1–1023. Server default: <c>127</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videomotionbucket")]
    public int? VideoMotionBucket { get; set; }

    /// <summary>How much noise to add to the init image for Image2Video. ("Video Augmentation Level").</summary>
    /// <remarks>Higher values yield more motion. For SVD, default is 0. For LTX, default is 0.15. Other models do not use this. Server range 0–10. Server default: <c>0.0</c>. Gated behind the <c>video</c> feature flag.</remarks>
    [JsonProperty("videoaugmentationlevel")]
    public float? VideoAugmentationLevel { get; set; }

    #endregion

    #region Other Fixes
    /// <summary>Seconds of silent audio to mask off at the start of the generation. ("Audio Silent Prefix Duration").</summary>
    /// <remarks>Useful for video models (such as MiniMax H3) which generate audio that tends to start with stray noise. By injecting a small amount of masked silence, the model understands to not add other noise at the start. Can also be used with a large duration value to force full silence throughout a video. Server range 0–9999. Server default: <c>0.1</c>.</remarks>
    [JsonProperty("audiosilentprefixduration")]
    public float? AudioSilentPrefixDuration { get; set; }

    /// <summary>Seconds of silent audio to mask off at the end of the generation. ("Audio Silent Suffix Duration").</summary>
    /// <remarks>Useful for video models (such as MiniMax H3) which generate audio that tends to end with stray noise. By injecting a small amount of masked silence, the model understands to not add other noise at the end. Server range 0–9999. Server default: <c>0</c>.</remarks>
    [JsonProperty("audiosilentsuffixduration")]
    public float? AudioSilentSuffixDuration { get; set; }

    #endregion

    #region Grid Generation, SAM2 selection & ControlNet preview
    /// <summary>Images to include with the prompt, for eg ReVision or UnCLIP. ("Prompt Images").</summary>
    /// <remarks>SwarmUI's own UI sets this from the prompt box's image-attachment widget; API callers set it directly to pass reference images for ReVision, UnCLIP, IP-Adapter-style or Flux-Kontext-style edit models.</remarks>
    [JsonProperty("promptimages")]
    public List<string>? PromptImages { get; set; }

    /// <summary>Audio files to include with the prompt. ("Prompt Audios").</summary>
    /// <remarks>SwarmUI's own UI sets this from the prompt box's audio-attachment widget; API callers set it directly to pass reference audio to models that accept it.</remarks>
    [JsonProperty("promptaudios")]
    public List<string>? PromptAudios { get; set; }

    /// <summary>Videos to include with the prompt. ("Prompt Videos").</summary>
    /// <remarks>SwarmUI's own UI sets this from the prompt box's video-attachment widget; API callers set it directly to pass reference video to models that accept it.</remarks>
    [JsonProperty("promptvideos")]
    public List<string>? PromptVideos { get; set; }

    /// <summary>Replace text in the prompt (or negative prompt) with some other text. ("[Grid Gen] Prompt Replace").</summary>
    [JsonProperty("gridgenpromptreplace")]
    public string? GridGenPromptReplace { get; set; }

    /// <summary>Add text to the end of the prompt in a stackable way. ("[Grid Gen] Prompt Add").</summary>
    /// <remarks>Only compatible with the base 'prompt' parameter.</remarks>
    [JsonProperty("gridgenpromptadd")]
    public string? GridGenPromptAdd { get; set; }

    /// <summary>Apply parameter presets to the image. ("[Grid Gen] Presets").</summary>
    /// <remarks>Can use a comma-separated list to apply multiple per-cell, eg 'a, b || a, c || b, c'</remarks>
    [JsonProperty("gridgenpresets")]
    public string? GridGenPresets { get; set; }

    /// <summary>(For API usage) If enabled, requests preview output from ControlNet and no image generation at all. ("ControlNet Preview Only").</summary>
    /// <remarks>Server default: <c>false</c>. Gated behind the <c>controlnet</c> feature flag.</remarks>
    [JsonProperty("controlnetpreviewonly")]
    public bool? ControlNetPreviewOnly { get; set; }

    /// <summary>Internal: JSON list of positive point coordinates for SAM2 point masking. ("SAM2 Positive Points").</summary>
    /// <remarks>Server default: <c>[]</c>. Gated behind the <c>sam2</c> feature flag.</remarks>
    [JsonProperty("sampositivepoints")]
    public string? SAM2PositivePoints { get; set; }

    /// <summary>Internal: JSON list of negative point coordinates for SAM2 point masking. ("SAM2 Negative Points").</summary>
    /// <remarks>Server default: <c>[]</c>. Gated behind the <c>sam2</c> feature flag.</remarks>
    [JsonProperty("samnegativepoints")]
    public string? SAM2NegativePoints { get; set; }

    /// <summary>Internal: JSON bounding box [x1,y1,x2,y2] for SAM2 bbox masking. ("SAM2 BBox").</summary>
    /// <remarks>Gated behind the <c>sam2</c> feature flag.</remarks>
    [JsonProperty("sambbox")]
    public string? SAM2BBox { get; set; }

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
