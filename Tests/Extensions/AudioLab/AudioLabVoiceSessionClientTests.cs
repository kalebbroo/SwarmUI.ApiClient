using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Extensions.AudioLab;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.Tests;
using SwarmUI.ApiClient.WebSockets;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.AudioLab;

/// <summary>Unit tests for <see cref="AudioLabVoiceSessionClient"/>, driven over the same scripted
/// <see cref="FakeClientWebSocket"/> seam <see cref="SwarmWebSocketClientTests"/> uses for the generic WS client,
/// since this route's mixed binary/JSON protocol needs its own connect/send/receive/close logic rather than
/// going through <see cref="ISwarmWebSocketClient.StreamFramesAsync"/>.</summary>
public class AudioLabVoiceSessionClientTests
{
    private static SwarmClientOptions Options() => new()
    {
        BaseUrl = "http://localhost:7801",
        MaxRetryAttempts = 0,
        RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        SessionRefreshCap = 3,
        WebSocketReceiveTimeout = TimeSpan.FromSeconds(5)
    };

    private static byte[] Utf8(string json) => System.Text.Encoding.UTF8.GetBytes(json);

    [Fact]
    public async Task ConnectAsync_SendsStartPayloadWithSessionId_AndForwardsTheWarmingEventToTheStream()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocketFactory factory = new(socket);
        FakeSessionManager sessions = new();
        AudioLabVoiceSessionClient client = new(Options(), sessions, factory);

        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", AssistantId = "asst-1", InputRate = 16000 }, "user-1", CancellationToken.None).ConfigureAwait(false);

        JObject sent = JObject.Parse(Assert.Single(socket.SentMessages));
        Assert.Equal("qwen3", sent["model"]?.ToString());
        Assert.Equal("asst-1", sent["assistantId"]?.ToString());
        Assert.Equal(16000, sent["inputRate"]?.ToObject<int>());
        Assert.True(sent["bargeIn"]?.ToObject<bool>());
        Assert.False(string.IsNullOrEmpty(sent["session_id"]?.ToString()));

