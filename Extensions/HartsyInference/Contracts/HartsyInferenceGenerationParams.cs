using Newtonsoft.Json;
using SwarmUI.ApiClient.Contracts.Requests;

namespace SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;

/// <summary>Generation parameters registered by the HartsyInference server extension for model families it
/// supports beyond plain text-to-image: ACE-Step music, Wan-Animate, Ideogram 4, MiniMax Music, reference-audio
/// / reference-edit inputs, and a handful of parameters that read like general-purpose SwarmUI/ComfyUI features
/// but are, per the server source, registered only by this extension.</summary>
/// <remarks>
/// <para><b>Coverage: 51 of the 53 parameters HartsyInference registers server-side are modeled here.</b></para>
/// <para>Attached to a request via <see cref="SwarmUI.ApiClient.Extensions.GenerationRequestExtensionParams.HartsyInference"/>
/// (<c>request.Extensions.HartsyInference</c>).</para>
/// <para>These are distinct from the identically-shaped stock parameters elsewhere on
/// <see cref="GenerationRequest"/> — for example <see cref="InitImageMode"/>'s <c>reference</c> value only exists
/// because this extension registers it; a vanilla server without HartsyInference installed does not offer it.</para>
/// <para>Only send parameters advertised by the selected model's feature flags.</para>
/// </remarks>
public class HartsyInferenceGenerationParams
{
    #region ACE-Step (via HartsyInference)
    /// <summary>Source audio clip for ACE-Step music editing. ("ACE-Step Source Audio").</summary>
    /// <remarks>What happens to it is picked by ACE-Step Edit Mode: continuation extends it, repaint regenerates a time span inside it, cover re-renders it in the prompt's style. ACE-Step models only. Cannot be combined with the ACE-Step LM Planner. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepsourceaudio")]
    public string? AceStepSourceAudio { get; set; }

    /// <summary>What the Source Audio is used for. ("ACE-Step Edit Mode").</summary>
    /// <remarks>'continuation' = generate Duration seconds continuing past the clip. 'repaint' = regenerate only Repaint Start..Repaint End seconds inside the clip. 'cover' = re-render the whole clip in the prompt's style at Cover Strength. Allowed values: <c>continuation</c>, <c>repaint</c>, <c>cover</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepeditmode")]
    public string? AceStepEditMode { get; set; }

    /// <summary>Repaint mode: start of the regenerated span, in seconds from the start of the Source Audio. ("ACE-Step Repaint Start").</summary>
    /// <remarks>Server range 0–600. Server default: <c>0</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acestepsourceaudio</c> is set.</remarks>
    [JsonProperty("acesteprepaintstart")]
    public float? AceStepRepaintStart { get; set; }

    /// <summary>Repaint mode: end of the regenerated span, in seconds. ("ACE-Step Repaint End").</summary>
    /// <remarks>Must be greater than Repaint Start. Server range 0–600. Server default: <c>0</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acestepsourceaudio</c> is set.</remarks>
    [JsonProperty("acesteprepaintend")]
    public float? AceStepRepaintEnd { get; set; }

    /// <summary>Cover mode: how much of the source is re-rendered (0 keeps it, 1 fully regenerates). ("ACE-Step Cover Strength").</summary>
    /// <remarks>Clamped to 0.05 minimum engine-side. Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acestepsourceaudio</c> is set.</remarks>
    [JsonProperty("acestepcoverstrength")]
    public float? AceStepCoverStrength { get; set; }

    /// <summary>ACE-Step 5 Hz LM planner: a language model that plans the song's structure before diffusion. ("ACE-Step LM Planner").</summary>
    /// <remarks>'0.6b' or '4b' selects the planner size (auto-downloaded). Cannot be combined with Source Audio editing. Allowed values: <c>none</c>, <c>0.6b</c>, <c>4b</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acesteplmplanner")]
    public string? AceStepLmPlanner { get; set; }

