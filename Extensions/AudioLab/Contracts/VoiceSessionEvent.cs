using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>A caller or assistant transcript line.</summary>
public class VoiceSessionTranscript
{
    /// <summary>"user" or "assistant".</summary>
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Transcribed or generated text.</summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>The turn this line belongs to.</summary>
    [JsonProperty("turnId")]
    public int TurnId { get; set; }
}

/// <summary>A barge-in: the caller started speaking mid-reply, interrupting the named turn.</summary>
/// <remarks>Reply audio and events already in flight for this turn id (and any lower) should be discarded --
/// <see cref="AudioLabVoiceSessionClient.ReplyAudio"/> frames carry their own turn id for exactly this.</remarks>
public class VoiceSessionBargeIn
{
    /// <summary>The turn that was interrupted.</summary>
    [JsonProperty("turnId")]
    public int TurnId { get; set; }
}

/// <summary>A live tool call report. Dispatch is the server's responsibility (through LLM Assistant); this is a
/// report, not a request for the caller to execute anything.</summary>
public class VoiceSessionToolCall
{
    /// <summary>Identifier the matching <see cref="VoiceSessionToolResult"/> will carry.</summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>Tool name.</summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>Call arguments as a JSON-encoded string (not a nested value -- unlike
    /// <c>LLMAssistantVoiceTurnWS</c>'s <c>native_tool_call.arguments</c>, this route's server embeds
    /// <c>NativeToolCall.Arguments</c> directly, and that property is itself documented as a raw JSON string).</summary>
    [JsonProperty("arguments")]
    public string? Arguments { get; set; }

    /// <summary>The turn this call happened in.</summary>
    [JsonProperty("turnId")]
    public int TurnId { get; set; }
}

/// <summary>The result of a tool call this same stream already reported via a <see cref="VoiceSessionToolCall"/>.</summary>
public class VoiceSessionToolResult
{
    /// <summary>Matches the originating <see cref="VoiceSessionToolCall.Id"/>.</summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>Tool name.</summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>Result as a plain string (same reasoning as <see cref="VoiceSessionToolCall.Arguments"/>).</summary>
    [JsonProperty("result")]
    public string? Result { get; set; }

    /// <summary>The turn this result belongs to.</summary>
    [JsonProperty("turnId")]
    public int TurnId { get; set; }
}

/// <summary>Timing and outcome for one completed turn.</summary>
/// <remarks>Field names mirror the wire's dotted keys directly rather than a nested object, since that is the
/// shape the server sends. Every millisecond field is 0 when a turn's kind did not measure that stage rather
/// than the field being omitted.</remarks>
public class VoiceSessionMetrics
{
    [JsonProperty("turnId")]
    public int TurnId { get; set; }

    /// <summary>What kind of turn this was (eg a normal reply vs. one cut short by a barge-in).</summary>
    [JsonProperty("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonProperty("voice.frontend.ms.p50")]
    public double FrontendP50Ms { get; set; }

    [JsonProperty("voice.frontend.ms.p99")]
    public double FrontendP99Ms { get; set; }

    [JsonProperty("voice.frontend.ms.max")]
    public double FrontendMaxMs { get; set; }

    [JsonProperty("voice.endpoint.ms")]
    public double EndpointMs { get; set; }

    [JsonProperty("voice.stt.ms")]
    public double SttMs { get; set; }

    [JsonProperty("voice.llm.ttft_ms")]
    public double LlmTtftMs { get; set; }

    [JsonProperty("voice.llm.first_sentence_ms")]
    public double LlmFirstSentenceMs { get; set; }

    [JsonProperty("voice.tts.first_chunk_ms")]
    public double TtsFirstChunkMs { get; set; }

    [JsonProperty("voice.transport.ms")]
    public double TransportMs { get; set; }

    [JsonProperty("voice.turn.total_ms")]
    public double TotalMs { get; set; }

    [JsonProperty("voice.bargein.stop_ms")]
    public double BargeInStopMs { get; set; }

    [JsonProperty("toolCalls")]
    public int ToolCalls { get; set; }

    [JsonProperty("interrupted")]
    public bool Interrupted { get; set; }
}

/// <summary>A single JSON event from <see cref="AudioLabVoiceSessionClient.Events"/>.</summary>
/// <remarks>Exactly one of the typed members is set per event, matching the server's own single-key-per-frame
/// convention (<c>AudioAPI/VoiceSessionEndpoints.EventToJson</c>); <see cref="Raw"/> preserves the complete frame
/// for any field without a typed member. Reply audio is a separate stream (<see cref="VoiceSessionAudioFrame"/>
/// over <see cref="AudioLabVoiceSessionClient.ReplyAudio"/>), never one of these events -- the server sends it as
/// binary frames on the same socket, not JSON.</remarks>
public class VoiceSessionEvent
{
    /// <summary>The session's state changed (eg to "Listening", "Thinking", "Speaking").</summary>
    [JsonProperty("state")]
    public string? State { get; set; }

    /// <summary>A caller or assistant transcript line.</summary>
    [JsonProperty("transcript")]
    public VoiceSessionTranscript? Transcript { get; set; }

    /// <summary>The caller barged in on the assistant's reply.</summary>
    [JsonProperty("bargein")]
    public VoiceSessionBargeIn? BargeIn { get; set; }

    /// <summary>A live tool call report.</summary>
    [JsonProperty("tool_call")]
    public VoiceSessionToolCall? ToolCall { get; set; }

    /// <summary>The result of a previously reported tool call.</summary>
    [JsonProperty("tool_result")]
    public VoiceSessionToolResult? ToolResult { get; set; }

    /// <summary>An informational notice (eg a requested voice being unavailable while another call is using it)
    /// that is not itself a failure.</summary>
    [JsonProperty("notice")]
    public string? Notice { get; set; }

    /// <summary>Timing and outcome for one completed turn.</summary>
    [JsonProperty("metrics")]
    public VoiceSessionMetrics? Metrics { get; set; }

    /// <summary>Failure detail. A fatal one ends the session; the server closes the socket afterward.</summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>The complete frame as received, including any field without a typed member.</summary>
    [JsonIgnore]
    public JObject Raw { get; set; } = [];

    /// <summary>Whether this event reports a failure.</summary>
    [JsonIgnore]
    public bool IsError => Error is not null;
}

/// <summary>One decoded reply-audio frame from <see cref="AudioLabVoiceSessionClient.ReplyAudio"/>.</summary>
/// <param name="TurnId">The turn that produced <paramref name="Samples"/>. Never mixes two turns -- the server
/// guarantees at most one turn's audio per frame, stopping where the turn changes.</param>
/// <param name="Samples">Mono float samples in [-1, 1] at the session's outbound rate (24 kHz, Kokoro's own rate;
/// the server never resamples outbound audio).</param>
public sealed record VoiceSessionAudioFrame(int TurnId, float[] Samples);
