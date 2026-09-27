using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>Generation parameters registered by the SwarmUI-API-Backends server extension, covering every remote
/// provider it fronts: Black Forest Labs, OpenAI, Ideogram, Google, Grok directly, plus a couple dozen more
/// through its fal.ai integration.</summary>
/// <remarks>
/// <para>These are extension parameters, not stock SwarmUI ones — they only apply when a request targets a model
/// served by one of these API backends, and only reach the server at all when SwarmUI-API-Backends is installed.
/// The fal.ai integration alone fronts Kling, Luma, Veo, Pika, Sora, Wan, Seedance, Hunyuan, Vidu, PixVerse,
/// Kandinsky, Recraft, Nano Banana 2, and more behind one API key. Each provider only reads its own parameters —
/// sending Kling's duration to a Luma generation does nothing, since SwarmUI only forwards the parameters the
/// selected model's feature flags advertise.</para>
/// <para>Only send parameters advertised by the selected model's feature flags.</para>
/// </remarks>
public partial class GenerationRequest
{
    #region Black Forest Labs (Flux via API)
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

    /// <summary>Optional image to use as a starting point or reference. ("Image Prompt").</summary>
    /// <remarks>Acts as a visual guide for the generation process. Useful for variations, style matching, or guided compositions. Supported by: Flux Dev, Flux Pro 1.1, Flux Ultra. Gated behind the <c>bfl_image_prompt</c> feature flag.</remarks>
    [JsonProperty("imageprompt")]
    public string? ImagePrompt { get; set; }

    #endregion

    #region DALL-E 2
    /// <summary>Dimensions of the generated image. ("DALL-E Two Output Resolution").</summary>
    /// <remarks>DALL-E 2 only supports these three square sizes. Allowed values: <c>256x256</c>, <c>512x512</c>, <c>1024x1024</c>. Gated behind the <c>dalle2_params</c> feature flag.</remarks>
    [JsonProperty("dalletwooutputresolution")]
    public string? DallETwoOutputResolution { get; set; }

    #endregion

    #region DALL-E 3
    /// <summary>Controls the level of detail and consistency in DALL-E 3 images. ("Generation Quality").</summary>
    /// <remarks>'Standard' - Balanced quality suitable for most uses, generates faster 'HD' - Enhanced detail and better consistency across the entire image, takes longer to generate Note: HD mode costs more credits but can be worth it for complex scenes or when fine details matter. Allowed values: <c>standard</c>, <c>hd</c>. Gated behind the <c>dalle3_params</c> feature flag.</remarks>
    [JsonProperty("generationquality")]
    public string? GenerationQuality { get; set; }

    /// <summary>Determines how the generated image is returned from the API. ("Response Format").</summary>
    /// <remarks>'URL' - Returns a temporary URL valid for 60 minutes 'Base64' - Returns the image data directly encoded in base64 Base64 is preferred for immediate use, URLs for deferred processing. Allowed values: <c>url</c>, <c>b64_json</c>. Gated behind the <c>dalle3_params</c> feature flag.</remarks>
    [JsonProperty("responseformat")]
    public string? ResponseFormat { get; set; }

    #endregion

    #region FLUX 1.1 Ultra
    /// <summary>Enables raw generation mode in Flux Pro 1.1. ("Raw Mode").</summary>
    /// <remarks>When enabled: Generates less processed, more natural-looking images Raw mode can produce more authentic results but may be less polished Only available in Flux Pro 1.1 ultra mode. Server default: <c>false</c>. Gated behind the <c>flux_ultra_params</c> feature flag.</remarks>
    [JsonProperty("rawmode")]
    public bool? RawMode { get; set; }

    /// <summary>Controls how much the Image Prompt influences the generation. ("Image Prompt Strength").</summary>
    /// <remarks>0.0: Ignore image prompt completely 1.0: Follow image prompt very closely Default 0.1 provides subtle guidance while allowing creativity. Server range 0–1. Server default: <c>0.1</c>. Gated behind the <c>flux_ultra_params</c> feature flag.</remarks>
    [JsonProperty("imagepromptstrength")]
    public float? ImagePromptStrength { get; set; }

    #endregion

    #region FLUX 3 Video (fal.ai direct BFL)
    /// <summary>Resolution for FLUX 3 on Black Forest Labs' own API, which names these hd and fhd. ("FLUX Three Direct Video Resolution").</summary>
    /// <remarks>Allowed values: <c>hd</c>, <c>fhd</c>. Gated behind the <c>bfl_flux3_params</c> feature flag.</remarks>
    [JsonProperty("fluxthreedirectvideoresolution")]
    public string? FluxThreeDirectVideoResolution { get; set; }

    #endregion

    #region FLUX 3 Video (fal.ai)
    /// <summary>Length of the generated video. ("FLUX Three Video Duration").</summary>
    /// <remarks>FLUX 3 runs any whole number of seconds from 5 to 20. Allowed values: <c>auto</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>, <c>13</c>, <c>14</c>, <c>15</c>, <c>16</c>, <c>17</c>, <c>18</c>, <c>19</c>, <c>20</c>. Gated behind the <c>fal_flux3_params</c> feature flag.</remarks>
    [JsonProperty("fluxthreevideoduration")]
    public string? FluxThreeVideoDuration { get; set; }

    /// <summary>Aspect ratio for FLUX 3. ("FLUX Three Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>21:9</c>, <c>2:1</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_flux3_params</c> feature flag.</remarks>
    [JsonProperty("fluxthreevideoaspectratio")]
    public string? FluxThreeVideoAspectRatio { get; set; }

    /// <summary>Resolution for FLUX 3. ("FLUX Three Video Resolution").</summary>
    /// <remarks>Allowed values: <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_flux3_params</c> feature flag.</remarks>
    [JsonProperty("fluxthreevideoresolution")]
    public string? FluxThreeVideoResolution { get; set; }