    /// <summary>Planner thinking mode (also selects the matching guidance scalers). ("ACE-Step LM Thinking").</summary>
    /// <remarks>Server default: <c>true</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmthinking")]
    public bool? AceStepLmThinking { get; set; }

    /// <summary>Planner sampling temperature. ("ACE-Step LM Temperature").</summary>
    /// <remarks>Server range 0–2. Server default: <c>0.85</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmtemperature")]
    public float? AceStepLmTemperature { get; set; }

    /// <summary>Planner guidance scale. ("ACE-Step LM CFG Scale").</summary>
    /// <remarks>Server range 1–10. Server default: <c>2</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmcfgscale")]
    public float? AceStepLmCfgScale { get; set; }

    /// <summary>Planner top-k sampling cutoff (0 = disabled). ("ACE-Step LM Top K").</summary>
    /// <remarks>Server range 0–500. Server default: <c>0</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmtopk")]
    public int? AceStepLmTopK { get; set; }

    /// <summary>Planner nucleus sampling cutoff. ("ACE-Step LM Top P").</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.9</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmtopp")]
    public float? AceStepLmTopP { get; set; }

    /// <summary>Planner negative prompt. ("ACE-Step LM Negative Prompt").</summary>
    /// <remarks>Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags. Does nothing unless <c>acesteplmplanner</c> is set.</remarks>
    [JsonProperty("acesteplmnegativeprompt")]
    public string? AceStepLmNegativePrompt { get; set; }

    /// <summary>ACE-Step diffusion solver: 'ode' (Euler, default) or 'sde' (predict-clean + renoise). ("ACE-Step Solver").</summary>
    /// <remarks>Allowed values: <c>ode</c>, <c>sde</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepsolver")]
    public string? AceStepSolver { get; set; }

    /// <summary>ACE-Step: guidance blend when CFG &gt; 1 on non-turbo checkpoints. ("ACE-Step Guidance Type").</summary>
    /// <remarks>apg (default, momentum-projected), cfg (plain classifier-free), adg. Allowed values: <c>apg</c>, <c>cfg</c>, <c>adg</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepguidancetype")]
    public string? AceStepGuidanceType { get; set; }

    /// <summary>ACE-Step: CFG applies only while sigma is inside this 0..1 interval. ("ACE-Step CFG Interval Start").</summary>
    /// <remarks>Server range 0–1. Server default: <c>0</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepcfgintervalstart")]
    public float? AceStepCfgIntervalStart { get; set; }

    /// <summary>ACE-Step: upper edge of the CFG sigma interval. ("ACE-Step CFG Interval End").</summary>
    /// <remarks>Server range 0–1. Server default: <c>1</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepcfgintervalend")]
    public float? AceStepCfgIntervalEnd { get; set; }

    /// <summary>ACE-Step v1: entropy-rectifying guidance on the tag branch — the unconditional pass sees a weakened (not zeroed) text encoding. ("ACE-Step V1 ERG Tag").</summary>
    /// <remarks>Upstream default is on. v1 checkpoints only. Server default: <c>true</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepvergtag")]
    public bool? AceStepV1ErgTag { get; set; }

    /// <summary>ACE-Step v1: the unconditional pass keeps the lyrics with a weakened lyric encoder instead of dropping them. ("ACE-Step V1 ERG Lyric").</summary>
    /// <remarks>Upstream default is on. v1 checkpoints only. Server default: <c>true</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepverglyric")]
    public bool? AceStepV1ErgLyric { get; set; }

    /// <summary>ACE-Step v1: the unconditional diffusion forwards run with weakened attention queries in the middle blocks. ("ACE-Step V1 ERG Diffusion").</summary>
    /// <remarks>Upstream default is on. v1 checkpoints only. Server default: <c>true</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_acestep</c> feature flags.</remarks>
    [JsonProperty("acestepvergdiffusion")]
    public bool? AceStepV1ErgDiffusion { get; set; }

    #endregion

