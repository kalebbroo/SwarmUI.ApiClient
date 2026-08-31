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

    // --- Music generation ---
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

    // --- AudioCraft sampling (MusicGen, AudioGen) ---
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