    /// <summary>Content filtering strictness. ("FLUX Three Safety Tolerance").</summary>
    /// <remarks>0 is strictest, 4 is most permissive. Server range 0–4. Server default: <c>2</c>. Gated behind the <c>fal_flux3_params</c> feature flag.</remarks>
    [JsonProperty("fluxthreesafetytolerance")]
    public int? FluxThreeSafetyTolerance { get; set; }

    #endregion

    #region FLUX.2 (fal.ai)
    /// <summary>Content filtering strictness. ("FLUX Two Safety Tolerance").</summary>
    /// <remarks>1 is most strict, 5 is most permissive. Server range 1–5. Server default: <c>2</c>. Gated behind the <c>fal_flux2_params</c> feature flag.</remarks>
    [JsonProperty("fluxtwosafetytolerance")]
    public int? FluxTwoSafetyTolerance { get; set; }

    #endregion

    #region FLUX.2 Edit (fal.ai)
    /// <summary>Output size for FLUX.2 editing. ("FLUX Two Edit Image Size").</summary>
    /// <remarks>'Auto' keeps the input image's dimensions. Allowed values: <c>auto</c>, <c>square_hd</c>, <c>square</c>, <c>portrait_4_3</c>, <c>portrait_16_9</c>, <c>landscape_4_3</c>, <c>landscape_16_9</c>. Gated behind the <c>fal_flux2_edit_params</c> feature flag.</remarks>
    [JsonProperty("fluxtwoeditimagesize")]
    public string? FluxTwoEditImageSize { get; set; }

    #endregion

    #region OpenAI (DALL-E, GPT-Image)
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

    #region GPT Image 1
    /// <summary>Dimensions of the generated image. ("GPT Image Output Resolution").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>1024x1024</c>, <c>1536x1024</c>, <c>1024x1536</c>. Gated behind the <c>gpt_image_params</c> feature flag.</remarks>
    [JsonProperty("gptimageoutputresolution")]
    public string? GptImageOutputResolution { get; set; }

    /// <summary>The compression level (0-100%) for JPEG and WebP formats. ("Output Compression").</summary>
    /// <remarks>Higher values mean better quality but larger file sizes. Only applies to JPEG and WebP output formats. Default is 100% (maximum quality). Server range 0–100. Server default: <c>100</c>. Gated behind the <c>gpt_image_params</c> feature flag.</remarks>
    [JsonProperty("outputcompression")]
    public int? OutputCompression { get; set; }

    #endregion

    #region Google (Gemini, Imagen)
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

    #region Grok
    /// <summary>Grok aspect ratio ("Grok Aspect Ratio").</summary>
    [JsonProperty("grokaspectratio")]
    public string? GrokAspectRatio { get; set; }

    /// <summary>Grok output resolution ("Grok Output Resolution").</summary>
    [JsonProperty("grokoutputresolution")]
    public string? GrokOutputResolution { get; set; }
    #endregion

    #region GPT Image 2
    /// <summary>Dimensions of the generated image. ("GPT Image Two Output Resolution").</summary>
    /// <remarks>GPT Image 2 reaches 2K; edges must be multiples of 16. Allowed values: <c>auto</c>, <c>1024x1024</c>, <c>1536x1024</c>, <c>1024x1536</c>, <c>2048x2048</c>, <c>2048x1152</c>, <c>1152x2048</c>. Gated behind the <c>gpt-image-2_params</c> feature flag.</remarks>
    [JsonProperty("gptimagetwooutputresolution")]
    public string? GptImageTwoOutputResolution { get; set; }

    #endregion

    #region Generic Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("Video Duration").</summary>
    /// <remarks>Allowed values: <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>8</c>, <c>10</c>. Gated behind the <c>fal_video_params</c> feature flag.</remarks>
    [JsonProperty("videoduration")]
    public string? VideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>. Gated behind the <c>fal_video_params</c> feature flag.</remarks>
    [JsonProperty("videoaspectratio")]
    public string? VideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Video Output Resolution").</summary>
    /// <remarks>Allowed values: <c>480p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_video_params</c> feature flag.</remarks>
    [JsonProperty("videooutputresolution")]
    public string? VideoOutputResolution { get; set; }

    #endregion

    #region Hunyuan Video (fal.ai)
    /// <summary>Aspect ratio for the generated video. ("Hunyuan Video Aspect Ratio").</summary>
    /// <remarks>Hunyuan supports 16:9 and 9:16. Allowed values: <c>16:9</c>, <c>9:16</c>. Gated behind the <c>fal_hunyuan_video_params</c> feature flag.</remarks>
    [JsonProperty("hunyuanvideoaspectratio")]
    public string? HunyuanVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Hunyuan Video Resolution").</summary>
    /// <remarks>Hunyuan supports 480p, 580p, and 720p. Allowed values: <c>480p</c>, <c>580p</c>, <c>720p</c>. Gated behind the <c>fal_hunyuan_video_params</c> feature flag.</remarks>
    [JsonProperty("hunyuanvideoresolution")]
    public string? HunyuanVideoResolution { get; set; }

    #endregion

    #region Ideogram
    /// <summary>Ideogram aspect ratio ("Ideogram Aspect Ratio"), e.g. "1:1", "16:9". Defaults to 1:1 server-side.</summary>
    [JsonProperty("ideogramaspectratio")]
    public string? IdeogramAspectRatio { get; set; }

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

    #region Ideogram V3
    /// <summary>Ideogram V3 rendering speed ("Rendering Speed"): DEFAULT, TURBO, QUALITY.</summary>
    [JsonProperty("renderingspeed")]
    public string? IdeogramRenderingSpeed { get; set; }

    /// <summary>Controls how strongly the input image influences the remixed generation (V3 only). ("Image Remix Weight").</summary>
    /// <remarks>Lower values give more creative freedom, higher values stay closer to the original. Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>ideogram_v3_params</c> feature flag.</remarks>
    [JsonProperty("imageremixweight")]
    public float? ImageRemixWeight { get; set; }

    #endregion

    #region Ideogram V4
    /// <summary>Ideogram V4 rendering speed ("Ideogram V4 Rendering Speed").</summary>
    [JsonProperty("ideogramvrenderingspeed")]
    public string? IdeogramV4RenderingSpeed { get; set; }