    #region Audio Reference (via HartsyInference)
    /// <summary>For audio+video models that support a reference audio clip (e.g. MiniMax-H3 voice reference): the generated soundtrack imitates this clip's voice/style rather than treating it as literal input audio. ("Video Audio Reference").</summary>
    /// <remarks>Gated behind the <c>hartsyinference</c> and <c>hartsy_audio_ref</c> feature flags.</remarks>
    [JsonProperty("videoaudioreference")]
    public string? VideoAudioReference { get; set; }

    #endregion

    #region Ideogram 4 (via HartsyInference)
    /// <summary>Rewrite your plain prompt into Ideogram 4's structured JSON caption using a running LLM backend, the way Ideogram's own stack does. ("Ideogram 4 Magic Prompt").</summary>
    /// <remarks>Requires an LLM backend (Server &gt; Backends — LlamaSharp, Anthropic, or remote). When off (default), your prompt is sent to the model as-is (which also works). Server default: <c>false</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_ideogram4</c> feature flags.</remarks>
    [JsonProperty("ideogrammagicprompt")]
    public bool? Ideogram4MagicPrompt { get; set; }

    /// <summary>Optional: which LLM model the magic prompt should use (must be available on a running LLM backend). ("Ideogram 4 Magic Prompt LLM").</summary>
    /// <remarks>Leave unset to use the running LLM backend's default model. Only used when 'Ideogram 4 Magic Prompt' is on. Gated behind the <c>hartsyinference</c> and <c>hartsy_ideogram4</c> feature flags. Does nothing unless <c>ideogrammagicprompt</c> is set.</remarks>
    [JsonProperty("ideogrammagicpromptllm")]
    public string? Ideogram4MagicPromptLlm { get; set; }

    #endregion

    #region MiniMax Music (via HartsyInference)
    /// <summary>Precision for MiniMax Music 3's 8B language model stage: bf16 (checkpoint precision), q8 or q4 (GGUF-quantized, for smaller cards). ("MiniMax Music LM Precision").</summary>
    /// <remarks>The flow DiT always runs the selected checkpoint's weights. Allowed values: <c>bf16</c>, <c>q8</c>, <c>q4</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_minimaxmusic</c> feature flags.</remarks>
    [JsonProperty("minimaxmusiclmprecision")]
    public string? MiniMaxMusicLmPrecision { get; set; }

    #endregion

    #region Wan-Animate (via HartsyInference)
    /// <summary>Wan-Animate: the character/identity image to animate. ("Animate Reference Image").</summary>
    /// <remarks>The Init Image slot carries the driving (pose/motion) video; this image is who performs that motion. An image attached to the prompt box works too, matching how core's ComfyUI backend carries Wan reference images; this param wins if both are set. Required (one way or the other) for Wan-Animate generations on the HartsyInference backend. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags.</remarks>
    [JsonProperty("animatereferenceimage")]
    public string? AnimateReferenceImage { get; set; }

    /// <summary>Wan-Animate: auto-derive the pose skeleton + cropped face from the Init-Image driving video (the format the model was trained on). ("Animate Auto-Preprocess").</summary>
    /// <remarks>On (default) = best motion fidelity; off = feed the raw clip (legacy). The pose/face override params below take precedence when set. Server default: <c>true</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags.</remarks>
    [JsonProperty("animateautopreprocess")]
    public bool? AnimateAutoPreprocess { get; set; }

    /// <summary>Wan-Animate: an already-rendered pose/skeleton driving video (OpenPose/DWPose colored limbs). ("Animate Pose Video").</summary>
    /// <remarks>Overrides auto-preprocessing for the pose branch — supply this when you have a pre-rendered skeleton. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animateposevideo")]
    public string? AnimatePoseVideo { get; set; }

    /// <summary>Wan-Animate: an already-cropped, face-centered driving video (square, ~512px) for the facial-motion branch. ("Animate Face Video").</summary>
    /// <remarks>Overrides auto-preprocessing for the face branch. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatefacevideo")]
    public string? AnimateFaceVideo { get; set; }

