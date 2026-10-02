using Newtonsoft.Json;
using SwarmUI.ApiClient.Contracts.Requests;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Generation parameters registered by the AudioLab server extension.</summary>
/// <remarks>
/// <para><b>Coverage: all 172 parameters AudioLab registers server-side are modeled here.</b></para>
/// <para>Attached to a request via <see cref="GenerationRequestExtensionParams.AudioLab"/>
/// (<c>request.Extensions.AudioLab</c>). Every property here still serializes under its own top-level wire name,
/// flattened into the same generation payload as every core parameter -- this type exists to keep AudioLab's
/// parameters off <see cref="GenerationRequest"/> itself, not to change the wire format.</para>
/// <para><b>ACE-Step, YuE2 and MiniMax Music 3 read lyrics from <see cref="GenerationRequest.Prompt"/> and genre
/// from <see cref="GenerationRequest.Text2AudioStyle"/>.</b> AudioLab dropped its own lyrics parameters for those
/// three on 2026-09-16 to match core and the engine, which have always read it that way. YuE v1 and HeartMuLa keep
/// theirs (<see cref="YuELyrics"/>, <see cref="HeartLibLyrics"/>) and are unaffected.</para>
/// <para>Only send parameters advertised by the selected model's feature flags. Each music provider reads its
/// own set and nothing from another's: ACE-Step (<c>acestep_music_params</c>, <c>acestep_cfg_params</c>,
/// <c>acestep_lm_params</c>, <c>acestep_task_params</c>), Stable Audio (<c>stableaudio_music_params</c>), YuE2
/// (<c>yue2_music_params</c>), YuE v1 (<c>yue_music_params</c>), HeartMuLa (<c>heartlib_music_params</c>),
/// MiniMax Music 3 (<c>minimax_music3_params</c>), AudioCraft (<c>audiocraft_sampling</c>). YuE2's wire names read
/// <c>song*</c> for the pass that renders audio and <c>score*</c> for the pass that plans an ABC score, rather than
/// carrying a YuE2 prefix, because SwarmUI strips digits from parameter ids and the prefix would collide with YuE v1.</para>
/// </remarks>
public class AudioLabGenerationParams
{
    /// <summary>Container for the returned audio ("Audio Output Format"): wav_16, wav_32, flac, mp3, ogg.</summary>
    [JsonProperty("audiooutputformat")]
    public string? AudioOutputFormat { get; set; }

    /// <summary>Encoding quality for the returned audio ("Audio Quality"): low, medium, high, max.</summary>
    [JsonProperty("audioquality")]
    public string? AudioQuality { get; set; }

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

    /// <summary>Denoising steps for Stable Audio models ("Stable Audio Steps"). Server range 1–100.</summary>
    [JsonProperty("stableaudiosteps")]
    public int? StableAudioSteps { get; set; }

    /// <summary>Timestep shift factor ("Shift"); 0 uses the checkpoint's own default. Server range 0–5.</summary>
    /// <remarks>Upstream recommends 3.0 for turbo checkpoints and it is not auto-corrected.</remarks>
    [JsonProperty("shift")]
    public float? AceShift { get; set; }

    /// <summary>ODE solver for ACE-Step ("Infer Method"): ode (deterministic) or sde (stochastic).</summary>
    [JsonProperty("infermethod")]
    public string? AceInferMethod { get; set; }

    /// <summary>Adaptive Diffusion Guidance ("Use ADG"). Registered as a string dropdown: "true" / "false".</summary>
    [JsonProperty("useadg")]
    public string? AceUseAdg { get; set; }

    /// <summary>Fraction of denoising at which CFG starts ("CFG Interval Start"). Server range 0–1.</summary>
    /// <remarks>Not offered for turbo checkpoints, which run without CFG.</remarks>
    [JsonProperty("cfgintervalstart")]
    public float? AceCfgIntervalStart { get; set; }

    /// <summary>Fraction of denoising at which CFG stops ("CFG Interval End"). Server range 0–1.</summary>
    [JsonProperty("cfgintervalend")]
    public float? AceCfgIntervalEnd { get; set; }

    /// <summary>Qwen3 planner that writes structured music metadata ("ACE LM Model"): none, 0.6B, 1.7B, 4B.</summary>
    [JsonProperty("acelmmodel")]
    public string? AceLmModel { get; set; }

    /// <summary>Chain-of-thought reasoning in the planner ("LM Thinking"). String dropdown: "true" / "false".</summary>
    [JsonProperty("lmthinking")]
    public string? AceLmThinking { get; set; }

    /// <summary>Sampling temperature for the planner ("LM Temperature"). Server range 0–2.</summary>
    [JsonProperty("lmtemperature")]
    public float? AceLmTemperature { get; set; }

    /// <summary>Guidance scale for the planner ("LM CFG Scale"). Server range 1–5.</summary>
    [JsonProperty("lmcfgscale")]
    public float? AceLmCfgScale { get; set; }

    /// <summary>Top-K for the planner ("LM Top K"); 0 disables it. Server range 0–500.</summary>
    [JsonProperty("lmtopk")]
    public int? AceLmTopK { get; set; }

    /// <summary>Nucleus threshold for the planner ("LM Top P"). Server range 0–1.</summary>
    [JsonProperty("lmtopp")]
    public float? AceLmTopP { get; set; }

    /// <summary>Characteristics the planner should avoid ("LM Negative Prompt").</summary>
    [JsonProperty("lmnegativeprompt")]
    public string? AceLmNegativePrompt { get; set; }

    /// <summary>Include genre/mood/instrument tags in the chain of thought ("CoT Metas"): "true" / "false".</summary>
    [JsonProperty("cotmetas")]
    public string? AceCotMetas { get; set; }

    /// <summary>Include a music description caption in the chain of thought ("CoT Caption"): "true" / "false".</summary>
    [JsonProperty("cotcaption")]
    public string? AceCotCaption { get; set; }

    /// <summary>Include language detection in the chain of thought ("CoT Language"): "true" / "false".</summary>
    [JsonProperty("cotlanguage")]
    public string? AceCotLanguage { get; set; }

    /// <summary>What ACE-Step should do ("Task Type"): text2music, cover, repaint, complete.</summary>
    /// <remarks>Everything except text2music requires <see cref="AceSourceAudio"/>.</remarks>
    [JsonProperty("tasktype")]
    public string? AceTaskType { get; set; }

    /// <summary>Input audio for the cover, repaint, and complete tasks ("ACE Source Audio"). Data URL or server path.</summary>
    [JsonProperty("acesourceaudio")]
    public string? AceSourceAudio { get; set; }

    /// <summary>Optional style/timbre reference ("Style Reference Audio"). Data URL or server path.</summary>
    [JsonProperty("stylereferenceaudio")]
    public string? AceStyleReferenceAudio { get; set; }

    /// <summary>Where the repainted section starts, in seconds ("Repaint Start"). Server range 0–600.</summary>
    [JsonProperty("repaintstart")]
    public float? AceRepaintStart { get; set; }

    /// <summary>Where the repainted section ends, in seconds ("Repaint End"); -1 repaints to the end. Server range -1–600.</summary>
    [JsonProperty("repaintend")]
    public float? AceRepaintEnd { get; set; }

    /// <summary>Style transfer strength for the cover task ("Cover Strength"); 1.0 is full transfer. Server range 0–1.</summary>
    [JsonProperty("coverstrength")]
    public float? AceCoverStrength { get; set; }

    /// <summary>Noise injected into the cover task for variation ("Cover Noise"). Server range 0–1.</summary>
    [JsonProperty("covernoise")]
    public float? AceCoverNoise { get; set; }

    /// <summary>How much YuE2 plans before rendering ("Score Planning Mode"): full, melody, off.</summary>
    [JsonProperty("scoreplanningmode")]
    public string? Yue2ScorePlanningMode { get; set; }