    /// <summary>Output resolution for Ideogram V4. ("Ideogram VFour Resolution").</summary>
    /// <remarks>Allowed values: <c>1024x1024</c>, <c>1344x768</c>, <c>768x1344</c>, <c>1536x640</c>, <c>640x1536</c>. Gated behind the <c>ideogram_v4_params</c> feature flag.</remarks>
    [JsonProperty("ideogramvfourresolution")]
    public string? IdeogramVFourResolution { get; set; }

    /// <summary>Ask Ideogram to flag potentially copyrighted content in the result. ("Ideogram Copyright Detection").</summary>
    /// <remarks>Server default: <c>false</c>. Gated behind the <c>ideogram_v4_params</c> feature flag.</remarks>
    [JsonProperty("ideogramcopyrightdetection")]
    public bool? IdeogramCopyrightDetection { get; set; }

    #endregion

    #region Image (fal.ai, cross-provider)
    /// <summary>Controls the dimensions of the generated image:. ("Image Size").</summary>
    /// <remarks>Square HD: 1024x1024 high definition Square: 512x512 standard Portrait: 768x1024 or 832x1216 Landscape: 1024x768 or 1216x832 Allowed values: <c>square_hd</c>, <c>square</c>, <c>portrait_4_3</c>, <c>portrait_16_9</c>, <c>landscape_4_3</c>, <c>landscape_16_9</c>. Gated behind the <c>fal_img_size</c> feature flag.</remarks>
    [JsonProperty("imagesize")]
    public string? ImageSize { get; set; }

    /// <summary>Aspect ratio for models that use aspect ratio instead of image size. ("Image Aspect Ratio").</summary>
    /// <remarks>16:9: Widescreen, 9:16: Portrait, 1:1: Square, etc. Allowed values: <c>auto</c>, <c>21:9</c>, <c>16:9</c>, <c>3:2</c>, <c>4:3</c>, <c>5:4</c>, <c>1:1</c>, <c>4:5</c>, <c>3:4</c>, <c>2:3</c>, <c>9:16</c>, <c>9:21</c>. Gated behind the <c>fal_aspect_image</c> feature flag.</remarks>
    [JsonProperty("imageaspectratio")]
    public string? ImageAspectRatio { get; set; }

    /// <summary>Resolution quality for the generated image. ("Image Resolution").</summary>
    /// <remarks>1K: Standard, 2K: High-res, 4K: Ultra high-res (some models only). Allowed values: <c>1K</c>, <c>2K</c>, <c>4K</c>. Gated behind the <c>fal_resolution_image</c> feature flag.</remarks>
    [JsonProperty("imageresolution")]
    public string? ImageResolution { get; set; }

    /// <summary>Enable or disable the NSFW safety checker. ("Safety Checker").</summary>
    /// <remarks>When enabled, inappropriate content will be filtered. Server default: <c>true</c>. Gated behind the <c>fal_img_common</c> feature flag.</remarks>
    [JsonProperty("safetychecker")]
    public bool? SafetyChecker { get; set; }

    /// <summary>Choose the file format for generated images:. ("Output File Format").</summary>
    /// <remarks>JPEG: Smaller files, slight quality loss, good for sharing PNG: Lossless quality, larger files, best for editing. Allowed values: <c>jpeg</c>, <c>png</c>. Gated behind the <c>fal_img_common</c> feature flag.</remarks>
    [JsonProperty("outputfileformat")]
    public string? OutputFileFormat { get; set; }

    /// <summary>Choose the file format for generated images. ("Fal Image Output Format").</summary>
    /// <remarks>Allowed values: <c>jpeg</c>, <c>png</c>, <c>webp</c>. Gated behind the <c>fal_aspect_image</c> feature flag.</remarks>
    [JsonProperty("falimageoutputformat")]
    public string? FalImageOutputFormat { get; set; }

    #endregion

    #region Image Utility (fal.ai, cross-provider)
    /// <summary>How many times larger to make the image. ("Upscale Factor").</summary>
    /// <remarks>Only applies to upscaler models; ignored by background removal and face restoration. Server range 1–4. Server default: <c>2</c>. Gated behind the <c>fal_utility_params</c> feature flag.</remarks>
    [JsonProperty("upscalefactor")]
    public float? UpscaleFactor { get; set; }

    #endregion

    #region Kandinsky Video (fal.ai)
    /// <summary>Length of the generated video. ("Kandinsky Video Duration").</summary>
    /// <remarks>Allowed values: <c>5</c>, <c>10</c>. Gated behind the <c>fal_kandinsky_params</c> feature flag.</remarks>
    [JsonProperty("kandinskyvideoduration")]
    public string? KandinskyVideoDuration { get; set; }

    /// <summary>Resolution for Kandinsky 5 Pro. ("Kandinsky Video Resolution").</summary>
    /// <remarks>This model uses its own scale rather than 480p/720p. Allowed values: <c>512P</c>, <c>1024P</c>. Gated behind the <c>fal_kandinsky_params</c> feature flag.</remarks>
    [JsonProperty("kandinskyvideoresolution")]
    public string? KandinskyVideoResolution { get; set; }

    #endregion

    #region Kling Turbo Video (fal.ai)
    /// <summary>Length of the generated video, 3 to 15 seconds. ("Kling Turbo Video Duration").</summary>
    /// <remarks>Kling V3 Turbo takes no aspect ratio, resolution, audio or seed controls. Allowed values: <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>, <c>13</c>, <c>14</c>, <c>15</c>. Gated behind the <c>fal_kling_turbo_params</c> feature flag.</remarks>
    [JsonProperty("klingturbovideoduration")]
    public string? KlingTurboVideoDuration { get; set; }

    #endregion

