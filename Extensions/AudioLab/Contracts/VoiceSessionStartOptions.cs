using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>The <c>start</c> handshake frame <see cref="AudioLabVoiceSessionClient.ConnectAsync"/> sends as the
/// first message of an <c>AudioLabVoiceSession</c> connection.</summary>
/// <remarks>Matches the server's own parsed shape (<c>VoiceSessionStart.TryParse</c>) field for field. There is
/// no second handshake message -- audio and events begin flowing immediately after this one.</remarks>
public class VoiceSessionStartOptions
{
    /// <summary>LLM model id to answer with. Required.</summary>
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>LLM Assistant assistant id, or null for the server's default.</summary>
    [JsonProperty("assistantId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssistantId { get; set; }

    /// <summary>Kokoro voice id, or null for the server's default. Fixed for the lifetime of the shared model set
    /// a call lands on -- a mismatched request while another call is active gets a <c>notice</c> event instead
    /// of the requested voice (see <see cref="VoiceSessionEvent.Notice"/>).</summary>
    [JsonProperty("voice", NullValueHandling = NullValueHandling.Ignore)]
    public string? Voice { get; set; }

    /// <summary>System prompt override, or null for the server's default persona.</summary>
    [JsonProperty("systemPrompt", NullValueHandling = NullValueHandling.Ignore)]
    public string? SystemPrompt { get; set; }

    /// <summary>Whether the caller speaking mid-reply interrupts (barges in on) the assistant's turn. Defaults to
    /// true, matching the server's own default when this field is omitted.</summary>
    [JsonProperty("bargeIn")]
    public bool BargeIn { get; set; } = true;

    /// <summary>Sample rate, in Hz, of the mono PCM16 audio <see cref="AudioLabVoiceSessionClient.SendAudioAsync"/>
    /// will send. Required; must be between 8000 and 192000 and the server must be able to resample it to 16 kHz
    /// (any rate a real browser or microphone actually offers works -- only a rate chosen to be pathologically
    /// coprime with 16000 can fail this).</summary>
    [JsonProperty("inputRate")]
    public int InputRate { get; set; } = 16000;
}