    /// <summary>An ABC score to render verbatim ("Song Score (ABC)"), skipping the planning pass.</summary>
    /// <remarks>Ignored when <see cref="Yue2ScorePlanningMode"/> is off. Generating once with planning on and
    /// reading the score back out of the result metadata gives you something to edit and feed in here.</remarks>
    [JsonProperty("songscoreabc")]
    public string? Yue2Score { get; set; }

    /// <summary>Classifier-free guidance for the audio pass ("Song Guidance"). Server range 1–3.</summary>
    /// <remarks>YuE2 runs at or just above 1.0; higher values distort rather than sharpen. Above 1.0 the model
    /// prefills a second cache, which doubles the cost, and if that branch is the longer of the two it is what
    /// bounds how much song fits in the context.</remarks>
    [JsonProperty("songguidance")]
    public float? Yue2Guidance { get; set; }

    /// <summary>Flow-matching ODE steps for the acoustic stack ("Acoustic Steps"). Server range 8–128.</summary>
    [JsonProperty("acousticsteps")]
    public int? Yue2AcousticSteps { get; set; }

    /// <summary>Sampling temperature for the codec-token pass ("Song Temperature"). Server range 0.1–2.</summary>
    [JsonProperty("songtemperature")]
    public float? Yue2Temperature { get; set; }

    /// <summary>Nucleus threshold for the codec-token pass ("Song Top P"). Server range 0.01–1.</summary>
    [JsonProperty("songtopp")]
    public float? Yue2TopP { get; set; }

    /// <summary>Top-K for the codec-token pass ("Song Top K"); 0 disables it. Server range 0–1000.</summary>
    [JsonProperty("songtopk")]
    public int? Yue2TopK { get; set; }

    /// <summary>Repetition penalty for the codec-token pass ("Song Repetition Penalty"). Server range 1–2.</summary>
    [JsonProperty("songrepetitionpenalty")]
    public float? Yue2RepetitionPenalty { get; set; }

    /// <summary>How many recent tokens that penalty looks back over ("Song Penalty Window"). Server range 1–100.</summary>
    /// <remarks>The model rejects anything outside 1–100 — out-of-range values fail the generation.</remarks>
    [JsonProperty("songpenaltywindow")]
    public int? Yue2PenaltyWindow { get; set; }

    /// <summary>Tokens the song must produce before it may end ("Song Minimum Tokens"), at 25 per second of audio.</summary>
    /// <remarks>Server range 0–22500. Clamped down when the requested duration is shorter. Forcing this high on
    /// short lyrics does not lengthen the song — it appends silence once the music ends.</remarks>
    [JsonProperty("songminimumtokens")]
    public int? Yue2MinTokens { get; set; }

    /// <summary>Sampling temperature for the score planner ("Score Temperature"). Server range 0.1–2.</summary>
    [JsonProperty("scoretemperature")]
    public float? Yue2ScoreTemperature { get; set; }

    /// <summary>Nucleus threshold for the score planner ("Score Top P"). Server range 0.01–1.</summary>
    [JsonProperty("scoretopp")]
    public float? Yue2ScoreTopP { get; set; }

    /// <summary>Top-K for the score planner ("Score Top K"); 0 disables it. Server range 0–1000.</summary>
    [JsonProperty("scoretopk")]
    public int? Yue2ScoreTopK { get; set; }

    /// <summary>Repetition penalty for the score planner ("Score Repetition Penalty"). Server range 1–2.</summary>
    [JsonProperty("scorerepetitionpenalty")]
    public float? Yue2ScoreRepetitionPenalty { get; set; }

    /// <summary>Token ceiling for the score planner ("Score Max Tokens"). Server range 256–16384.</summary>
    /// <remarks>A score that hits the ceiling is reported as truncated in the result metadata.</remarks>
    [JsonProperty("scoremaxtokens")]
    public int? Yue2ScoreMaxTokens { get; set; }

    /// <summary>How many recent tokens the planner's penalty looks back over ("Score Penalty Window"). Server range 1–100.</summary>
    [JsonProperty("scorepenaltywindow")]
    public int? Yue2ScorePenaltyWindow { get; set; }

    /// <summary>Lyrics for YuE ("YuE Lyrics"). Required; each section marker becomes its own generated segment.</summary>
    [JsonProperty("yuelyrics")]
    public string? YuELyrics { get; set; }

    /// <summary>Stage-1 token ceiling ("Max Tokens"); roughly 3000 per 30 seconds. Server range 1000–12000.</summary>
    [JsonProperty("maxtokens")]
    public int? YuEMaxTokens { get; set; }

    /// <summary>Weight quantization for the Stage-1 LM ("Quantization"): fp16, 8bit, 4bit.</summary>
    /// <remarks>An engine feature, not an upstream YuE flag.</remarks>
    [JsonProperty("quantization")]
    public string? YuEQuantization { get; set; }

    /// <summary>Batch size for Stage-2 refinement ("Stage-2 Batch Size"). Server range 1–32.</summary>
    [JsonProperty("stagebatchsize")]
    public int? YuEStage2BatchSize { get; set; }

    /// <summary>Sampling temperature for YuE ("YuE Temperature"). Server range 0.1–2.</summary>
    [JsonProperty("yuetemperature")]
    public float? YuETemperature { get; set; }

    /// <summary>Nucleus threshold for YuE ("YuE Top P"). Server range 0–1.</summary>
    [JsonProperty("yuetopp")]
    public float? YuETopP { get; set; }

    /// <summary>Repetition penalty for YuE ("YuE Repetition Penalty"). Server range 1–2.</summary>
    [JsonProperty("yuerepetitionpenalty")]
    public float? YuERepetitionPenalty { get; set; }

    /// <summary>How many lyric segments to generate ("Segments"); 0 generates every segment. Server range 0–10.</summary>
    [JsonProperty("segments")]
    public int? YuESegments { get; set; }

    /// <summary>Lyrics for HeartMuLa ("HeartLib Lyrics"). Only [Intro] [Verse] [Prechorus] [Chorus] [Bridge] [Outro] are recognized.</summary>
    [JsonProperty("heartliblyrics")]
    public string? HeartLibLyrics { get; set; }

    /// <summary>Guidance strength ("HeartLib CFG Scale"); upstream uses 1.5. Server range 0.1–10.</summary>
    [JsonProperty("heartlibcfgscale")]
    public float? HeartLibCfgScale { get; set; }

    /// <summary>Sampling temperature ("HeartLib Temperature"). Server range 0.1–2.</summary>
    [JsonProperty("heartlibtemperature")]
    public float? HeartLibTemperature { get; set; }

    /// <summary>Top-K token limit ("HeartLib Top K"). Server range 1–500.</summary>
    [JsonProperty("heartlibtopk")]
    public int? HeartLibTopK { get; set; }

    /// <summary>Flow-matching guidance strength ("MiniMax Music 3 CFG Scale"); the reference recipe uses 1.7. Server range 0.1–10.</summary>
    [JsonProperty("minimaxmusiccfgscale")]
    public float? MiniMaxMusic3CfgScale { get; set; }

    /// <summary>Euler steps per 200-frame window ("MiniMax Music 3 Steps"); the reference recipe uses 30. Server range 4–100.</summary>
    [JsonProperty("minimaxmusicsteps")]
    public int? MiniMaxMusic3Steps { get; set; }

    /// <summary>Maximum output length in seconds ("Max Duration"). Server range 1–900.</summary>
    /// <remarks>Shared by every AudioLab generation provider, not just AudioCraft. It is a ceiling, not a target:
    /// a model that finishes early returns the shorter clip. The server prefers the stock
    /// <see cref="GenerationRequest.Text2AudioDuration"/> when that is set and falls back to this one, so set one
    /// or the other rather than both. YuE2 clamps further per request — a long prompt and score leave less room
    /// inside the model's context, and the budget actually used comes back in the result metadata.</remarks>
    [JsonProperty("maxduration")]
    public float? MaxDuration { get; set; }

    // --- AudioCraft sampling (MusicGen, AudioGen) ---

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