    #region Kling Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("Kling Video Duration").</summary>
    /// <remarks>Kling supports 3-15 second videos. Allowed values: <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>, <c>13</c>, <c>14</c>, <c>15</c>. Gated behind the <c>fal_kling_video_params</c> feature flag.</remarks>
    [JsonProperty("klingvideoduration")]
    public string? KlingVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Kling Video Aspect Ratio").</summary>
    /// <remarks>Kling supports 16:9, 9:16, and 1:1. Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>. Gated behind the <c>fal_kling_video_params</c> feature flag.</remarks>
    [JsonProperty("klingvideoaspectratio")]
    public string? KlingVideoAspectRatio { get; set; }

    /// <summary>Generate audio alongside the video. ("Kling Generate Audio").</summary>
    /// <remarks>Kling supports native audio generation in Chinese and English. Server default: <c>true</c>. Gated behind the <c>fal_kling_video_params</c> feature flag.</remarks>
    [JsonProperty("klinggenerateaudio")]
    public bool? KlingGenerateAudio { get; set; }

    /// <summary>Describe what you don't want in the generated video. ("Kling Negative Prompt").</summary>
    /// <remarks>Default: blur, distort, and low quality Gated behind the <c>fal_kling_video_params</c> feature flag.</remarks>
    [JsonProperty("klingnegativeprompt")]
    public string? KlingNegativePrompt { get; set; }

    #endregion

    #region LTX-13B Video (fal.ai)
    /// <summary>Length of the generated video. ("LTX-13B Video Duration").</summary>
    /// <remarks>Sent as a frame count at 24fps. Allowed values: <c>3</c>, <c>5</c>, <c>8</c>, <c>10</c>. Gated behind the <c>fal_ltx13b_params</c> feature flag.</remarks>
    [JsonProperty("ltxbvideoduration")]
    public string? Ltx13BVideoDuration { get; set; }

    /// <summary>Aspect ratio for LTX-13B distilled. ("LTX-13B Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>auto</c>, <c>16:9</c>, <c>9:16</c>, <c>1:1</c>. Gated behind the <c>fal_ltx13b_params</c> feature flag.</remarks>
    [JsonProperty("ltxbvideoaspectratio")]
    public string? Ltx13BVideoAspectRatio { get; set; }

    /// <summary>Resolution for LTX-13B distilled. ("LTX-13B Video Resolution").</summary>
    /// <remarks>This model tops out at 720p. Allowed values: <c>480p</c>, <c>720p</c>. Gated behind the <c>fal_ltx13b_params</c> feature flag.</remarks>
    [JsonProperty("ltxbvideoresolution")]
    public string? Ltx13BVideoResolution { get; set; }

    #endregion

    #region LTX-2 Video (fal.ai)
    /// <summary>Length of the generated video. ("LTX-2 Video Duration").</summary>
    /// <remarks>Sent as a frame count at 25fps. Allowed values: <c>3</c>, <c>5</c>, <c>8</c>, <c>10</c>. Gated behind the <c>fal_ltx2_params</c> feature flag.</remarks>
    [JsonProperty("ltxvideoduration")]
    public string? Ltx2VideoDuration { get; set; }

    #endregion

    #region Luma Video (fal.ai)
    /// <summary>Length of the generated video. ("Luma Video Duration").</summary>
    /// <remarks>Luma Ray 2 supports 5 or 9 second videos (9s costs 2x more). Allowed values: <c>5s</c>, <c>9s</c>. Gated behind the <c>fal_luma_video_params</c> feature flag.</remarks>
    [JsonProperty("lumavideoduration")]
    public string? LumaVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Luma Video Aspect Ratio").</summary>
    /// <remarks>Luma supports many aspect ratios. Allowed values: <c>16:9</c>, <c>9:16</c>, <c>4:3</c>, <c>3:4</c>, <c>21:9</c>, <c>9:21</c>. Gated behind the <c>fal_luma_video_params</c> feature flag.</remarks>
    [JsonProperty("lumavideoaspectratio")]
    public string? LumaVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Luma Video Resolution").</summary>
    /// <remarks>720p costs 2x, 1080p costs 4x more than 540p. Allowed values: <c>540p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_luma_video_params</c> feature flag.</remarks>
    [JsonProperty("lumavideoresolution")]
    public string? LumaVideoResolution { get; set; }

    #endregion

    #region MiniMax Hailuo 3 Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("MiniMax HThree Video Duration").</summary>
    /// <remarks>Allowed values: <c>5</c>, <c>6</c>, <c>8</c>, <c>10</c>, <c>12</c>, <c>15</c>. Gated behind the <c>fal_h3_params</c> feature flag.</remarks>
    [JsonProperty("minimaxhthreevideoduration")]
    public string? MiniMaxHThreeVideoDuration { get; set; }

    /// <summary>Aspect ratio for H3 text-to-video. ("MiniMax HThree Video Aspect Ratio").</summary>
    /// <remarks>Image-to-video follows the input image instead. Allowed values: <c>21:9</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_h3_aspect</c> feature flag.</remarks>
    [JsonProperty("minimaxhthreevideoaspectratio")]
    public string? MiniMaxHThreeVideoAspectRatio { get; set; }

    /// <summary>Aspect ratio for H3 reference-to-video. ("MiniMax HThree Reference Aspect Ratio").</summary>
    /// <remarks>'Adaptive' follows the references. Allowed values: <c>adaptive</c>, <c>21:9</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_h3_ref_aspect</c> feature flag.</remarks>
    [JsonProperty("minimaxhthreereferenceaspectratio")]
    public string? MiniMaxHThreeReferenceAspectRatio { get; set; }

    /// <summary>Output resolution. ("MiniMax HThree Video Resolution").</summary>
    /// <remarks>H3 uses its own scale rather than 480p/720p/1080p. Allowed values: <c>768P</c>, <c>2K</c>, <c>4K</c>. Gated behind the <c>fal_h3_params</c> feature flag.</remarks>
    [JsonProperty("minimaxhthreevideoresolution")]
    public string? MiniMaxHThreeVideoResolution { get; set; }

    #endregion

    #region MiniMax Video (fal.ai)
    /// <summary>Length of the generated video. ("MiniMax Video Duration").</summary>
    /// <remarks>MiniMax Hailuo supports 6 or 10 second videos. Allowed values: <c>6</c>, <c>10</c>. Gated behind the <c>fal_minimax_video_params</c> feature flag.</remarks>
    [JsonProperty("minimaxvideoduration")]
    public string? MiniMaxVideoDuration { get; set; }