    /// <summary>Wan-Animate replacement mode: the background video the character is composited into. ("Animate Background Video").</summary>
    /// <remarks>The conditioning carries these frames instead of the mid-gray placeholder, so unmasked regions keep this background. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatebackgroundvideo")]
    public string? AnimateBackgroundVideo { get; set; }

    /// <summary>Wan-Animate replacement mode: per-frame mask video (white = generate the character there, black = keep the background). ("Animate Character Mask").</summary>
    /// <remarks>A single image repeats across all frames. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatecharactermask")]
    public string? AnimateCharacterMask { get; set; }

    /// <summary>Wan-Animate: total length of the finished video, generated as back-to-back chunks of 'Video Frames' each. ("Animate Total Frames").</summary>
    /// <remarks>Leave off (or at or below Video Frames) for a single chunk. Every chunk after the first re-renders and discards a short motion-context prefix, so the driving video must be at least this long — a shorter one freezes on its last frame. Server range 0–2048. Server default: <c>0</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatetotalframes")]
    public int? AnimateTotalFrames { get; set; }

    /// <summary>Wan-Animate: how many frames of the previous chunk each new chunk sees as motion context, which is what keeps the seams continuous. ("Animate Motion Context Frames").</summary>
    /// <remarks>5 (default) is the reference value. Higher holds continuity better and costs that many re-rendered frames per chunk; values are snapped down onto the 4n+1 grid (1, 5, 9, 13, ...). Server range 1–61. Server default: <c>5</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatemotioncontextframes")]
    public int? AnimateMotionContextFrames { get; set; }

    /// <summary>Wan-Animate chunked generation: strength of the per-chunk color correction. ("Animate Color Correction").</summary>
    /// <remarks>Each chunk after the first is color-matched (Lab mean/std) to the reference image, so color drift cannot compound across chunks. 1 (default) = fully matched, 0 = off. Only multi-chunk (Total Frames) generations are affected — the first chunk is never touched. Server range 0–1. Server default: <c>1</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatecolorcorrection")]
    public float? AnimateColorCorrection { get; set; }

    /// <summary>Wan-Animate-2: how strongly the driving video's motion steers the generation (ComfyUI pose_strength) — above 1 over-drives the motion, below 1 under-drives it, and 0 does not fully mute the driving branch. ("Animate Pose Strength").</summary>
    /// <remarks>1 (default) is the trained behaviour. This is a deliberate stylization knob, NOT a fix for washed-out or hazy output — that comes from running a checkpoint at the wrong steps/CFG, not from strength. Animate-2 checkpoints only; Wan-Animate V1 refuses a non-default value. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animateposestrength")]
    public float? AnimatePoseStrength { get; set; }

    /// <summary>Wan-Animate-2: how strongly the character reference image is attended (ComfyUI reference_image_strength) — above 1 weights identity harder, below 1 loosens it. ("Animate Reference Strength").</summary>
    /// <remarks>1 (default) is the trained behaviour. This is a deliberate stylization knob, NOT a fix for washed-out or hazy output — that comes from running a checkpoint at the wrong steps/CFG, not from strength. Animate-2 checkpoints only; Wan-Animate V1 refuses a non-default value. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>hartsyinference</c> and <c>hartsy_wan_animate</c> feature flags. Does nothing unless <c>animatereferenceimage</c> is set.</remarks>
    [JsonProperty("animatereferencestrength")]
    public float? AnimateReferenceStrength { get; set; }

    #endregion

    #region Core Feature Overrides (CFG Rescale, Init Image Mode, Restore/SeedVR, VRAM & Memory)
    // Confirmed live against a stock ComfyUI backend: these wire names are registered ONLY by
    // HartsyInference, even though they read like general-purpose SwarmUI features. Setting any of them
    // against a server without this extension makes the whole request fail backend matching with
    // "Request requires flag 'hartsyinference' which is not present on the backend".