        List<VoiceSessionEvent> events = [];
        await foreach (VoiceSessionEvent evt in client.Events)
        {
            events.Add(evt);
        }
        Assert.Single(events);
        Assert.Equal("Warming", events[0].State);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_RequiresModel()
    {
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory());

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.ConnectAsync(new VoiceSessionStartOptions { Model = "  ", InputRate = 16000 }).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Fact]
    public async Task ConnectAsync_ThrowsIfCalledASecondTime()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false)).ConfigureAwait(false);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_InvalidSession_RefreshesAndReconnects()
    {
        FakeClientWebSocket rejected = new([
            new FakeClientWebSocket.TextFrame("""{"error":"Invalid session ID. You may need to refresh the page.","error_id":"invalid_session_id"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocket accepted = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocketFactory factory = new(rejected, accepted);
        FakeSessionManager sessions = new();
        AudioLabVoiceSessionClient client = new(Options(), sessions, factory);

        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }, "user-1").ConfigureAwait(false);

        (string key, string? observed) = Assert.Single(sessions.Invalidations);
        Assert.Equal("user-1", key);
        Assert.NotNull(observed);
        Assert.Equal(2, sessions.CreateCount);
        Assert.True(rejected.Disposed);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_RejectedStartMessage_ThrowsSwarmWebSocketException()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"error":"'inputRate' is required and must be an integer."}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));

        SwarmWebSocketException ex = await Assert.ThrowsAsync<SwarmWebSocketException>(async () =>
            await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.Contains("inputRate", ex.Message);
    }

    [Fact]
    public async Task Events_DecodesEveryEventKind()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.TextFrame("""{"transcript":{"role":"user","text":"hi","turnId":1}}"""),
            new FakeClientWebSocket.TextFrame("""{"bargein":{"turnId":1}}"""),
            new FakeClientWebSocket.TextFrame("""{"tool_call":{"id":"call-1","name":"get_weather","arguments":"{\"city\":\"Reno\"}","turnId":2}}"""),
            new FakeClientWebSocket.TextFrame("""{"tool_result":{"id":"call-1","name":"get_weather","result":"{\"tempF\":72}","turnId":2}}"""),
            new FakeClientWebSocket.TextFrame("""{"notice":"tool calling is unavailable for this model"}"""),
            new FakeClientWebSocket.TextFrame("""{"metrics":{"turnId":2,"kind":"Reply","voice.frontend.ms.p50":1.5,"voice.llm.ttft_ms":120.0,"toolCalls":1,"interrupted":false}}"""),
            new FakeClientWebSocket.TextFrame("""{"error":"a fatal problem"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        List<VoiceSessionEvent> events = [];
        await foreach (VoiceSessionEvent evt in client.Events)
        {
            events.Add(evt);
        }

        Assert.Equal(8, events.Count);
        Assert.Equal("Warming", events[0].State);
        Assert.Equal("hi", events[1].Transcript?.Text);
        Assert.Equal(1, events[1].Transcript?.TurnId);
        Assert.Equal(1, events[2].BargeIn?.TurnId);
        Assert.Equal("call-1", events[3].ToolCall?.Id);
        Assert.Equal("{\"city\":\"Reno\"}", events[3].ToolCall?.Arguments);
        Assert.Equal("{\"tempF\":72}", events[4].ToolResult?.Result);
        Assert.Equal("tool calling is unavailable for this model", events[5].Notice);
        Assert.Equal(2, events[6].Metrics?.TurnId);
        Assert.Equal("Reply", events[6].Metrics?.Kind);
        Assert.Equal(1.5, events[6].Metrics?.FrontendP50Ms);
        Assert.Equal(1, events[6].Metrics?.ToolCalls);
        Assert.False(events[6].Metrics?.Interrupted);
        Assert.True(events[7].IsError);
        Assert.Equal("a fatal problem", events[7].Error);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task ReplyAudio_DecodesTurnTaggedBinaryFrames()
    {
        float[] original = [0.5f, -0.5f, 0.25f, -0.25f];
        byte[] frame = EncodeOutboundFrame(turnId: 7, original);
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.BinaryFrame(frame),
            new FakeClientWebSocket.CloseFrame()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        List<VoiceSessionAudioFrame> frames = [];
        await foreach (VoiceSessionAudioFrame audioFrame in client.ReplyAudio)
        {
            frames.Add(audioFrame);
        }

        VoiceSessionAudioFrame decoded = Assert.Single(frames);
        Assert.Equal(7, decoded.TurnId);
        Assert.Equal(original.Length, decoded.Samples.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], decoded.Samples[i], 0.001);
        }
        await client.DisposeAsync();
    }

    [Fact]
    public async Task SendAudioAsync_EncodesFloatSamplesAsPcm16BinaryFrame()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        float[] samples = [0.1f, -0.2f, 0.9f, -1.0f];
        await client.SendAudioAsync(samples).ConfigureAwait(false);

        byte[] sentBytes = Assert.Single(socket.SentBinaryMessages);
        Assert.Equal(samples.Length * 2, sentBytes.Length);
        float[] decoded = DecodePcm16(sentBytes);
        for (int i = 0; i < samples.Length; i++)
        {
            Assert.Equal(samples[i], decoded[i], 0.001);
        }
        await client.DisposeAsync();
    }

    [Fact]
    public async Task EndAsync_SendsEndFrame_AndCompletesTheCloseHandshake_NeverABareDispose()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.HangStep()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        await client.EndAsync().ConfigureAwait(false);

        JObject lastSent = JObject.Parse(socket.SentMessages[^1]);
        Assert.True(lastSent["end"]?.ToObject<bool>());
        Assert.True(socket.CloseOutputCalled);
        Assert.True(socket.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_WithoutEndAsyncFirst_StillCompletesTheCloseHandshake_NeverABareDispose()
    {
        // The HangStep after Warming simulates the ordinary, healthy case: nothing more has arrived because
        // nobody has spoken yet. If DisposeAsync (or EndAsync) ever regressed to not cancelling the pump before
        // the close handshake's own receive, this would deadlock instead of completing -- the test's own
        // timeout is the backstop for that, same as it would be for a real stuck connection.
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"state":"Warming"}"""),
            new FakeClientWebSocket.HangStep()
        ]);
        AudioLabVoiceSessionClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await client.ConnectAsync(new VoiceSessionStartOptions { Model = "qwen3", InputRate = 16000 }).ConfigureAwait(false);

        await client.DisposeAsync().ConfigureAwait(false);

        Assert.True(socket.CloseOutputCalled);
        Assert.True(socket.Disposed);
    }

    private static byte[] EncodeOutboundFrame(int turnId, float[] samples)
    {
        byte[] frame = new byte[4 + (samples.Length * 2)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(frame, turnId);
        byte[] pcm = EncodePcm16(samples);
        pcm.CopyTo(frame, 4);
        return frame;
    }

    private static byte[] EncodePcm16(ReadOnlySpan<float> samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)Math.Clamp(MathF.Round(samples[i] * 32767f), short.MinValue, short.MaxValue);
            result[i * 2] = (byte)(sample & 0xFF);
            result[(i * 2) + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return result;
    }

    private static float[] DecodePcm16(ReadOnlySpan<byte> bytes)
    {
        int count = bytes.Length / 2;
        float[] result = new float[count];
        for (int i = 0; i < count; i++)
        {
            short sample = (short)(bytes[i * 2] | (bytes[(i * 2) + 1] << 8));
            result[i] = sample / 32768f;
        }
        return result;
    }
}