    #endregion

    #region Nano Banana 2 / Gemini Image (fal.ai)
    /// <summary>Aspect ratio. ("Nano Banana Two Aspect Ratio").</summary>
    /// <remarks>Nano Banana 2 also accepts extreme banner and strip ratios. Allowed values: <c>auto</c>, <c>21:9</c>, <c>16:9</c>, <c>3:2</c>, <c>4:3</c>, <c>5:4</c>, <c>1:1</c>, <c>4:5</c>, <c>3:4</c>, <c>2:3</c>, <c>9:16</c>, <c>4:1</c>, <c>1:4</c>, <c>8:1</c>, <c>1:8</c>. Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatwoaspectratio")]
    public string? NanoBananaTwoAspectRatio { get; set; }

    /// <summary>Output resolution. ("Nano Banana Two Resolution").</summary>
    /// <remarks>Starts at 0.5K, unlike the other aspect-ratio models. Allowed values: <c>0.5K</c>, <c>1K</c>, <c>2K</c>, <c>4K</c>. Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatworesolution")]
    public string? NanoBananaTwoResolution { get; set; }

    /// <summary>Content filtering strictness, 1 (most strict) to 6 (most permissive). ("Nano Banana Two Safety Tolerance").</summary>
    /// <remarks>Server range 1–6. Server default: <c>4</c>. Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatwosafetytolerance")]
    public int? NanoBananaTwoSafetyTolerance { get; set; }

    /// <summary>How much reasoning the model applies before generating. ("Nano Banana Two Thinking Level").</summary>
    /// <remarks>'High' improves complex prompts and text rendering at the cost of speed. Allowed values: <c>(empty/default)</c>, <c>minimal</c>, <c>high</c>. Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatwothinkinglevel")]
    public string? NanoBananaTwoThinkingLevel { get; set; }

    /// <summary>Let the model search the web for reference while generating. ("Nano Banana Two Web Search").</summary>
    /// <remarks>Server default: <c>false</c>. Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatwowebsearch")]
    public bool? NanoBananaTwoWebSearch { get; set; }

    /// <summary>Optional system-level instruction applied before your prompt. ("Nano Banana Two System Prompt").</summary>
    /// <remarks>Gated behind the <c>fal_nb2_params</c> feature flag.</remarks>
    [JsonProperty("nanobananatwosystemprompt")]
    public string? NanoBananaTwoSystemPrompt { get; set; }

    #endregion

    #region OpenAI Sora Video
    /// <summary>Controls the resolution of the generated video. ("OpenAI Sora Video Size").</summary>
    /// <remarks>'1920x1080' - Full HD landscape (16:9) '1080x1920' - Full HD portrait (9:16) '1280x720' - HD landscape (16:9) '480x480' - Square format Allowed values: <c>1920x1080</c>, <c>1080x1920</c>, <c>1280x720</c>, <c>480x480</c>. Gated behind the <c>openai_sora_params</c> feature flag.</remarks>
    [JsonProperty("openaisoravideosize")]
    public string? OpenAISoraVideoSize { get; set; }

    /// <summary>Controls the duration of the generated video in seconds. ("OpenAI Sora Video Duration").</summary>
    /// <remarks>Sora supports videos from 5 to 20 seconds. Longer videos take more time and cost more credits. Server range 5–20. Server default: <c>10</c>. Gated behind the <c>openai_sora_params</c> feature flag.</remarks>
    [JsonProperty("openaisoravideoduration")]
    public int? OpenAISoraVideoDuration { get; set; }

    #endregion

    #region Pika Video (fal.ai)
    /// <summary>Length of the generated video. ("Pika Video Duration").</summary>
    /// <remarks>Pika v2.2 supports 5 or 10 seconds. Allowed values: <c>5</c>, <c>10</c>. Gated behind the <c>fal_pika_params</c> feature flag.</remarks>
    [JsonProperty("pikavideoduration")]
    public string? PikaVideoDuration { get; set; }

    /// <summary>Aspect ratio for Pika v2.2. ("Pika Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>, <c>4:5</c>, <c>5:4</c>, <c>3:2</c>, <c>2:3</c>. Gated behind the <c>fal_pika_params</c> feature flag.</remarks>
    [JsonProperty("pikavideoaspectratio")]
    public string? PikaVideoAspectRatio { get; set; }

    /// <summary>Resolution for Pika v2.2. ("Pika Video Resolution").</summary>
    /// <remarks>Allowed values: <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_pika_params</c> feature flag.</remarks>
    [JsonProperty("pikavideoresolution")]
    public string? PikaVideoResolution { get; set; }

    #endregion

    #region PixVerse Video (fal.ai)
    /// <summary>Length of the generated video. ("PixVerse Video Duration").</summary>
    /// <remarks>PixVerse v5 supports 5 or 8 seconds. Allowed values: <c>5</c>, <c>8</c>. Gated behind the <c>fal_pixverse_params</c> feature flag.</remarks>
    [JsonProperty("pixversevideoduration")]
    public string? PixVerseVideoDuration { get; set; }

    /// <summary>Aspect ratio for PixVerse v5. ("PixVerse Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_pixverse_params</c> feature flag.</remarks>
    [JsonProperty("pixversevideoaspectratio")]
    public string? PixVerseVideoAspectRatio { get; set; }

    /// <summary>Resolution for PixVerse v5. ("PixVerse Video Resolution").</summary>
    /// <remarks>Allowed values: <c>360p</c>, <c>540p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_pixverse_params</c> feature flag.</remarks>
    [JsonProperty("pixversevideoresolution")]
    public string? PixVerseVideoResolution { get; set; }

    #endregion