    /// <summary>Pulls a high-CFG-Scale guided prediction back toward the conditional prediction's magnitude, reducing the oversaturated/burnt-highlights look that high CFG Scale causes. ("CFG Rescale").</summary>
    /// <remarks>0 (default) = off. 0.7 is a reasonable starting point at CFG Scale 10+. Only SDXL honors this today. Not the same math as ComfyUI's RescaleCFG node — this rescales per-token L2 norm, not per-sample standard deviation — so the same numeric value produces a different-strength effect. Server range 0–1. Server default: <c>0</c>.</remarks>
    [JsonProperty("cfgrescale")]
    public float? CfgRescale { get; set; }

    /// <summary>How the Init Image is consumed. ("Init Image Mode").</summary>
    /// <remarks>'denoise' is classic img2img, where Creativity picks how much is regenerated; 'reference' is HartsyInference's own mode. Allowed values: <c>auto</c>, <c>denoise</c>, <c>reference</c>. Does nothing unless <c>initimage</c> is set.</remarks>
    [JsonProperty("initimagemode")]
    public string? InitImageMode { get; set; }

    /// <summary>Restore/upscale the generated output with SeedVR2 — video frames before muxing, or a still image after generation. ("Restore Model").</summary>
    /// <remarks>Toggle ON to enable; the weights are fetched on first use. Allowed values: <c>seedvr2-3b</c>, <c>seedvr2-7b</c>.</remarks>
    [JsonProperty("restoremodel")]
    public string? RestoreModel { get; set; }

    /// <summary>Width component of the restore target AREA (aspect is preserved; this is not an output width). ("Restore Target Width").</summary>
    /// <remarks>Applies to video frames and stills alike. Server range 256–4096. Server default: <c>1280</c>. Does nothing unless <c>restoremodel</c> is set.</remarks>
    [JsonProperty("restoretargetwidth")]
    public int? RestoreTargetWidth { get; set; }

    /// <summary>Height component of the restore target AREA. ("Restore Target Height").</summary>
    /// <remarks>Server range 256–4096. Server default: <c>720</c>. Does nothing unless <c>restoremodel</c> is set.</remarks>
    [JsonProperty("restoretargetheight")]
    public int? RestoreTargetHeight { get; set; }

    /// <summary>Frames per restore chunk (rounded to the model's (n-1)%4==0 contract). ("Restore Clip Frames").</summary>
    /// <remarks>Lower on tight VRAM — fp32 720p-area needs ~5. Ignored for stills. Server range 1–121. Server default: <c>5</c>. Does nothing unless <c>restoremodel</c> is set.</remarks>
    [JsonProperty("restoreclipframes")]
    public int? RestoreClipFrames { get; set; }

    /// <summary>Frame overlap between restore chunks, cross-faded. ("Restore Frame Overlap").</summary>
    /// <remarks>Server range 0–16. Server default: <c>1</c>. Does nothing unless <c>restoremodel</c> is set.</remarks>
    [JsonProperty("restoreframeoverlap")]
    public int? RestoreFrameOverlap { get; set; }

    /// <summary>Restoration strength 0..1. ("Restore Strength").</summary>
    /// <remarks>1.0 = pure model output; lower keeps the input's low-frequency band (guards oversharpening on clean input). Server range 0–1. Server default: <c>1</c>. Does nothing unless <c>restoremodel</c> is set.</remarks>
    [JsonProperty("restorestrength")]
    public float? RestoreStrength { get; set; }

    /// <summary>Override the backend's VRAM mode for this generation. ("VRAM Mode").</summary>
    /// <remarks>The individual levers below still win over whatever this picks. Allowed values: <c>Backend default</c>, <c>Performance</c>, <c>Auto</c>, <c>Balanced</c>, <c>Aggressive</c>, <c>Maximum</c>.</remarks>
    [JsonProperty("vrammode")]
    public string? VramMode { get; set; }

