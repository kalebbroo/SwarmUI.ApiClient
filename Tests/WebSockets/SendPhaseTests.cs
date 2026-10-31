using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.WebSockets;
using Xunit;

namespace SwarmUI.ApiClient.Tests.WebSockets;

/// <summary>Every stream failure says how far the request got, so a caller knows whether the server may have started the work.</summary>
public class SendPhaseTests
{
    private static SwarmClientOptions Options(TimeSpan? connectTimeout = null) => new()
    {
        BaseUrl = "http://localhost:7801",
        MaxRetryAttempts = 0,
        RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        SessionRefreshCap = 2,
        WebSocketReceiveTimeout = TimeSpan.FromSeconds(5),
        WebSocketConnectTimeout = connectTimeout
    };

    private static async Task<SwarmException> FailureOf(FakeClientWebSocket[] sockets, SwarmClientOptions? options = null, Action<JObject>? onFrame = null, FakeSessionManager? sessions = null)
    {
        SwarmWebSocketClient client = new(options ?? Options(), sessions ?? new FakeSessionManager(), new FakeClientWebSocketFactory(sockets));
        return await Assert.ThrowsAnyAsync<SwarmException>(async () =>
        {
            await foreach (JObject frame in client.StreamFramesAsync("GenerateText2ImageWS", new JObject { ["prompt"] = "cat" }))
            {
                onFrame?.Invoke(frame);
            }
        });
    }

    [Fact]
    public async Task AConnectThatFailsIsNotInvoked()
    {
        FakeClientWebSocket socket = new([]) { ConnectException = new WebSocketException("refused") };
        SwarmException failure = await FailureOf([socket]);
        Assert.Equal(SwarmSendPhase.NotInvoked, failure.SendPhase);
        Assert.Empty(socket.SentMessages);
    }

    [Fact]
    public async Task ASendThatFailsIsUnknown()
    {
        FakeClientWebSocket socket = new([]) { SendException = new WebSocketException("reset") };
        Assert.Equal(SwarmSendPhase.SendUnknown, (await FailureOf([socket])).SendPhase);
    }

    [Fact]
    public async Task ADropBeforeTheServerAnswersIsUnknown()
    {
        FakeClientWebSocket socket = new([new FakeClientWebSocket.ThrowStep(new WebSocketException("reset"))]);
        Assert.Equal(SwarmSendPhase.SendUnknown, (await FailureOf([socket])).SendPhase);
    }

    [Fact]
    public async Task ADropAfterTheServerAnsweredIsSent()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"status":{"waiting_gens":1}}"""),
            new FakeClientWebSocket.ThrowStep(new WebSocketException("reset"))
        ]);
        int frames = 0;
        SwarmException failure = await FailureOf([socket], onFrame: _ => frames++);
        Assert.Equal(1, frames);
        Assert.Equal(SwarmSendPhase.Sent, failure.SendPhase);
    }

    [Fact]
    public async Task ASessionTheServerKeepsRejectingIsNotInvoked()
    {
        SwarmException failure = await FailureOf([Rejected(), Rejected()]);
        Assert.IsType<SwarmSessionException>(failure);
        Assert.Equal(SwarmSendPhase.NotInvoked, failure.SendPhase);
    }

    [Fact]
    public async Task AConnectPastItsTimeoutFailsAsATimeoutAndIsNotInvoked()
    {
        FakeClientWebSocket socket = new([]) { ConnectHangs = true };
        SwarmException failure = await FailureOf([socket], Options(TimeSpan.FromMilliseconds(50))).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsType<SwarmWebSocketException>(failure);
        Assert.IsType<TimeoutException>(failure.InnerException);
        Assert.Equal(SwarmSendPhase.NotInvoked, failure.SendPhase);
        Assert.True(socket.Disposed);
    }

    [Fact]
    public async Task WithoutAConnectTimeoutOnlyTheCallerEndsAHungConnect()
    {
        FakeClientWebSocket socket = new([]) { ConnectHangs = true };
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        using CancellationTokenSource cancel = new(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (JObject _ in client.StreamFramesAsync("GenerateText2ImageWS", [], cancellationToken: cancel.Token))
            {
            }
        });
    }

    [Fact]
    public async Task ATimedOutConnectIsRetriedWhenRetriesAreAllowed()
    {
        FakeClientWebSocket hung = new([]) { ConnectHangs = true };
        FakeClientWebSocket answering = new([new FakeClientWebSocket.TextFrame("""{"status":{}}"""), new FakeClientWebSocket.CloseFrame()]);
        SwarmClientOptions options = Options(TimeSpan.FromMilliseconds(50));
        options.MaxRetryAttempts = 1;
        SwarmWebSocketClient client = new(options, new FakeSessionManager(), new FakeClientWebSocketFactory(hung, answering));
        List<JObject> frames = [];
        await foreach (JObject frame in client.StreamFramesAsync("GenerateText2ImageWS", []))
        {
            frames.Add(frame);
        }
        Assert.Single(frames);
        Assert.True(hung.Disposed);
    }

    [Fact]
    public async Task ASessionThatCannotBeCreatedIsNotInvoked()
    {
        FakeSessionManager sessions = new() { CreateThrows = new SwarmSessionException("server down") };
        SwarmException failure = await FailureOf([], sessions: sessions);
        Assert.Equal(SwarmSendPhase.NotInvoked, failure.SendPhase);
    }

    private static FakeClientWebSocket Rejected() => new([new FakeClientWebSocket.TextFrame("""{"error_id":"invalid_session_id","error":"bad session"}""")]);

    [Fact]
    public async Task AConnectFailureAfterARejectedSessionIsStillNotInvoked()
    {
        FakeClientWebSocket refused = new([]) { ConnectException = new WebSocketException("refused") };
        Assert.Equal(SwarmSendPhase.NotInvoked, (await FailureOf([Rejected(), refused])).SendPhase);
    }

    [Fact]
    public async Task ASendFailureAfterARejectedSessionIsUnknown()
    {
        FakeClientWebSocket reset = new([]) { SendException = new WebSocketException("reset") };
        Assert.Equal(SwarmSendPhase.SendUnknown, (await FailureOf([Rejected(), reset])).SendPhase);
    }

    [Fact]
    public async Task AStreamClosedWithoutAnErrorThrowsNothing()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"status":{}}"""),
            new FakeClientWebSocket.ThrowStep(new WebSocketException(WebSocketError.ConnectionClosedPrematurely))
        ]);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        List<JObject> frames = [];
        await foreach (JObject frame in client.StreamFramesAsync("GenerateText2ImageWS", []))
        {
            frames.Add(frame);
        }
        Assert.Single(frames);
    }
}