    #region Recraft (fal.ai)
    /// <summary>The visual style for Recraft V3 image generation. ("Recraft Style").</summary>
    /// <remarks>Choose a base style category. Sub-styles available via API. Allowed values: <c>any</c>, <c>realistic_image</c>, <c>realistic_image/b_and_w</c>, <c>realistic_image/hdr</c>, <c>realistic_image/natural_light</c>, <c>realistic_image/studio_portrait</c>, <c>digital_illustration</c>, <c>digital_illustration/pixel_art</c>, <c>digital_illustration/hand_drawn</c>, <c>digital_illustration/2d_art_poster</c>, <c>digital_illustration/handmade_3d</c>, <c>digital_illustration/pop_art</c>, <c>digital_illustration/noir</c>, <c>vector_illustration</c>, <c>vector_illustration/line_art</c>, <c>vector_illustration/bold_stroke</c>, <c>vector_illustration/flat</c>, <c>vector_illustration/linocut</c>. Gated behind the <c>fal_recraft_params</c> feature flag.</remarks>
    [JsonProperty("recraftstyle")]
    public string? RecraftStyle { get; set; }

    #endregion

    #region Reference Audio (fal.ai, cross-provider)
    /// <summary>Comma-separated URLs of reference audio clips. ("Reference Audio URLs").</summary>
    /// <remarks>Refer to them in your prompt in order (Audio 1, Audio 2, ...). Up to 3, 2-15s each. Requires at least one reference image or video alongside it. Gated behind the <c>fal_ref_audio</c> feature flag.</remarks>
    [JsonProperty("referenceaudiourls")]
    public string? ReferenceAudioUrls { get; set; }

    #endregion

    #region Reference Images (fal.ai, cross-provider)
    /// <summary>Comma-separated URLs of reference images for subject and style. ("Reference Image URLs").</summary>
    /// <remarks>Refer to them in your prompt in order (Image 1, Image 2, ...). Seedance and MiniMax H3 accept up to 9; Wan 2.7 has no fixed cap. Gated behind the <c>fal_ref_images</c> feature flag.</remarks>
    [JsonProperty("referenceimageurls")]
    public string? ReferenceImageUrls { get; set; }

    #endregion

    #region Reference Videos (fal.ai, cross-provider)
    /// <summary>Comma-separated URLs of reference videos for motion. ("Reference Video URLs").</summary>
    /// <remarks>Refer to them in your prompt in order (Video 1, Video 2, ...). Up to 3, 2-15s each. Gated behind the <c>fal_ref_videos</c> feature flag.</remarks>
    [JsonProperty("referencevideourls")]
    public string? ReferenceVideoUrls { get; set; }

    #endregion

    #region Seedance One Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("Seedance One Video Duration").</summary>
    /// <remarks>Seedance 1.0 supports 2-12 second videos. Allowed values: <c>2</c>, <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>. Gated behind the <c>fal_seedance1_video_params</c> feature flag.</remarks>
    [JsonProperty("seedanceonevideoduration")]
    public string? SeedanceOneVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Seedance One Video Aspect Ratio").</summary>
    /// <remarks>Seedance 1.0 supports multiple aspect ratios. Allowed values: <c>21:9</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_seedance1_video_params</c> feature flag.</remarks>
    [JsonProperty("seedanceonevideoaspectratio")]
    public string? SeedanceOneVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Seedance One Video Resolution").</summary>
    /// <remarks>Seedance 1.0 supports 480p, 720p, and 1080p. Allowed values: <c>480p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_seedance1_video_params</c> feature flag.</remarks>
    [JsonProperty("seedanceonevideoresolution")]
    public string? SeedanceOneVideoResolution { get; set; }

    /// <summary>Whether to fix the camera position during video generation. ("Seedance One Camera Fixed").</summary>
    /// <remarks>When enabled, the camera stays stationary. Server default: <c>false</c>. Gated behind the <c>fal_seedance1_video_params</c> feature flag.</remarks>
    [JsonProperty("seedanceonecamerafixed")]
    public bool? SeedanceOneCameraFixed { get; set; }

    #endregion

    #region Seedance Two Video (fal.ai)
    /// <summary>Length of the generated video. ("Seedance Two Video Duration").</summary>
    /// <remarks>Seedance 2.0 supports auto or 4-15 second videos. Allowed values: <c>auto</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>, <c>13</c>, <c>14</c>, <c>15</c>. Gated behind the <c>fal_seedance2_duration</c> feature flag.</remarks>
    [JsonProperty("seedancetwovideoduration")]
    public string? SeedanceTwoVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Seedance Two Video Aspect Ratio").</summary>
    /// <remarks>Seedance 2.0 supports multiple aspect ratios including auto. Allowed values: <c>auto</c>, <c>21:9</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>3:4</c>, <c>9:16</c>. Gated behind the <c>fal_seedance2_video_params</c> feature flag.</remarks>
    [JsonProperty("seedancetwovideoaspectratio")]
    public string? SeedanceTwoVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Seedance Two Video Resolution").</summary>
    /// <remarks>Seedance 2.0 supports 480p and 720p. Allowed values: <c>480p</c>, <c>720p</c>. Gated behind the <c>fal_seedance2_video_params</c> feature flag.</remarks>
    [JsonProperty("seedancetwovideoresolution")]
    public string? SeedanceTwoVideoResolution { get; set; }

    /// <summary>Generate synchronized audio alongside the video. ("Seedance Two Generate Audio").</summary>
    /// <remarks>Seedance 2.0 supports native audio generation including lip-synced speech. Server default: <c>true</c>. Gated behind the <c>fal_seedance2_video_params</c> feature flag.</remarks>
    [JsonProperty("seedancetwogenerateaudio")]
    public bool? SeedanceTwoGenerateAudio { get; set; }

    #endregion

    #region Seedance TwoFive Video (fal.ai)
    /// <summary>Length of the generated video. ("Seedance TwoFive Video Duration").</summary>
    /// <remarks>Seedance 2.5 runs up to 30 seconds in a single shot. 28 allowed values (stepped), server default <c>auto</c>. Gated behind the <c>fal_seedance25_params</c> feature flag.</remarks>
    [JsonProperty("seedancetwofivevideoduration")]
    public string? SeedanceTwoFiveVideoDuration { get; set; }