    // --- Sound effects ---
    /// <summary>Requested effect length in seconds ("SFX Duration"); 0 lets the model choose. Server range 0–30.</summary>
    [JsonProperty("sfxduration")]
    public float? SfxDuration { get; set; }

    /// <summary>How literally the effect follows the prompt ("SFX Prompt Influence"). Server range 0–1.</summary>
    [JsonProperty("sfxpromptinfluence")]
    public float? SfxPromptInfluence { get; set; }

    // --- Speech: TTS voice reference, transcription input, voice conversion ---
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

    // The regions below close the coverage gap noted in the class remarks above: generic TTS sampling knobs
    // and every per-provider TTS/STT parameter AudioLab registers, grouped the same way the server's own
    // AudioLabParams.cs groups them (one #region per provider, matching its feature flag).

    #region TTS Shared (flag: audiolab_tts)
    /// <summary>Output volume multiplier ("Volume"). 1.0 = full volume, 0.5 = half volume.</summary>
    /// <remarks>Server range 0.1–1. Server default: <c>0.8</c>. Gated behind the <c>audiolab_tts</c> feature flag.</remarks>
    [JsonProperty("volume")]
    public double? Volume { get; set; }

    /// <summary>How to split text for streaming audio generation ("Stream Chunk Size").</summary>
    /// <remarks>Smaller chunks = faster first audio. Larger chunks = better quality per chunk. Per Sentence is
    /// recommended for most models. Each chunk plays immediately while the next generates. Allowed values:
    /// <c>off</c> (full text), <c>word</c>, <c>phrase</c> (~5 words), <c>sentence</c>, <c>paragraph</c>. Server
    /// default: <c>off</c>. Gated behind the <c>audiolab_tts</c> feature flag.</remarks>
    [JsonProperty("streamchunksize")]
    public string? StreamChunkSize { get; set; }
    #endregion