    /// <summary>Stream the denoiser's weights through VRAM a few blocks at a time instead of holding all of them. ("VRAM Weight Streaming").</summary>
    /// <remarks>'On' forces it even when everything would fit (typically 5-8x slower, but it is what makes a large model run on a small card). 'Off' forbids it — an oversized model fails instead. Only models with a block-structured denoiser can honour this. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>On</c>, <c>Off</c>.</remarks>
    [JsonProperty("vramweightstreaming")]
    public string? VramWeightStreaming { get; set; }

    /// <summary>Keep this model's weights on the GPU after the generation finishes, so the next one skips the re-upload. ("VRAM Keep Models Resident").</summary>
    /// <remarks>'Off' frees them every time — slower back-to-back, but it leaves the card free for other work. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>On</c>, <c>Off</c>.</remarks>
    [JsonProperty("vramkeepmodelsresident")]
    public string? VramKeepModelsResident { get; set; }

    /// <summary>Release each stage's weights at its boundary — the text encoder before denoising, the denoiser before the VAE decode — so the next stage gets the space. ("VRAM Phase Unload").</summary>
    /// <remarks>Costs a re-upload per stage; often the difference between fitting and an out-of-VRAM error. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>On</c>, <c>Off</c>.</remarks>
    [JsonProperty("vramphaseunload")]
    public string? VramPhaseUnload { get; set; }

    /// <summary>Storage precision for the caches that persist across steps (KV caches, Wan-Animate's driving cache). ("VRAM Cache Precision").</summary>
    /// <remarks>'Half' roughly halves their size but diverges from the reference maths — it changes the output slightly. 'Full' keeps exact numerics whatever it costs, which is what a parity comparison needs. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>Full</c>, <c>Half</c>.</remarks>
    [JsonProperty("vramcacheprecision")]
    public string? VramCachePrecision { get; set; }

    /// <summary>Page cross-step activations out to system RAM as they are produced. ("VRAM Activation Offload").</summary>
    /// <remarks>Trades a PCIe round trip per step for their full size on the card. A last resort — it is suppressed automatically while a captured CUDA graph is live. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>On</c>, <c>Off</c>.</remarks>
    [JsonProperty("vramactivationoffload")]
    public string? VramActivationOffload { get; set; }

    /// <summary>Release this model's device memory as soon as the generation finishes, rather than leaving it loaded. ("VRAM Free After Generation").</summary>
    /// <remarks>Use when something else needs the card immediately afterwards. Allowed values: <c>Backend default</c>, <c>Auto</c>, <c>On</c>, <c>Off</c>.</remarks>
    [JsonProperty("vramfreeaftergeneration")]
    public string? VramFreeAfterGeneration { get; set; }

    /// <summary>Scales the chunk/tile size a model decodes in. ("VRAM Chunk Scale").</summary>
    /// <remarks>1.0 leaves the model's own default alone; 0.5 halves it. This is the lever for work whose peak is activations rather than weights — VAE decodes, vocoders, 3D grid decodes — where streaming weights does nothing at all. Server range 0.1–1. Server default: <c>1.0</c>.</remarks>
    [JsonProperty("vramchunkscale")]
    public float? VramChunkScale { get; set; }
    #endregion

    #region Image Prompting (via HartsyInference)
    // Moved from the core GenerationRequest: this wire name is registered only by HartsyInference's
    // SwarmUIHartsyInference.cs, sitting alongside ComfyUI's own IP-Adapter params in the UI but not
    // actually one of them server-side.

    /// <summary>Strength of the FaceID-PlusV2 CLIP-face shortcut mix (the official pipeline's 's_scale'). ("FaceID V2 Weight").</summary>
    /// <remarks>Higher = the CLIP appearance of the face crop contributes more on top of the ArcFace identity tokens. Only used with ip-adapter-faceid-plusv2 models; 1.0 is the official default. Server range 0–2. Server default: <c>1</c>. Gated behind the <c>ipadapter</c> feature flag. Does nothing unless <c>useipadapter</c> is set.</remarks>
    [JsonProperty("faceidvweight")]
    public float? FaceIDV2Weight { get; set; }

    #endregion
}