    #endregion

    #region Sora Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("Sora Video Duration").</summary>
    /// <remarks>Sora supports 4, 8, or 12 second videos. Allowed values: <c>4</c>, <c>8</c>, <c>12</c>. Gated behind the <c>fal_sora_video_params</c> feature flag.</remarks>
    [JsonProperty("soravideoduration")]
    public string? SoraVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Sora Video Aspect Ratio").</summary>
    /// <remarks>Sora supports 16:9 (widescreen) and 9:16 (portrait). Allowed values: <c>16:9</c>, <c>9:16</c>. Gated behind the <c>fal_sora_video_params</c> feature flag.</remarks>
    [JsonProperty("soravideoaspectratio")]
    public string? SoraVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Sora Video Resolution").</summary>
    /// <remarks>Sora supports 720p and 1080p. Allowed values: <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_sora_video_params</c> feature flag.</remarks>
    [JsonProperty("soravideoresolution")]
    public string? SoraVideoResolution { get; set; }

    #endregion

    #region Veo Video (fal.ai)
    /// <summary>Length of the generated video. ("Veo Video Duration").</summary>
    /// <remarks>Veo supports 4, 6, or 8 second videos. Allowed values: <c>4s</c>, <c>6s</c>, <c>8s</c>. Gated behind the <c>fal_veo_video_params</c> feature flag.</remarks>
    [JsonProperty("veovideoduration")]
    public string? VeoVideoDuration { get; set; }

    /// <summary>Aspect ratio for the generated video. ("Veo Video Aspect Ratio").</summary>
    /// <remarks>Veo supports 16:9 and 9:16. Allowed values: <c>16:9</c>, <c>9:16</c>. Gated behind the <c>fal_veo_video_params</c> feature flag.</remarks>
    [JsonProperty("veovideoaspectratio")]
    public string? VeoVideoAspectRatio { get; set; }

    /// <summary>Resolution of the generated video. ("Veo Video Resolution").</summary>
    /// <remarks>Veo supports 720p and 1080p. Allowed values: <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_veo_video_params</c> feature flag.</remarks>
    [JsonProperty("veovideoresolution")]
    public string? VeoVideoResolution { get; set; }

    /// <summary>Generate audio alongside the video. ("Veo Generate Audio").</summary>
    /// <remarks>Veo 3 supports native audio generation. Server default: <c>true</c>. Gated behind the <c>fal_veo_video_params</c> feature flag.</remarks>
    [JsonProperty("veogenerateaudio")]
    public bool? VeoGenerateAudio { get; set; }

    /// <summary>Describe what you don't want in the generated video. ("Veo Negative Prompt").</summary>
    /// <remarks>Gated behind the <c>fal_veo_video_params</c> feature flag.</remarks>
    [JsonProperty("veonegativeprompt")]
    public string? VeoNegativePrompt { get; set; }

    #endregion

    #region Video Audio (fal.ai, cross-provider)
    /// <summary>Generate audio alongside the video. ("Generate Audio").</summary>
    /// <remarks>Only shown for models that actually support it. Server default: <c>true</c>. Gated behind the <c>fal_video_audio</c> feature flag.</remarks>
    [JsonProperty("generateaudio")]
    public bool? GenerateAudio { get; set; }

    #endregion

    #region Video End Frame (fal.ai, cross-provider)
    /// <summary>Publicly accessible image URL to use as the final frame. ("Last Frame Image URL").</summary>
    /// <remarks>The model generates the motion between your Init Image and this one. Gated behind the <c>fal_end_image_url</c> feature flag.</remarks>
    [JsonProperty("lastframeimageurl")]
    public string? LastFrameImageUrl { get; set; }

    #endregion

    #region Video Negative Prompt (fal.ai, cross-provider)
    /// <summary>Describe what you don't want in the generated video. ("Video Negative Prompt").</summary>
    /// <remarks>Gated behind the <c>fal_video_negative</c> feature flag.</remarks>
    [JsonProperty("videonegativeprompt")]
    public string? VideoNegativePrompt { get; set; }

    #endregion

    #region Video Prompt Expansion (fal.ai, cross-provider)
    /// <summary>Let the model rewrite your prompt for richer detail. ("Prompt Expansion").</summary>
    /// <remarks>Disable for literal prompt following. Server default: <c>true</c>. Gated behind the <c>fal_prompt_expansion</c> feature flag.</remarks>
    [JsonProperty("promptexpansion")]
    public bool? PromptExpansion { get; set; }

    #endregion

    #region Video Utility (fal.ai, cross-provider)
    /// <summary>Publicly accessible URL of the video to process. ("Input Video URL").</summary>
    /// <remarks>Required by the video utility models (video upscale, video background removal), which take a video rather than the Init Image used by the image utilities. Gated behind the <c>fal_utility_video_params</c> feature flag.</remarks>
    [JsonProperty("inputvideourl")]
    public string? InputVideoUrl { get; set; }

    #endregion

    #region Vidu Video (fal.ai)
    /// <summary>Length of the generated video in seconds. ("Vidu Video Duration").</summary>
    /// <remarks>Allowed values: <c>3</c>, <c>5</c>, <c>8</c>, <c>10</c>. Gated behind the <c>fal_vidu_params</c> feature flag.</remarks>
    [JsonProperty("viduvideoduration")]
    public string? ViduVideoDuration { get; set; }

    /// <summary>Aspect ratio for Vidu Q3. ("Vidu Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>9:16</c>, <c>4:3</c>, <c>3:4</c>, <c>1:1</c>. Gated behind the <c>fal_vidu_params</c> feature flag.</remarks>
    [JsonProperty("viduvideoaspectratio")]
    public string? ViduVideoAspectRatio { get; set; }

    /// <summary>Resolution for Vidu Q3. ("Vidu Video Resolution").</summary>
    /// <remarks>Allowed values: <c>360p</c>, <c>540p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_vidu_params</c> feature flag.</remarks>
    [JsonProperty("viduvideoresolution")]
    public string? ViduVideoResolution { get; set; }

