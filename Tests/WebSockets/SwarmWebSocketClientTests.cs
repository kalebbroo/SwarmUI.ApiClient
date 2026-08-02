using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Exceptions;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;
using Xunit;

namespace SwarmUI.ApiClient.Tests.WebSockets;

/// <summary>Unit tests for the real SwarmWebSocketClient via the scripted IClientWebSocket seam.</summary>
public class SwarmWebSocketClientTests
{
    private static SwarmClientOptions Options() => new()
    {
        BaseUrl = "http://localhost:7801",
        MaxRetryAttempts = 0,
        RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        SessionRefreshCap = 3,
        WebSocketReceiveTimeout = TimeSpan.FromSeconds(5)
    };

    private static async Task<List<JObject>> CollectAsync(IAsyncEnumerable<JObject> frames, CancellationToken ct = default)
    {
        List<JObject> collected = [];
        await foreach (JObject frame in frames.WithCancellation(ct))
        {
            collected.Add(frame);
        }
        return collected;
    }

    [Fact]
    public async Task StreamFrames_YieldsFramesAndStopsOnClose()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"status":{"waiting_gens":1}}"""),
            new FakeClientWebSocket.TextFrame("""{"image":"View/local/img.png","batch_index":"0"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocketFactory factory = new(socket);
        FakeSessionManager sessions = new();
        SwarmWebSocketClient client = new(Options(), sessions, factory);
        List<JObject> frames = await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", []));
        Assert.Equal(2, frames.Count);
        Assert.NotNull(frames[0]["status"]);
        Assert.Equal("View/local/img.png", frames[1]["image"]?.ToString());
        Assert.True(socket.Disposed);
    }

    [Fact]
    public async Task StreamFrames_InjectsSessionIdIntoPayload()
    {
        FakeClientWebSocket socket = new([new FakeClientWebSocket.CloseFrame()]);
        FakeClientWebSocketFactory factory = new(socket);
        FakeSessionManager sessions = new();
        SwarmWebSocketClient client = new(Options(), sessions, factory);
        await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", new JObject { ["prompt"] = "cat" }, "user-42"));
        JObject sent = JObject.Parse(Assert.Single(socket.SentMessages));
        Assert.Equal("cat", sent["prompt"]?.ToString());
        Assert.False(string.IsNullOrEmpty(sent["session_id"]?.ToString()));
    }

    [Fact]
    public async Task StreamFrames_DoesNotMutateCallerPayload()
    {
        FakeClientWebSocket socket = new([new FakeClientWebSocket.CloseFrame()]);
        FakeClientWebSocketFactory factory = new(socket);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), factory);
        JObject payload = new() { ["prompt"] = "cat" };
        await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", payload));
        Assert.False(payload.ContainsKey("session_id"));
    }

    [Fact]
    public async Task StreamFrames_InvalidSession_RefreshesAndReconnects()
    {
        // First socket: server rejects the session with one error frame then close (the documented server behavior).
        FakeClientWebSocket rejected = new([
            new FakeClientWebSocket.TextFrame("""{"error":"Invalid session ID. You may need to refresh the page.","error_id":"invalid_session_id"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocket accepted = new([
            new FakeClientWebSocket.TextFrame("""{"status":{"waiting_gens":0}}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocketFactory factory = new(rejected, accepted);
        FakeSessionManager sessions = new();
        SwarmWebSocketClient client = new(Options(), sessions, factory);
        List<JObject> frames = await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", [], "user-1"));
        Assert.Single(frames);
        Assert.NotNull(frames[0]["status"]);
        // The stale session must have been invalidated with its observed id (CAS), then a new one created.
        (string key, string? observed) = Assert.Single(sessions.Invalidations);
        Assert.Equal("user-1", key);
        Assert.NotNull(observed);
        Assert.Equal(2, sessions.CreateCount);
        Assert.True(rejected.Disposed);
        // The two connection attempts must have used different session ids.
        string firstSession = JObject.Parse(rejected.SentMessages[0])["session_id"]!.ToString();
        string secondSession = JObject.Parse(accepted.SentMessages[0])["session_id"]!.ToString();
        Assert.NotEqual(firstSession, secondSession);
    }

    [Fact]
    public async Task StreamFrames_InvalidSession_GivesUpAfterRefreshCap()
    {
        static FakeClientWebSocket Rejecting() => new([
            new FakeClientWebSocket.TextFrame("""{"error":"bad","error_id":"invalid_session_id"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        FakeClientWebSocketFactory factory = new(Rejecting(), Rejecting(), Rejecting(), Rejecting());
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), factory);
        await Assert.ThrowsAsync<SwarmSessionException>(async () => await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", [])));
        // SessionRefreshCap = 3 → exactly 3 connection attempts, no more.
        Assert.Equal(3, factory.Created.Count);
    }

    [Fact]
    public async Task StreamFrames_KeepAliveFramesAreConsumed()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"keep_alive":true}"""),
            new FakeClientWebSocket.TextFrame("""{"status":{"waiting_gens":0}}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        List<JObject> frames = await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", []));
        Assert.Single(frames);
        Assert.NotNull(frames[0]["status"]);
    }

    [Fact]
    public async Task StreamFrames_CancellationPropagatesAsOperationCanceled()
    {
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"status":{"waiting_gens":1}}"""),
            new FakeClientWebSocket.HangStep()
        ]);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        using CancellationTokenSource cts = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (JObject frame in client.StreamFramesAsync("GenerateText2ImageWS", [], cancellationToken: cts.Token))
            {
                cts.Cancel();
            }
        });
        Assert.True(socket.Disposed);
    }

    [Fact]
    public async Task StreamFrames_ReceiveTimeoutThrowsWebSocketException()
    {
        SwarmClientOptions options = Options();
        options.WebSocketReceiveTimeout = TimeSpan.FromMilliseconds(50);
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame("""{"status":{}}"""),
            new FakeClientWebSocket.HangStep()
        ]);
        SwarmWebSocketClient client = new(options, new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        SwarmWebSocketException ex = await Assert.ThrowsAsync<SwarmWebSocketException>(async () => await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", [])));
        Assert.Contains("timed out", ex.Message);
    }

    [Fact]
    public async Task StreamFrames_MalformedFrameIsSkipped_ThreeInARowAborts()
    {
        FakeClientWebSocket skipsOne = new([
            new FakeClientWebSocket.TextFrame("""{"status":{}}"""),
            new FakeClientWebSocket.TextFrame("not json"),
            new FakeClientWebSocket.TextFrame("""{"image":"data:image/png;base64,abc","batch_index":"0"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(skipsOne));
        List<JObject> frames = await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", []));
        Assert.Equal(2, frames.Count);

        FakeClientWebSocket threeBad = new([
            new FakeClientWebSocket.TextFrame("""{"status":{}}"""),
            new FakeClientWebSocket.TextFrame("bad1"),
            new FakeClientWebSocket.TextFrame("bad2"),
            new FakeClientWebSocket.TextFrame("bad3"),
            new FakeClientWebSocket.CloseFrame()
        ]);
        SwarmWebSocketClient client2 = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(threeBad));
        await Assert.ThrowsAsync<SwarmWebSocketException>(async () => await CollectAsync(client2.StreamFramesAsync("GenerateText2ImageWS", [])));
    }

    [Fact]
    public async Task StreamFrames_MultiPartMessageIsReassembled()
    {
        // Frame bigger than the 16KB read buffer forces multi-read reassembly.
        string bigValue = new('x', 100_000);
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame($$"""{"image":"{{bigValue}}","batch_index":"0"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        SwarmWebSocketClient client = new(Options(), new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        List<JObject> frames = await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", []));
        Assert.Equal(bigValue, Assert.Single(frames)["image"]?.ToString());
    }

    [Fact]
    public async Task StreamFrames_OversizeMessageAborts()
    {
        SwarmClientOptions options = Options();
        options.MaxWebSocketMessageBytes = 1024;
        FakeClientWebSocket socket = new([
            new FakeClientWebSocket.TextFrame($$"""{"image":"{{new string('x', 10_000)}}"}"""),
            new FakeClientWebSocket.CloseFrame()
        ]);
        SwarmWebSocketClient client = new(options, new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        SwarmWebSocketException ex = await Assert.ThrowsAsync<SwarmWebSocketException>(async () => await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", [])));
        Assert.Contains("byte limit", ex.Message);
    }

    [Fact]
    public async Task StreamFrames_HonorsConfiguredAuthorizationHeaderName()
    {
        SwarmClientOptions options = Options();
        options.Authorization = "secret-token";
        options.AuthorizationHeaderName = "X-Custom-Auth";
        FakeClientWebSocket socket = new([new FakeClientWebSocket.CloseFrame()]);
        SwarmWebSocketClient client = new(options, new FakeSessionManager(), new FakeClientWebSocketFactory(socket));
        await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", []));
        Assert.Equal("secret-token", socket.Headers["X-Custom-Auth"]);
        Assert.False(socket.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task StreamFrames_ConnectFailureRetriesThenSurfacesInnerException()
    {
        SwarmClientOptions options = Options();
        options.MaxRetryAttempts = 1;
        FakeClientWebSocket failing1 = new([]) { ConnectException = new WebSocketException(WebSocketError.Faulted, "boom") };
        FakeClientWebSocket failing2 = new([]) { ConnectException = new WebSocketException(WebSocketError.Faulted, "boom") };
        FakeClientWebSocketFactory factory = new(failing1, failing2);
        SwarmWebSocketClient client = new(options, new FakeSessionManager(), factory);
        SwarmWebSocketException ex = await Assert.ThrowsAsync<SwarmWebSocketException>(async () => await CollectAsync(client.StreamFramesAsync("GenerateText2ImageWS", [])));
        Assert.IsType<WebSocketException>(ex.InnerException);
    }
}