    #region TTS Shared Sampling (flag: tts_sampling)
    /// <summary>Sampling temperature ("Temperature"). Higher = more varied/creative speech, lower = more consistent.</summary>
    /// <remarks>Server range 0.1–2. Server default: <c>0.8</c>. Gated behind the <c>tts_sampling</c> feature flag.</remarks>
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }

    /// <summary>Nucleus sampling threshold ("Top P"). 1.0 = no filtering; lower restricts to higher-probability tokens.</summary>
    /// <remarks>Server range 0–1. Server default: <c>1.0</c>. Gated behind the <c>tts_sampling</c> feature flag.</remarks>
    [JsonProperty("topp")]
    public double? TopP { get; set; }

    /// <summary>Penalizes repeated tokens ("Repetition Penalty"). Higher values reduce stuttering and repetitive speech.</summary>
    /// <remarks>Server range 1–2. Server default: <c>1.2</c>. Gated behind the <c>tts_sampling</c> feature flag.</remarks>
    [JsonProperty("repetitionpenalty")]
    public double? RepetitionPenalty { get; set; }

    /// <summary>Top-K token sampling ("Top K"). Limits sampling to the K most likely tokens; 0 disables it.</summary>
    /// <remarks>Server range 0–1000. Server default: <c>50</c>. Gated behind the <c>tts_sampling</c> feature flag.</remarks>
    [JsonProperty("topk")]
    public int? TopK { get; set; }

    /// <summary>Minimum probability threshold ("Min P"). Tokens below this probability are excluded from sampling.</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.05</c>. Gated behind the <c>tts_sampling</c> feature flag.</remarks>
    [JsonProperty("minp")]
    public double? MinP { get; set; }
    #endregion

    #region TTS — Bark (flag: bark_tts_params)
    /// <summary>Voice preset for Bark TTS ("Bark Voice"). 'Random' generates a random voice.</summary>
    /// <remarks>Dropdown of 17 preset speakers (English 0-9, Chinese, German, French, Japanese, Korean, plus
    /// Random). Server default: <c>v2/en_speaker_6</c>. Gated behind the <c>bark_tts_params</c> feature flag.</remarks>
    [JsonProperty("barkvoice")]
    public string? BarkVoice { get; set; }

    /// <summary>Controls randomness of text token generation ("Text Temperature"). Higher = more varied speech patterns.</summary>
    /// <remarks>Server range 0–2. Server default: <c>0.7</c>. Gated behind the <c>bark_tts_params</c> feature flag.</remarks>
    [JsonProperty("texttemperature")]
    public double? BarkTextTemperature { get; set; }

    /// <summary>Controls randomness of audio waveform generation ("Waveform Temperature"). Higher = more varied audio quality.</summary>
    /// <remarks>Server range 0–2. Server default: <c>0.7</c>. Gated behind the <c>bark_tts_params</c> feature flag.</remarks>
    [JsonProperty("waveformtemperature")]
    public double? BarkWaveformTemperature { get; set; }
    #endregion

    #region TTS — Chatterbox (flag: chatterbox_tts_params)
    /// <summary>Voice expressiveness level ("Exaggeration"). Higher values produce more animated, expressive speech.</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>chatterbox_tts_params</c> feature flag.</remarks>
    [JsonProperty("exaggeration")]
    public double? ChatterboxExaggeration { get; set; }

    /// <summary>Classifier-free guidance weight ("CFG Weight"). Higher = more controlled/stable, lower = more variation.</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>chatterbox_tts_params</c> feature flag.</remarks>
    [JsonProperty("cfgweight")]
    public double? ChatterboxCfgWeight { get; set; }
    #endregion

    #region TTS — Kokoro (flag: kokoro_tts_params)
    /// <summary>Voice to synthesize with ("Kokoro Voice"). The first letter is the language (a=American, b=British,
    /// j=Japanese, z=Mandarin, e=Spanish, f=French, h=Hindi, i=Italian, p=Portuguese); the second is f=female / m=male.</summary>
    /// <remarks>All 54 official voices from the model card are listed. Server default: <c>af_heart</c>. Gated
    /// behind the <c>kokoro_tts_params</c> feature flag.</remarks>
    [JsonProperty("kokorovoice")]
    public string? KokoroVoice { get; set; }

    /// <summary>Speech speed multiplier ("Kokoro Speed"). 1.0 = normal, 0.5 = half, 2.0 = double.</summary>
    /// <remarks>Server range 0.25–4. Server default: <c>1.0</c>. Gated behind the <c>kokoro_tts_params</c> feature flag.</remarks>
    [JsonProperty("kokorospeed")]
    public double? KokoroSpeed { get; set; }
    #endregion

    #region TTS — Piper (flag: piper_tts_params)
    /// <summary>Piper voice ("Piper Voice"). Each voice is a separate download, named language-speaker-quality.</summary>
    /// <remarks>All 37 English voices from the official VOICES.md are listed; higher quality is larger and
    /// slower. Server default: <c>en_US-amy-medium</c>. Gated behind the <c>piper_tts_params</c> feature flag.</remarks>
    [JsonProperty("pipervoice")]
    public string? PiperVoice { get; set; }

    /// <summary>Speech speed multiplier ("Piper Speed"). 1.0 = normal, 0.5 = half, 2.0 = double.</summary>
    /// <remarks>Server range 0.25–4. Server default: <c>1.0</c>. Gated behind the <c>piper_tts_params</c> feature flag.</remarks>
    [JsonProperty("piperspeed")]
    public double? PiperSpeed { get; set; }
    #endregion

    #region TTS — Orpheus (flag: orpheus_tts_params)
    /// <summary>Voice preset for Orpheus TTS ("Orpheus Voice").</summary>
    /// <remarks>Supports emotion tags: <c>&lt;laugh&gt;</c>, <c>&lt;chuckle&gt;</c>, <c>&lt;sigh&gt;</c>,
    /// <c>&lt;cough&gt;</c>, <c>&lt;sniffle&gt;</c>, <c>&lt;groan&gt;</c>, <c>&lt;yawn&gt;</c>, <c>&lt;gasp&gt;</c>.
    /// Allowed values: <c>tara</c>, <c>leah</c>, <c>jess</c>, <c>leo</c>, <c>dan</c>, <c>mia</c>, <c>zac</c>,
    /// <c>zoe</c>. Server default: <c>tara</c>. Gated behind the <c>orpheus_tts_params</c> feature flag.</remarks>
    [JsonProperty("orpheusvoice")]
    public string? OrpheusVoice { get; set; }
    #endregion

    #region TTS — CSM (flag: csm_tts_params)
    /// <summary>Speaker ID for multi-speaker conversation ("Speaker"). 0 = primary speaker.</summary>
    /// <remarks>Server default: <c>0</c>. Gated behind the <c>csm_tts_params</c> feature flag.</remarks>
    [JsonProperty("speaker")]
    public string? CsmSpeaker { get; set; }
    #endregion

    #region TTS — VibeVoice (flag: vibevoice_tts_params)
    /// <summary>Number of DDPM denoising steps ("Diffusion Steps"). More steps = higher quality but slower; 10 is recommended.</summary>
    /// <remarks>Server range 5–100. Server default: <c>10</c>. Gated behind the <c>vibevoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("diffusionsteps")]
    public int? VibeVoiceDiffusionSteps { get; set; }

    /// <summary>Classifier-free guidance scale for speech diffusion ("VibeVoice CFG").</summary>
    /// <remarks>1.3 is recommended for standard models, 1.5 for streaming. Server range 0–5. Server default:
    /// <c>1.3</c>. Gated behind the <c>vibevoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("vibevoicecfg")]
    public double? VibeVoiceCfg { get; set; }
    #endregion

    #region TTS — Dia (flag: dia_tts_params)
    /// <summary>Top-K filtering for classifier-free guidance ("CFG Filter Top K"); Dia's <c>cfg_filter_top_k</c>.</summary>
    /// <remarks>Upstream default is 45. Server range 0–500. Server default: <c>45</c>. Gated behind the
    /// <c>dia_tts_params</c> feature flag.</remarks>
    [JsonProperty("cfgfiltertopk")]
    public int? DiaCfgFilterTopK { get; set; }

    /// <summary>Classifier-free guidance strength for Dia ("Dia CFG Scale"). Higher = closer to the prompt, lower = more natural variation.</summary>
    /// <remarks>Server range 1–10. Server default: <c>3.0</c>. Gated behind the <c>dia_tts_params</c> feature flag.</remarks>
    [JsonProperty("diacfgscale")]
    public double? DiaCfgScale { get; set; }
    #endregion

    #region TTS — F5-TTS (flag: f5_tts_params)
    /// <summary>Number of function evaluation steps for flow matching ("NFE Steps"). More steps = higher quality but slower.</summary>
    /// <remarks>Server range 1–100. Server default: <c>32</c>. Gated behind the <c>f5_tts_params</c> feature flag.</remarks>
    [JsonProperty("nfesteps")]
    public int? F5NfeSteps { get; set; }

    /// <summary>Speech speed multiplier ("Speed") for F5-TTS. 1.0 = normal, 0.5 = half speed, 2.0 = double speed.</summary>
    /// <remarks>Server range 0.25–4. Server default: <c>1.0</c>. Gated behind the <c>f5_tts_params</c> feature flag.</remarks>
    [JsonProperty("speed")]
    public double? F5Speed { get; set; }

    /// <summary>Classifier-free guidance for flow matching ("F5 CFG"). 2.0 is recommended; higher = stronger prompt adherence.</summary>
    /// <remarks>Server range 0–10. Server default: <c>2.0</c>. Gated behind the <c>f5_tts_params</c> feature flag.</remarks>
    [JsonProperty("fcfg")]
    public double? F5Cfg { get; set; }

    /// <summary>Sway-sampling coefficient ("F5 Sway Sampling").</summary>
    /// <remarks>Negative values bias sampling toward earlier flow steps; -1.0 is the upstream default. Server
    /// range -1–1. Server default: <c>-1.0</c>. Gated behind the <c>f5_tts_params</c> feature flag.</remarks>
    [JsonProperty("fswaysampling")]
    public double? F5SwaySampling { get; set; }
    #endregion

    #region TTS — ZipVoice (flag: zipvoice_tts_params)
    /// <summary>Flow-matching sampling steps ("ZipVoice Steps"). Upstream default is 8; the distill model can go as low as 4.</summary>
    /// <remarks>Server range 1–100. Server default: <c>8</c>. Gated behind the <c>zipvoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("zipvoicesteps")]
    public int? ZipVoiceSteps { get; set; }

    /// <summary>Speech speed multiplier ("ZipVoice Speed"). 1.0 = normal, 0.5 = half speed, 2.0 = double speed.</summary>
    /// <remarks>Server range 0.25–4. Server default: <c>1.0</c>. Gated behind the <c>zipvoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("zipvoicespeed")]
    public double? ZipVoiceSpeed { get; set; }

    /// <summary>Classifier-free guidance for flow matching ("ZipVoice CFG"). 1.0 is the base checkpoint's default.</summary>
    /// <remarks>Higher = stronger prompt adherence. Server range 0–10. Server default: <c>1.0</c>. Gated behind
    /// the <c>zipvoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("zipvoicecfg")]
    public double? ZipVoiceCfg { get; set; }
    #endregion

    #region TTS — Zonos (flag: zonos_tts_params)
    /// <summary>Language for Zonos TTS synthesis ("Zonos Language").</summary>
    /// <remarks>Allowed values: <c>en-us</c>, <c>en-gb</c>, <c>es</c>, <c>fr</c>, <c>de</c>, <c>it</c>, <c>pt</c>,
    /// <c>ja</c>, <c>zh</c>, <c>ko</c>. Server default: <c>en-us</c>. Gated behind the <c>zonos_tts_params</c>
    /// feature flag.</remarks>
    [JsonProperty("zonoslanguage")]
    public string? ZonosLanguage { get; set; }

    /// <summary>Emotion preset ("Emotion") for Zonos.</summary>
    /// <remarks>Zonos conditions on an 8-way vector (Happiness, Sadness, Disgust, Fear, Surprise, Anger, Other,
    /// Neutral); each preset weights that vector, which is then renormalized to sum 1. Allowed values:
    /// <c>neutral</c>, <c>happy</c>, <c>sad</c>, <c>angry</c>, <c>fearful</c>, <c>surprised</c>,
    /// <c>disgusted</c>. Server default: <c>neutral</c>. Gated behind the <c>zonos_tts_params</c> feature flag.</remarks>
    [JsonProperty("emotion")]
    public string? ZonosEmotion { get; set; }

    /// <summary>Speaking rate in phonemes per second ("Speaking Rate") for Zonos.</summary>
    /// <remarks>Reference range is 0-40: 15 is the default, 30 is very fast, 10 is slow. Server range 0–40.
    /// Server default: <c>15.0</c>. Gated behind the <c>zonos_tts_params</c> feature flag.</remarks>
    [JsonProperty("speakingrate")]
    public double? ZonosSpeakingRate { get; set; }

    /// <summary>Pitch standard deviation ("Pitch Variation") for Zonos.</summary>
    /// <remarks>Reference guidance: 20-45 for normal speech, 60-150 for expressive delivery. Server range 0–400.
    /// Server default: <c>20.0</c>. Gated behind the <c>zonos_tts_params</c> feature flag.</remarks>
    [JsonProperty("pitchvariation")]
    public double? ZonosPitchVariation { get; set; }
    #endregion

    #region TTS — Fish Speech (flag: fishspeech_tts_params)
    /// <summary>Cap on generated tokens ("FishSpeech Max Tokens"). Upstream default is 0, meaning generate until the stop token.</summary>
    /// <remarks>Server range 0–4096. Server default: <c>0</c>. Gated behind the <c>fishspeech_tts_params</c> feature flag.</remarks>
    [JsonProperty("fishspeechmaxtokens")]
    public int? FishSpeechMaxTokens { get; set; }

    /// <summary>Text chunk size for long-form synthesis ("FishSpeech Chunk Length"); upstream default 300.</summary>
    /// <remarks>NOTE: the in-process engine does not chunk text yet, so this currently has no effect. Server
    /// range 100–1000. Server default: <c>300</c>. Gated behind the <c>fishspeech_tts_params</c> feature flag.</remarks>
    [JsonProperty("fishspeechchunklength")]
    public int? FishSpeechChunkLength { get; set; }

    /// <summary>Normalize text before synthesis ("FishSpeech Normalize"). Improves handling of numbers, abbreviations, and special characters.</summary>
    /// <remarks>Registered as a string dropdown: "true" / "false". Server default: <c>true</c>. Gated behind
    /// the <c>fishspeech_tts_params</c> feature flag.</remarks>
    [JsonProperty("fishspeechnormalize")]
    public string? FishSpeechNormalize { get; set; }
    #endregion

    #region TTS — Qwen3-TTS (flags: qwen3tts_tts_params, qwen3tts_speaker_params, qwen3tts_instruct_params)
    /// <summary>Language for Qwen3-TTS synthesis ("Qwen3 Language"). 'Auto' lets the model detect automatically.</summary>
    /// <remarks>Allowed values: <c>Auto</c>, <c>Chinese</c>, <c>English</c>, <c>Japanese</c>, <c>Korean</c>,
    /// <c>German</c>, <c>French</c>, <c>Russian</c>, <c>Portuguese</c>, <c>Spanish</c>, <c>Italian</c>. Server
    /// default: <c>Auto</c>. Gated behind the <c>qwen3tts_tts_params</c> feature flag.</remarks>
    [JsonProperty("qwenlanguage")]
    public string? Qwen3Language { get; set; }

    /// <summary>Built-in CustomVoice speaker ("Qwen3 Speaker").</summary>
    /// <remarks>Four are confirmed against the checkpoint (Ryan, Serena, Ono_Anna, Sohee); the others fall back
    /// to the default voice until their ids are verified. Server default: <c>Ryan</c>. Gated behind the
    /// <c>qwen3tts_speaker_params</c> feature flag.</remarks>
    [JsonProperty("qwenspeaker")]
    public string? Qwen3Speaker { get; set; }

    /// <summary>Natural language instruction for voice control ("Qwen3 Instruct").</summary>
    /// <remarks>CustomVoice: describe emotion/style (e.g. 'Speak with excitement'). VoiceDesign: describe the
    /// voice (e.g. 'A deep male voice with a British accent'). Ignored for Base models. Gated behind the
    /// <c>qwen3tts_instruct_params</c> feature flag.</remarks>
    [JsonProperty("qweninstruct")]
    public string? Qwen3Instruct { get; set; }

    /// <summary>Condition on the speaker vector alone instead of the full reference encoding ("Qwen3 X-Vector Only").</summary>
    /// <remarks>Faster, with less prosody transfer. Clone (Base) models only. Registered as a string dropdown:
    /// "false" / "true". Server default: <c>false</c>. Gated behind the <c>qwen3tts_tts_params</c> feature flag.</remarks>
    [JsonProperty("qwenxvectoronly")]
    public string? Qwen3XVectorOnly { get; set; }
    #endregion

    #region TTS — MeloTTS (flag: melotts_tts_params)
    /// <summary>Speaker/accent within the selected MeloTTS checkpoint ("MeloTTS Speaker").</summary>
    /// <remarks>English ships EN-US, EN-BR, EN-AU, EN-Default and EN-India; other languages have a single
    /// speaker. Server default: <c>EN-US</c>. Gated behind the <c>melotts_tts_params</c> feature flag.</remarks>
    [JsonProperty("melottsspeaker")]
    public string? MeloTtsSpeaker { get; set; }

    /// <summary>Speech speed multiplier ("MeloTTS Speed").</summary>
    /// <remarks>Server range 0.5–2. Server default: <c>1.0</c>. Gated behind the <c>melotts_tts_params</c> feature flag.</remarks>
    [JsonProperty("melottsspeed")]
    public double? MeloTtsSpeed { get; set; }
    #endregion

    #region TTS — StyleTTS 2 (flags: styletts2_tts_params, styletts2_clone_params)
    /// <summary>Style-diffusion sampler steps ("StyleTTS2 Diffusion Steps"). Upstream inference uses 5.</summary>
    /// <remarks>More steps trade speed for style stability. Server range 1–100. Server default: <c>5</c>.
    /// Gated behind the <c>styletts2_tts_params</c> feature flag.</remarks>
    [JsonProperty("stylettsdiffusionsteps")]
    public int? StyleTts2DiffusionSteps { get; set; }

    /// <summary>Classifier-free-style guidance on the text embedding ("StyleTTS2 Embedding Scale"). Higher = more emotive delivery.</summary>
    /// <remarks>This is NOT the alpha/beta reference blend. Server range 0.5–5. Server default: <c>1.0</c>.
    /// Gated behind the <c>styletts2_tts_params</c> feature flag.</remarks>
    [JsonProperty("stylettsembeddingscale")]
    public double? StyleTts2EmbeddingScale { get; set; }

    /// <summary>Timbre balance between the reference clip and the sampled style ("StyleTTS2 Alpha (Timbre)").</summary>
    /// <remarks>Upstream: 0 matches the reference deterministically, 1 is maximum diversity and least
    /// similarity. Default 0.3. Server range 0–1. Server default: <c>0.3</c>. Gated behind the
    /// <c>styletts2_clone_params</c> feature flag.</remarks>
    [JsonProperty("stylettsalphatimbre")]
    public double? StyleTts2AlphaTimbre { get; set; }

    /// <summary>Prosody balance between the reference clip and the sampled style ("StyleTTS2 Beta (Prosody)").</summary>
    /// <remarks>Upstream: 0 matches the reference deterministically, 1 is maximum diversity. Default 0.7.
    /// Server range 0–1. Server default: <c>0.7</c>. Gated behind the <c>styletts2_clone_params</c> feature flag.</remarks>
    [JsonProperty("stylettsbetaprosody")]
    public double? StyleTts2BetaProsody { get; set; }
    #endregion

    #region TTS — Spark-TTS (flag: sparktts_create_params)
    /// <summary>Voice gender for Spark-TTS voice creation ("Spark Voice Gender").</summary>
    /// <remarks>Ignored when a reference clip is supplied (that switches it to cloning). Allowed values:
    /// <c>female</c>, <c>male</c>. Server default: <c>female</c>. Gated behind the
    /// <c>sparktts_create_params</c> feature flag.</remarks>
    [JsonProperty("sparkvoicegender")]
    public string? SparkVoiceGender { get; set; }

    /// <summary>Pitch level for Spark-TTS voice creation ("Spark Pitch").</summary>
    /// <remarks>Allowed values: <c>very_low</c>, <c>low</c>, <c>moderate</c>, <c>high</c>, <c>very_high</c>.
    /// Server default: <c>moderate</c>. Gated behind the <c>sparktts_create_params</c> feature flag.</remarks>
    [JsonProperty("sparkpitch")]
    public string? SparkPitch { get; set; }

    /// <summary>Speed level for Spark-TTS voice creation ("Spark Speed").</summary>
    /// <remarks>Allowed values: <c>very_low</c>, <c>low</c>, <c>moderate</c>, <c>high</c>, <c>very_high</c>.
    /// Server default: <c>moderate</c>. Gated behind the <c>sparktts_create_params</c> feature flag.</remarks>
    [JsonProperty("sparkspeed")]
    public string? SparkSpeed { get; set; }
    #endregion

    #region TTS — CosyVoice (flag: cosyvoice_tts_params)
    /// <summary>Preset speaker ("CosyVoice Voice"). These presets belong to the CosyVoice-300M-SFT checkpoint.</summary>
    /// <remarks>The shipped CosyVoice2-0.5B has no built-in presets and works zero-shot -- supply a
    /// <see cref="ReferenceAudio"/> clip and its transcript instead; this selection is ignored for it. Hidden
    /// in the UI (nothing reads it there, and showing it invited skipping the reference clip CosyVoice 2
    /// requires) but still settable here. Server default: <c>中文女</c>. Gated behind the
    /// <c>cosyvoice_tts_params</c> feature flag.</remarks>
    [JsonProperty("cosyvoicevoice")]
    public string? CosyVoiceVoice { get; set; }
    #endregion

    #region TTS — Pocket TTS (flag: pockettts_tts_params)
    /// <summary>Built-in voice embedding ("Pocket TTS Voice"). All 27 voices published in the model repo are listed.</summary>
    /// <remarks>Server default: <c>alba</c>. Gated behind the <c>pockettts_tts_params</c> feature flag.</remarks>
    [JsonProperty("pocketttsvoice")]
    public string? PocketTtsVoice { get; set; }
    #endregion

    #region TTS — Kyutai TTS (flag: kyutaitts_tts_params)
    /// <summary>Voice file path within the kyutai/tts-voices repo ("Kyutai TTS Voice").</summary>
    /// <remarks>Folders: <c>expresso/</c> (emotive), <c>ears/</c> (per-speaker emotion), <c>vctk/</c> (speaker
    /// ids), <c>voice-donations/</c> (named). Examples:
    /// <c>expresso/ex03-ex01_happy_001_channel1_334s.wav</c>, <c>vctk/p225_023.wav</c>,
    /// <c>voice-donations/James.wav</c>. Server default:
    /// <c>expresso/ex03-ex01_happy_001_channel1_334s.wav</c>. Gated behind the <c>kyutaitts_tts_params</c>
    /// feature flag.</remarks>
    [JsonProperty("kyutaittsvoice")]
    public string? KyutaiTtsVoice { get; set; }
    #endregion

    #region STT — Whisper (flag: whisper_stt_params)
    /// <summary>Beam-search width ("Whisper Beam Size"). 1 = greedy (fastest); 5 is the upstream default for best accuracy.</summary>
    /// <remarks>Server range 1–10. Server default: <c>5</c>. Gated behind the <c>whisper_stt_params</c> feature flag.</remarks>
    [JsonProperty("whisperbeamsize")]
    public int? WhisperBeamSize { get; set; }

    /// <summary>Optional text biasing vocabulary and spelling ("Whisper Initial Prompt"); useful for names, jargon and acronyms.</summary>
    /// <remarks>Gated behind the <c>whisper_stt_params</c> feature flag.</remarks>
    [JsonProperty("whisperinitialprompt")]
    public string? WhisperInitialPrompt { get; set; }
    #endregion

    #region STT — AssemblyAI (flag: assemblyai_stt_params)
    /// <summary>Label each utterance with a speaker id ("Speaker Labels"); diarization.</summary>
    /// <remarks>Registered as a string dropdown: "false" / "true". Server default: <c>false</c>. Gated behind
    /// the <c>assemblyai_stt_params</c> feature flag.</remarks>
    [JsonProperty("speakerlabels")]
    public string? AssemblySpeakerLabels { get; set; }

    /// <summary>Return per-utterance sentiment alongside the transcript ("Sentiment Analysis").</summary>
    /// <remarks>Registered as a string dropdown: "false" / "true". Server default: <c>false</c>. Gated behind
    /// the <c>assemblyai_stt_params</c> feature flag.</remarks>
    [JsonProperty("sentimentanalysis")]
    public string? AssemblySentiment { get; set; }
    #endregion

    #region TTS — ElevenLabs (flags: elevenlabs_tts_params, elevenlabs_vc_params)
    /// <summary>ElevenLabs voice stability ("ElevenLabs Stability"). Lower = more expressive and variable, higher = more consistent and monotone.</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.5</c>. Gated behind the <c>elevenlabs_tts_params</c> feature flag.</remarks>
    [JsonProperty("elevenlabsstability")]
    public double? ElevenLabsStability { get; set; }

    /// <summary>How closely to match the original voice ("ElevenLabs Similarity Boost").</summary>
    /// <remarks>Very high values can reproduce artifacts present in the source recording. Server range 0–1.
    /// Server default: <c>0.75</c>. Gated behind the <c>elevenlabs_tts_params</c> feature flag.</remarks>
    [JsonProperty("elevenlabssimilarityboost")]
    public double? ElevenLabsSimilarityBoost { get; set; }

    /// <summary>Style exaggeration ("ElevenLabs Style"). 0 disables it and is fastest.</summary>
    /// <remarks>Server range 0–1. Server default: <c>0.0</c>. Gated behind the <c>elevenlabs_tts_params</c> feature flag.</remarks>
    [JsonProperty("elevenlabsstyle")]
    public double? ElevenLabsStyle { get; set; }

    /// <summary>Boost similarity to the original speaker, at some latency cost ("ElevenLabs Speaker Boost").</summary>
    /// <remarks>Registered as a string dropdown: "false" / "true". Server default: <c>true</c>. Gated behind
    /// the <c>elevenlabs_tts_params</c> feature flag.</remarks>
    [JsonProperty("elevenlabsspeakerboost")]
    public string? ElevenLabsSpeakerBoost { get; set; }

    /// <summary>Run the audio-isolation model on the input before converting ("Remove Background Noise").</summary>
    /// <remarks>Documented for Voice Changer only. Registered as a string dropdown: "false" / "true". Server
    /// default: <c>false</c>. Gated behind the <c>elevenlabs_vc_params</c> feature flag.</remarks>
    [JsonProperty("removebackgroundnoise")]
    public string? ElevenLabsRemoveBackgroundNoise { get; set; }
    #endregion

    #region TTS/STT — Azure (flags: azure_tts_params, azure_stt_params)
    /// <summary>mstts express-as speaking style ("Azure Speaking Style"); availability depends on the chosen neural voice.</summary>
    /// <remarks>Allowed values: (empty/default), <c>cheerful</c>, <c>sad</c>, <c>angry</c>, <c>excited</c>,
    /// <c>friendly</c>, <c>hopeful</c>, <c>shouting</c>, <c>whispering</c>, <c>terrified</c>,
    /// <c>unfriendly</c>, <c>newscast</c>, <c>customerservice</c>. Gated behind the <c>azure_tts_params</c>
    /// feature flag.</remarks>
    [JsonProperty("azurespeakingstyle")]
    public string? AzureSpeakingStyle { get; set; }

    /// <summary>Intensity of the selected speaking style ("Azure Style Degree"). 1.0 is normal.</summary>
    /// <remarks>Server range 0.01–2. Server default: <c>1.0</c>. Gated behind the <c>azure_tts_params</c> feature flag.</remarks>
    [JsonProperty("azurestyledegree")]
    public double? AzureStyleDegree { get; set; }

    /// <summary>How Azure handles profanity in the transcript ("Azure Profanity Handling").</summary>
    /// <remarks>API default is masked (asterisks); removed strips it; raw leaves it in. Allowed values:
    /// <c>masked</c>, <c>removed</c>, <c>raw</c>. Server default: <c>masked</c>. Gated behind the
    /// <c>azure_stt_params</c> feature flag.</remarks>
    [JsonProperty("azureprofanityhandling")]
    public string? AzureProfanityHandling { get; set; }
    #endregion

    #region STT — Cloud model selection (flags: deepgram_stt_params, google_stt_params, openai_stt_params)
    /// <summary>Deepgram speech-to-text model ("Deepgram Model"). Nova-3 is the current general-purpose recommendation.</summary>
    /// <remarks>Allowed values: <c>nova-3</c> (recommended), <c>nova-3-medical</c>, <c>nova-2</c>,
    /// <c>flux-general-en</c>, <c>flux-general-multi</c>, <c>whisper-large</c>. Server default:
    /// <c>nova-3</c>. Gated behind the <c>deepgram_stt_params</c> feature flag.</remarks>
    [JsonProperty("deepgrammodel")]
    public string? DeepgramModel { get; set; }

    /// <summary>Google Speech-to-Text v1 model ("Google STT Model").</summary>
    /// <remarks>Chirp 2/3 are Speech-to-Text v2 only and are not reachable from this v1 endpoint. Allowed
    /// values: <c>latest_long</c>, <c>latest_short</c>, <c>telephony</c>, <c>telephony_short</c>,
    /// <c>medical_dictation</c>, <c>medical_conversation</c>, <c>command_and_search</c>, <c>video</c>,
    /// <c>phone_call</c>, <c>default</c>. Server default: <c>latest_long</c>. Gated behind the
    /// <c>google_stt_params</c> feature flag.</remarks>
    [JsonProperty("googlesttmodel")]
    public string? GoogleSttModel { get; set; }

    /// <summary>Optional text biasing the transcript's vocabulary and spelling ("Transcription Prompt"): names, jargon, acronyms.</summary>
    /// <remarks>Gated behind the <c>openai_stt_params</c> feature flag.</remarks>
    [JsonProperty("transcriptionprompt")]
    public string? OpenAiTranscriptionPrompt { get; set; }
    #endregion

    #region TTS — Cloud voice/style extras (OpenAI, Google, Deepgram, Cartesia, PlayHT, Dolby)
    /// <summary>Voice for OpenAI text-to-speech ("OpenAI Voice"). All 13 documented voices are listed.</summary>
    /// <remarks>tts-1 and tts-1-hd support only 9 of them; ballad, marin and cedar are gpt-4o-mini-tts only.
    /// Server default: <c>alloy</c>. Gated behind the <c>openai_tts_params</c> feature flag.</remarks>
    [JsonProperty("openaivoice")]
    public string? OpenAiVoice { get; set; }

    /// <summary>Free-form delivery direction ("OpenAI Instructions"), e.g. 'Speak slowly and sound apologetic'.</summary>
    /// <remarks>Supported by gpt-4o-mini-tts only. Gated behind the <c>openai_tts_instructions_params</c> feature flag.</remarks>
    [JsonProperty("openaiinstructions")]
    public string? OpenAiInstructions { get; set; }

    /// <summary>Speech speed multiplier ("OpenAI Speed").</summary>
    /// <remarks>Server range 0.25–4. Server default: <c>1.0</c>. Gated behind the <c>openai_tts_params</c> feature flag.</remarks>
    [JsonProperty("openaispeed")]
    public double? OpenAiSpeed { get; set; }

    /// <summary>Google Cloud voice name ("Google Voice Name"). Must match the selected language.</summary>
    /// <remarks>Server default: <c>en-US-Neural2-F</c>. Gated behind the <c>google_tts_params</c> feature flag.</remarks>
    [JsonProperty("googlevoicename")]
    public string? GoogleVoiceName { get; set; }

    /// <summary>Speaking rate ("Google Speaking Rate"). 1.0 is the voice's natural speed.</summary>
    /// <remarks>The API accepts 0.25 to 2.0. Server range 0.25–2. Server default: <c>1.0</c>. Gated behind the
    /// <c>google_tts_params</c> feature flag.</remarks>
    [JsonProperty("googlespeakingrate")]
    public double? GoogleSpeakingRate { get; set; }

    /// <summary>Pitch offset in semitones ("Google Pitch").</summary>
    /// <remarks>Server range -20–20. Server default: <c>0.0</c>. Gated behind the <c>google_tts_params</c> feature flag.</remarks>
    [JsonProperty("googlepitch")]
    public double? GooglePitch { get; set; }

    /// <summary>Deepgram Aura voice model ("Deepgram Voice"). Aura-2 is the newer generation; the Aura-1 voices remain available.</summary>
    /// <remarks>Server default: <c>aura-2-thalia-en</c>. Gated behind the <c>deepgram_tts_params</c> feature flag.</remarks>
    [JsonProperty("deepgramvoice")]
    public string? DeepgramVoice { get; set; }

    /// <summary>Cartesia voice id from your voice library ("Cartesia Voice ID").</summary>
    /// <remarks>Gated behind the <c>cartesia_tts_params</c> feature flag.</remarks>
    [JsonProperty("cartesiavoiceid")]
    public string? CartesiaVoiceId { get; set; }

    /// <summary>Cartesia Sonic model id ("Cartesia Model"). sonic-3.5 is the current API default.</summary>
    /// <remarks>Allowed values: <c>sonic-3.5</c> (default), <c>sonic-3</c>, <c>sonic-preview</c>,
    /// <c>sonic-latest</c>. Server default: <c>sonic-3.5</c>. Gated behind the <c>cartesia_tts_params</c>
    /// feature flag.</remarks>
    [JsonProperty("cartesiamodel")]
    public string? CartesiaModel { get; set; }

    /// <summary>Speech speed via <c>generation_config</c> ("Cartesia Speed"). The API accepts 0.6x to 1.5x.</summary>
    /// <remarks>Server range 0.6–1.5. Server default: <c>1.0</c>. Gated behind the <c>cartesia_tts_params</c> feature flag.</remarks>
    [JsonProperty("cartesiaspeed")]
    public double? CartesiaSpeed { get; set; }

    /// <summary>PlayHT voice id or manifest URL ("PlayHT Voice").</summary>
    /// <remarks>Gated behind the <c>playht_tts_params</c> feature flag.</remarks>
    [JsonProperty("playhtvoice")]
    public string? PlayHtVoice { get; set; }

    /// <summary>Output quality tier ("PlayHT Quality"). Higher costs more and is slower.</summary>
    /// <remarks>Allowed values: <c>draft</c>, <c>low</c>, <c>medium</c>, <c>high</c>, <c>premium</c>. Server
    /// default: <c>medium</c>. Gated behind the <c>playht_tts_params</c> feature flag.</remarks>
    [JsonProperty("playhtquality")]
    public string? PlayHtQuality { get; set; }

    /// <summary>Synthesis engine ("PlayHT Voice Engine"). The API default is PlayHT2.0; the Play3.0/PlayDialog engines are newer.</summary>
    /// <remarks>Allowed values: <c>PlayDialog-turbo</c>, <c>PlayDialog</c>, <c>Play3.0-mini</c>,
    /// <c>PlayHT2.0-turbo</c>, <c>PlayHT2.0</c> (default), <c>PlayHT1.0</c>. Server default:
    /// <c>PlayHT2.0</c>. Gated behind the <c>playht_tts_params</c> feature flag.</remarks>
    [JsonProperty("playhtvoiceengine")]
    public string? PlayHtVoiceEngine { get; set; }

    /// <summary>Speech speed ("PlayHT Speed"). The API accepts 0.1 to 5.0.</summary>
    /// <remarks>Server range 0.1–5. Server default: <c>1.0</c>. Gated behind the <c>playht_tts_params</c> feature flag.</remarks>
    [JsonProperty("playhtspeed")]
    public double? PlayHtSpeed { get; set; }

    /// <summary>Dolby.io Media Enhance <c>content.type</c> preset ("Dolby Enhance Preset").</summary>
    /// <remarks>NOTE: the public Media Enhance reference is currently unreachable, so this list could not be
    /// verified; an unsupported value is rejected by the API. Allowed values: <c>voice_over</c>,
    /// <c>conference</c>, <c>interview</c>, <c>lecture</c>, <c>meeting</c>, <c>mobile_phone</c>,
    /// <c>music</c>, <c>podcast</c>, <c>studio</c>. Server default: <c>voice_over</c>. Gated behind the
    /// <c>dolby_audioproc_params</c> feature flag.</remarks>
    [JsonProperty("dolbyenhancepreset")]
    public string? DolbyEnhancePreset { get; set; }
    #endregion

    #region TTS — Amazon Polly (flag: polly_tts_params)
    /// <summary>Polly synthesis engine ("Polly Engine"). Neural sounds better; standard covers more voices.</summary>
    /// <remarks>Allowed values: <c>neural</c>, <c>standard</c>, <c>long-form</c>, <c>generative</c>. Server
    /// default: <c>neural</c>. Gated behind the <c>polly_tts_params</c> feature flag.</remarks>
    [JsonProperty("pollyengine")]
    public string? PollyEngine { get; set; }

    /// <summary>Polly voice id ("Polly Voice"). Each voice supports only some engines, noted in its label.</summary>
    /// <remarks>27 voices across en-US/GB/AU/NZ/ZA/IE/IN. Server default: <c>Joanna</c>. Gated behind the
    /// <c>polly_tts_params</c> feature flag.</remarks>
    [JsonProperty("pollyvoice")]
    public string? PollyVoice { get; set; }
    #endregion

    #region Clone — RVC (flag: rvc_clone_params)
    /// <summary>Semitone pitch shift for RVC voice conversion ("Pitch Shift"). 0 = no shift, +12 = octave up, -12 = octave down.</summary>
    /// <remarks>Server range -12–12. Server default: <c>0</c>. Gated behind the <c>rvc_clone_params</c> feature flag.</remarks>
    [JsonProperty("pitchshift")]
    public int? RvcPitchShift { get; set; }

    /// <summary>Pitch-extraction algorithm ("F0 Method").</summary>
    /// <remarks>RMVPE tracks pitch (incl. octave errors and unvoiced segments) more robustly than YIN;
    /// pm/harvest/crepe are not implemented in the engine yet and fall back to YIN. Allowed values:
    /// <c>rmvpe</c> (in use today), <c>yin</c> (in use today), <c>pm</c> (pending), <c>harvest</c>
    /// (pending), <c>crepe</c> (pending). Server default: <c>rmvpe</c>. Gated behind the
    /// <c>rvc_clone_params</c> feature flag.</remarks>
    [JsonProperty("fmethod")]
    public string? RvcF0Method { get; set; }

    /// <summary>How much the index file influences the result ("Index Rate"). Upstream default is 0.75.</summary>
    /// <remarks>Lower values can reduce artifacts. NOTE: index retrieval is not implemented in the engine yet.
    /// Server range 0–1. Server default: <c>0.75</c>. Gated behind the <c>rvc_clone_params</c> feature flag.</remarks>
    [JsonProperty("indexrate")]
    public double? RvcIndexRate { get; set; }

    /// <summary>Volume envelope mixing ratio ("RMS Mix Rate"). 1.0 = use original input volume, 0.0 = use model output volume.</summary>
    /// <remarks>Server range 0–1. Server default: <c>1.0</c>. Gated behind the <c>rvc_clone_params</c> feature flag.</remarks>
    [JsonProperty("rmsmixrate")]
    public double? RvcRmsMixRate { get; set; }

    /// <summary>Protects voiceless consonants and breath sounds ("Protect"). Higher values preserve more consonant detail; 0.5 = max protection.</summary>
    /// <remarks>Server range 0–0.5. Server default: <c>0.33</c>. Gated behind the <c>rvc_clone_params</c> feature flag.</remarks>
    [JsonProperty("protect")]
    public double? RvcProtect { get; set; }
    #endregion

    #region Clone — GPT-SoVITS (flag: gptsovits_clone_params)
    /// <summary>Transcript of the reference audio for GPT-SoVITS ("Clone Prompt Text"). Improves cloning accuracy when provided.</summary>
    /// <remarks>Gated behind the <c>gptsovits_clone_params</c> feature flag.</remarks>
    [JsonProperty("cloneprompttext")]
    public string? GptSoVitsClonePromptText { get; set; }

    /// <summary>Language for GPT-SoVITS voice cloning ("Clone Language").</summary>
    /// <remarks>Allowed values: <c>en</c>, <c>zh</c>, <c>ja</c>, <c>ko</c>. Server default: <c>en</c>. Gated
    /// behind the <c>gptsovits_clone_params</c> feature flag.</remarks>
    [JsonProperty("clonelanguage")]
    public string? GptSoVitsCloneLanguage { get; set; }
    #endregion

    #region Audio FX Shared (flag: audiolab_audioproc)
    /// <summary>Audio file to process ("FX Input"). Upload audio for separation, enhancement, or denoising.</summary>
    /// <remarks>Data URL or server path. Gated behind the <c>audiolab_audioproc</c> feature flag.</remarks>
    [JsonProperty("fxinput")]
    public string? FxInput { get; set; }
    #endregion

    #region FX — Demucs (flag: demucs_fx_params)
    /// <summary>Fractional overlap between processing segments ("Demucs Overlap"). Upstream default is 0.25.</summary>
    /// <remarks>Its README notes this can be reduced to about 0.1 for a bit more speed. Server range 0–0.95.
    /// Server default: <c>0.25</c>. Gated behind the <c>demucs_fx_params</c> feature flag.</remarks>
    [JsonProperty("demucsoverlap")]
    public double? DemucsOverlap { get; set; }

    /// <summary>Length of each processing segment, in seconds ("Demucs Segment").</summary>
    /// <remarks>Lower it if you run out of memory. Hybrid Transformer checkpoints support at most 7.8s, so
    /// larger values are clamped. Server range 1–7.8. Server default: <c>7.8</c>. Gated behind the
    /// <c>demucs_fx_params</c> feature flag.</remarks>
    [JsonProperty("demucssegment")]
    public double? DemucsSegment { get; set; }

    /// <summary>Demucs "shift trick" repetitions ("Demucs Shifts"): separate several randomly time-shifted copies and average them.</summary>
    /// <remarks>Upstream documents this as worth up to 0.2 SDR, and it makes the run exactly this many times
    /// slower. 0 = off. Server range 0–10. Server default: <c>0</c>. Gated behind the
    /// <c>demucs_fx_params</c> feature flag.</remarks>
    [JsonProperty("demucsshifts")]
    public int? DemucsShifts { get; set; }
    #endregion

    #region FX — Resemble Enhance (flag: resemble_enhance_fx_params)
    /// <summary>CFM function-evaluation budget ("Enhancement Steps (NFE)").</summary>
    /// <remarks>Upstream's own interface exposes 1-128 with a default of 64. More = slower, generally cleaner.
    /// Server range 1–128. Server default: <c>64</c>. Gated behind the <c>resemble_enhance_fx_params</c>
    /// feature flag.</remarks>
    [JsonProperty("enhancementstepsnfe")]
    public int? ResembleEnhanceSteps { get; set; }

    /// <summary>ODE solver method for enhancement ("Solver"). Midpoint is recommended for best quality/speed balance.</summary>
    /// <remarks>Allowed values: <c>midpoint</c> (recommended), <c>euler</c>, <c>rk4</c>. Server default:
    /// <c>midpoint</c>. Gated behind the <c>resemble_enhance_fx_params</c> feature flag.</remarks>
    [JsonProperty("solver")]
    public string? ResembleEnhanceSolver { get; set; }

    /// <summary>How much the denoiser output is blended in before enhancement ("Lambda (Denoise Blend)").</summary>
    /// <remarks>This is NOT the temperature; that is <see cref="ResembleEnhanceTau"/>. Server range 0–1.
    /// Server default: <c>0.1</c>. Gated behind the <c>resemble_enhance_fx_params</c> feature flag.</remarks>
    [JsonProperty("lambdadenoiseblend")]
    public double? ResembleEnhanceLambda { get; set; }

    /// <summary>CFM prior temperature ("Tau (Prior Temperature)").</summary>
    /// <remarks>Upstream's interface exposes 0-1 with a default of 0.5. Server range 0–1. Server default:
    /// <c>0.5</c>. Gated behind the <c>resemble_enhance_fx_params</c> feature flag.</remarks>
    [JsonProperty("taupriortemperature")]
    public double? ResembleEnhanceTau { get; set; }
    #endregion
}
