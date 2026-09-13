using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>Generation parameters registered by the AudioLab server extension.</summary>
/// <remarks>Only send parameters advertised by the selected model's feature flags.</remarks>
public partial class GenerationRequest
{
    /// <summary>Container for the returned audio ("Audio Output Format"): wav_16, wav_32, flac, mp3, ogg.</summary>
    [JsonProperty("audiooutputformat")]
    public string? AudioOutputFormat { get; set; }

    /// <summary>Encoding quality for the returned audio ("Audio Quality"): low, medium, high, max.</summary>
    [JsonProperty("audioquality")]
    public string? AudioQuality { get; set; }

    // --- Music generation: ACE-Step ("acestep_music_params") ---
    /// <summary>Lyrics for ACE-Step ("Lyrics"). "[Instrumental]" for no vocals.</summary>
    /// <remarks>ACE-Step only. Every other music provider has its own lyrics parameter: <see cref="Yue2Lyrics"/>,
    /// <see cref="YuELyrics"/>, <see cref="HeartLibLyrics"/>, <see cref="MiniMaxMusic3Lyrics"/>. Sending this one
    /// to a YuE2 model does nothing — SwarmUI accepts the name and the provider never reads it.</remarks>
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

    // --- ACE-Step: solver and guidance interval ("acestep_music_params", "acestep_cfg_params") ---
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

    // --- ACE-Step: LM metadata planner ("acestep_lm_params") ---
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

    // --- ACE-Step: audio-to-audio tasks ("acestep_task_params") ---
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

    // --- YuE2 ("yue2_music_params") ---
    // YuE2 runs two passes: a score planner that writes an ABC score, then a codec-token pass that renders the
    // audio. "Score *" parameters steer the planner, "Song *" parameters steer the audio. The names are not
    // prefixed "YuE2" on the wire because SwarmUI strips digits from parameter ids, which would collide with YuE v1.
    /// <summary>Lyrics for YuE2 ("Song Lyrics"), section tags such as [verse] / [chorus] each on their own line.</summary>
    /// <remarks>Style and genre tags belong in <see cref="GenerationRequest.Prompt"/>, not here.</remarks>
    [JsonProperty("songlyrics")]
    public string? Yue2Lyrics { get; set; }

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
    /// prefills a second cache, which both doubles the cost and shortens the length that fits in its context.</remarks>
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

    // --- YuE v1 ("yue_music_params") — a different model from YuE2, sharing only the name ---
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

    // --- HeartMuLa ("heartlib_music_params") ---
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

    // --- MiniMax Music 3 ("minimax_music3_params") ---
    /// <summary>Lyrics for MiniMax Music 3 ("MiniMax Music 3 Lyrics"). Each section tag must be on its own line.</summary>
    [JsonProperty("minimaxmusiclyrics")]
    public string? MiniMaxMusic3Lyrics { get; set; }

    /// <summary>Flow-matching guidance strength ("MiniMax Music 3 CFG Scale"); the reference recipe uses 1.7. Server range 0.1–10.</summary>
    [JsonProperty("minimaxmusiccfgscale")]
    public float? MiniMaxMusic3CfgScale { get; set; }

    /// <summary>Euler steps per 200-frame window ("MiniMax Music 3 Steps"); the reference recipe uses 30. Server range 4–100.</summary>
    [JsonProperty("minimaxmusicsteps")]
    public int? MiniMaxMusic3Steps { get; set; }

    // --- Clip length ("audiolab_audiogen") ---
    /// <summary>Maximum output length in seconds ("Max Duration"). Server range 1–900.</summary>
    /// <remarks>Shared by every AudioLab generation provider, not just AudioCraft. It is a ceiling, not a target:
    /// a model that finishes early returns the shorter clip. The server prefers the stock
    /// <see cref="GenerationRequest.Text2AudioDuration"/> when that is set and falls back to this one, so set one
    /// or the other rather than both. YuE2 clamps further per request — a long prompt and score leave less room
    /// inside the model's context, and the budget actually used comes back in the result metadata.</remarks>
    [JsonProperty("maxduration")]
    public float? MaxDuration { get; set; }

    // --- AudioCraft sampling: MusicGen, AudioGen ("audiocraft_sampling") ---

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
}
