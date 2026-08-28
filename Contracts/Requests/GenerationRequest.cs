using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>Request parameters for text-to-image generation via SwarmUI.</summary>
/// <remarks>Every property carries its exact SwarmUI wire name via <see cref="JsonPropertyAttribute"/> — the payload is serialized from these attributes, so a property without one does not reach the server. SwarmUI silently drops unrecognized parameter names (it normalizes incoming keys to lowercase letters before matching), which is why each name here was verified against the server's registered parameter list. Extension-region parameters require the corresponding server extension to be installed.</remarks>
public class GenerationRequest
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

    /// <summary>Sampling algorithm used to denoise the image. Null omits it from the payload,
    /// which is required by models that carry their own solver — several video models refuse a
    /// request that names a sampler at all.</summary>
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
    [JsonProperty("initimagecreativity")]
    public float InitImageCreativity { get; set; } = 0.7f;

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

    #region Video
    // Text-to-video, where the selected model IS a video model (Wan, LTX-V, Hunyuan Video, ...).
    // The separate `videoframes`/`videomodel` family drives Swarm's image-to-video stage, which
    // appends a secondary video model after an image generation — a different pipeline.

    /// <summary>Frame count for text-to-video ("Text-To-Video Frames"). Server range 1–1000.</summary>
    /// <remarks>Duration in seconds is this divided by <see cref="VideoFps"/>.</remarks>
    [JsonProperty("textvideoframes")]
    public int? VideoFrames { get; set; }

    /// <summary>Output frame rate ("Video FPS"). Server range 1–1024, default 24.</summary>
    [JsonProperty("videofps")]
    public int? VideoFps { get; set; }

    /// <summary>Container/codec for the returned video ("Video Format"): webp, gif, gif-hd, webm,
    /// h264-mp4, h265-mp4, prores.</summary>
    [JsonProperty("videoformat")]
    public string? VideoFormat { get; set; }
    #endregion

    #region AudioLab extension — shared
    // These parameters require the AudioLab server extension. Which ones a given model honours is
    // decided by the feature flags SwarmUI registers per audio architecture (music, TTS, STT, SFX).

    /// <summary>Container for the returned audio ("Audio Output Format"): wav_16, wav_32, flac, mp3, ogg.</summary>
    [JsonProperty("audiooutputformat")]
    public string? AudioOutputFormat { get; set; }

    /// <summary>Encoding quality for the returned audio ("Audio Quality"): low, medium, high, max.</summary>
    [JsonProperty("audioquality")]
    public string? AudioQuality { get; set; }
    #endregion

    #region AudioLab extension — music generation
    /// <summary>Song lyrics for music models that sing ("Lyrics"). "[Instrumental]" for no vocals.</summary>
    [JsonProperty("lyrics")]
    public string? Lyrics { get; set; }

    /// <summary>Denoising steps for audio models ("Infer Steps"); 0 uses the model's own default. Server range 0–200.</summary>
    [JsonProperty("infersteps")]
    public int? AudioInferSteps { get; set; }

    /// <summary>Guidance scale for ACE-Step music models ("ACE Guidance"). Server range 1–15.</summary>
    [JsonProperty("aceguidance")]
    public float? AceGuidance { get; set; }

    /// <summary>Whether to generate without vocals ("Instrumental"). Registered as a string dropdown: "true" / "false".</summary>
    [JsonProperty("instrumental")]
    public string? Instrumental { get; set; }

    /// <summary>Free-text musical style/genre tags ("Music Style").</summary>
    [JsonProperty("musicstyle")]
    public string? MusicStyle { get; set; }

    /// <summary>Tempo in beats per minute ("BPM"); 0 lets the model choose. Server range 0–300.</summary>
    [JsonProperty("bpm")]
    public int? Bpm { get; set; }

    /// <summary>Musical key and mode ("Key Scale"), e.g. "C major". Empty lets the model choose.</summary>
    [JsonProperty("keyscale")]
    public string? KeyScale { get; set; }

    /// <summary>Beats per bar ("Time Signature"): 2, 3, 4, 6.</summary>
    [JsonProperty("timesignature")]
    public string? TimeSignature { get; set; }

    /// <summary>Language sung in the vocals ("Vocal Language"), e.g. "en", "ja". "unknown" lets the model choose.</summary>
    [JsonProperty("vocallanguage")]
    public string? VocalLanguage { get; set; }

    /// <summary>Denoising steps for Stable Audio models ("Stable Audio Steps"). Server range 1–100.</summary>
    [JsonProperty("stableaudiosteps")]
    public int? StableAudioSteps { get; set; }
    #endregion

    #region AudioLab extension — AudioCraft sampling (MusicGen, AudioGen)
    /// <summary>Maximum output length in seconds ("Max Duration"). Server range 1–300.</summary>
    [JsonProperty("maxduration")]
    public float? MaxDuration { get; set; }

    /// <summary>Prompt adherence for AudioCraft models ("Guidance Scale"). Server range 0–10.</summary>
    [JsonProperty("guidancescale")]
    public float? AudioGuidanceScale { get; set; }

    /// <summary>Sampling temperature for AudioCraft models ("Temperature"). Server range 0–2.</summary>
    [JsonProperty("audiocrafttemperature")]
    public float? AudioCraftTemperature { get; set; }

    /// <summary>Top-K sampling cutoff for AudioCraft models ("Top K"). Server range 0–1000.</summary>
    [JsonProperty("audiocrafttopk")]
    public int? AudioCraftTopK { get; set; }

    /// <summary>Top-P (nucleus) sampling cutoff for AudioCraft models ("Top P"). Server range 0–1.</summary>
    [JsonProperty("audiocrafttopp")]
    public float? AudioCraftTopP { get; set; }
    #endregion

    #region AudioLab extension — sound effects
    /// <summary>Requested effect length in seconds ("SFX Duration"); 0 lets the model choose. Server range 0–30.</summary>
    [JsonProperty("sfxduration")]
    public float? SfxDuration { get; set; }

    /// <summary>How literally the effect follows the prompt ("SFX Prompt Influence"). Server range 0–1.</summary>
    [JsonProperty("sfxpromptinfluence")]
    public float? SfxPromptInfluence { get; set; }
    #endregion

    #region AudioLab extension — speech (TTS, STT, voice conversion)
    /// <summary>Reference clip whose voice a TTS model should imitate ("Reference Audio"). Data URL or server path.</summary>
    [JsonProperty("referenceaudio")]
    public string? ReferenceAudio { get; set; }

    /// <summary>Transcript of <see cref="ReferenceAudio"/> ("Reference Text"), required by some cloning models.</summary>
    [JsonProperty("referencetext")]
    public string? ReferenceText { get; set; }

    /// <summary>Audio to transcribe ("Audio Input") for speech-to-text models. Data URL or server path.</summary>
    [JsonProperty("audioinput")]
    public string? AudioInput { get; set; }

    /// <summary>Spoken language hint for speech-to-text ("Language"); empty auto-detects.</summary>
    [JsonProperty("language")]
    public string? SpeechLanguage { get; set; }

    /// <summary>Whisper mode ("Whisper Task"): transcribe or translate.</summary>
    [JsonProperty("whispertask")]
    public string? WhisperTask { get; set; }

    /// <summary>Audio whose voice is being converted ("Source Audio"). Data URL or server path.</summary>
    [JsonProperty("sourceaudio")]
    public string? SourceAudio { get; set; }

    /// <summary>Target voice for voice conversion ("Target Voice").</summary>
    [JsonProperty("targetvoice")]
    public string? TargetVoice { get; set; }
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