    #endregion

    #region Wan 2.2 Video (fal.ai)
    /// <summary>Length of the generated video. ("Wan Video Duration").</summary>
    /// <remarks>Wan 2.2 has no duration field, so this is sent as a frame count at 16fps. Allowed values: <c>1</c>, <c>2</c>, <c>3</c>, <c>5</c>, <c>8</c>, <c>10</c>. Gated behind the <c>fal_wan22_params</c> feature flag.</remarks>
    [JsonProperty("wanvideoduration")]
    public string? WanVideoDuration { get; set; }

    /// <summary>Aspect ratio for Wan 2.2. ("Wan Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>. Gated behind the <c>fal_wan22_params</c> feature flag.</remarks>
    [JsonProperty("wanvideoaspectratio")]
    public string? WanVideoAspectRatio { get; set; }

    /// <summary>Resolution for Wan 2.2. ("Wan Video Resolution").</summary>
    /// <remarks>This model tops out at 720p. Allowed values: <c>480p</c>, <c>580p</c>, <c>720p</c>. Gated behind the <c>fal_wan22_params</c> feature flag.</remarks>
    [JsonProperty("wanvideoresolution")]
    public string? WanVideoResolution { get; set; }

    #endregion

    #region Wan 2.5 Video (fal.ai)
    /// <summary>Length of the generated video. ("Wan TwoFive Video Duration").</summary>
    /// <remarks>Wan 2.5 supports 5 or 10 seconds. Allowed values: <c>5</c>, <c>10</c>. Gated behind the <c>fal_wan25_params</c> feature flag.</remarks>
    [JsonProperty("wantwofivevideoduration")]
    public string? WanTwoFiveVideoDuration { get; set; }

    /// <summary>Aspect ratio for Wan 2.5. ("Wan TwoFive Video Aspect Ratio").</summary>
    /// <remarks>Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>. Gated behind the <c>fal_wan25_params</c> feature flag.</remarks>
    [JsonProperty("wantwofivevideoaspectratio")]
    public string? WanTwoFiveVideoAspectRatio { get; set; }

    /// <summary>Resolution for Wan 2.5. ("Wan TwoFive Video Resolution").</summary>
    /// <remarks>Allowed values: <c>480p</c>, <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_wan25_params</c> feature flag.</remarks>
    [JsonProperty("wantwofivevideoresolution")]
    public string? WanTwoFiveVideoResolution { get; set; }

    #endregion

    #region Wan 2.6 Video (fal.ai)
    /// <summary>Length of the generated video. ("Wan TwoSix Video Duration").</summary>
    /// <remarks>Wan 2.6 supports 5, 10 or 15 seconds. Allowed values: <c>5</c>, <c>10</c>, <c>15</c>. Gated behind the <c>fal_wan26_params</c> feature flag.</remarks>
    [JsonProperty("wantwosixvideoduration")]
    public string? WanTwoSixVideoDuration { get; set; }

    #endregion

    #region Wan 2.7 Reference Video (fal.ai)
    /// <summary>Length of the generated video. ("Wan TwoSeven Reference Video Duration").</summary>
    /// <remarks>Reference-to-video is capped at 10 seconds. Allowed values: <c>2</c>, <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>. Gated behind the <c>fal_wan27ref_params</c> feature flag.</remarks>
    [JsonProperty("wantwosevenreferencevideoduration")]
    public string? WanTwoSevenReferenceVideoDuration { get; set; }

    #endregion

    #region Wan 2.7 Video (fal.ai)
    /// <summary>Length of the generated video, 2 to 15 seconds. ("Wan TwoSeven Video Duration").</summary>
    /// <remarks>Allowed values: <c>2</c>, <c>3</c>, <c>4</c>, <c>5</c>, <c>6</c>, <c>7</c>, <c>8</c>, <c>9</c>, <c>10</c>, <c>11</c>, <c>12</c>, <c>13</c>, <c>14</c>, <c>15</c>. Gated behind the <c>fal_wan27_params</c> feature flag.</remarks>
    [JsonProperty("wantwosevenvideoduration")]
    public string? WanTwoSevenVideoDuration { get; set; }

    /// <summary>Aspect ratio for Wan 2.7. ("Wan TwoSeven Video Aspect Ratio").</summary>
    /// <remarks>Not used by image-to-video, which follows the input image. Allowed values: <c>16:9</c>, <c>9:16</c>, <c>1:1</c>, <c>4:3</c>, <c>3:4</c>. Gated behind the <c>fal_wan27_aspect</c> feature flag.</remarks>
    [JsonProperty("wantwosevenvideoaspectratio")]
    public string? WanTwoSevenVideoAspectRatio { get; set; }

    #endregion

    #region Wan 2.x Video (fal.ai)
    /// <summary>Resolution for Wan 2.6 and 2.7. ("Wan TwoSixPlus Video Resolution").</summary>
    /// <remarks>These versions start at 720p. Allowed values: <c>720p</c>, <c>1080p</c>. Gated behind the <c>fal_wan2x_resolution</c> feature flag.</remarks>
    [JsonProperty("wantwosixplusvideoresolution")]
    public string? WanTwoSixPlusVideoResolution { get; set; }

    #endregion

    #region Wan Video (fal.ai)
    /// <summary>Publicly accessible WAV or MP3 URL to drive the video's motion and timing. ("Reference Audio URL").</summary>
    /// <remarks>3-30 seconds, up to 15 MB. Leave empty for a silent generation. Gated behind the <c>fal_wan_audio</c> feature flag.</remarks>
    [JsonProperty("referenceaudiourl")]
    public string? ReferenceAudioUrl { get; set; }

    /// <summary>Let the model split the video into multiple camera shots. ("Multi-Shot Segmentation").</summary>
    /// <remarks>Only takes effect when Prompt Expansion is enabled. Server default: <c>false</c>. Gated behind the <c>fal_wan_multishot</c> feature flag.</remarks>
    [JsonProperty("multishotsegmentation")]
    public bool? MultiShotSegmentation { get; set; }

    #endregion
}
