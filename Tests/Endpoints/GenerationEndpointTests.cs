using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Common;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Endpoints.Generation;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints;

/// <summary>Tests for generation payload shaping (reflection round-trip over every property) and stream semantics (terminal complete update, error handling, no image counting).</summary>
public class GenerationEndpointTests
{
    private static GenerationEndpoint Create(FakeSwarmHttpClient? http = null, FakeSwarmWebSocketClient? ws = null, string sessionKey = SwarmSessionKeys.Default)
        => new(http ?? new FakeSwarmHttpClient(), ws ?? new FakeSwarmWebSocketClient(), sessionKey);

    private static GenerationRequest ValidRequest() => new()
    {
        Prompt = "a cat",
        Model = "TestModel",
        Width = 1024,
        Height = 1024
    };

    #region Payload round-trip

    /// <summary>Every serializable GenerationRequest property must reach the payload under its exact wire name when set. This test makes the "silently dropped parameter" bug class impossible to reintroduce.</summary>
    [Fact]
    public void CreateGenerationPayload_EveryPropertyReachesTheWire()
    {
        GenerationRequest request = new() { Prompt = "p" };
        List<(PropertyInfo Property, string WireName)> serializable = [];
        foreach (PropertyInfo property in typeof(GenerationRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                continue;
            }
            JsonPropertyAttribute? attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
            Assert.True(attribute is { PropertyName.Length: > 0 }, $"Property {property.Name} has neither [JsonProperty] with a wire name nor [JsonIgnore] — it would silently use the C# name and likely be dropped by the server.");
            serializable.Add((property, attribute!.PropertyName!));
            property.SetValue(request, SampleValue(property.PropertyType));
        }
        // InitImageCreativity requires InitImage; Seed's sample must not be the omitted sentinel.
        request.InitImage = "data:image/png;base64,abc";
        request.Seed = 42;
        JObject payload = GenerationEndpoint.CreateGenerationPayload(request);
        foreach ((PropertyInfo property, string wireName) in serializable)
        {
            Assert.True(payload.ContainsKey(wireName), $"Property {property.Name} (wire name '{wireName}') did not reach the payload.");
        }
    }

    [Fact]
    public void CreateGenerationPayload_NullOptionalsAreOmitted()
    {
        JObject payload = GenerationEndpoint.CreateGenerationPayload(ValidRequest());
        foreach (string key in new[] { "fluxguidancescale", "sigmashift", "clipstopatlayer", "vaetilesize", "zeronegative", "quality", "ideogramaspectratio", "googleaspectratio", "grokaspectratio", "presets", "initimage", "seed", "negativeprompt" })
        {
            Assert.False(payload.ContainsKey(key), $"Unset optional '{key}' should be omitted from the payload.");
        }
    }

    [Fact]
    public void CreateGenerationPayload_ImagesAndBatchSizeAreDistinct()
    {
        GenerationRequest request = ValidRequest();
        request.Images = 4;
        request.BatchSize = 2;
        JObject payload = GenerationEndpoint.CreateGenerationPayload(request);
        Assert.Equal(4, payload["images"]!.Value<int>());
        Assert.Equal(2, payload["batchsize"]!.Value<int>());
    }

    [Fact]
    public void CreateGenerationPayload_LorasAreJsonArrays_CommaSafe_FullPrecision()
    {
        GenerationRequest request = ValidRequest();
        request.Loras =
        [
            new LoraModel { Name = "style, with comma", Weight = 0.85f },
            new LoraModel { Name = "detail-lora", Weight = 1.25f }
        ];
        JObject payload = GenerationEndpoint.CreateGenerationPayload(request);
        JArray loras = Assert.IsType<JArray>(payload["loras"]);
        JArray weights = Assert.IsType<JArray>(payload["loraweights"]);
        Assert.Equal(new[] { "style, with comma", "detail-lora" }, loras.Select(t => t.ToString()).ToArray());
        // 0.85 must stay 0.85 — the old F1 formatting turned it into 0.9.
        Assert.Equal(new[] { "0.85", "1.25" }, weights.Select(t => t.ToString()).ToArray());
    }

    [Fact]
    public void CreateGenerationPayload_RandomSeedIsOmitted_ExplicitSeedIsSent()
    {
        GenerationRequest request = ValidRequest();
        Assert.False(GenerationEndpoint.CreateGenerationPayload(request).ContainsKey("seed"));
        request.Seed = 123456789;
        Assert.Equal(123456789L, GenerationEndpoint.CreateGenerationPayload(request)["seed"]!.Value<long>());
    }

    private static object SampleValue(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual == typeof(string))
        {
            return "sample";
        }
        if (actual == typeof(int))
        {
            return 7;
        }
        if (actual == typeof(long))
        {
            return 42L;
        }
        if (actual == typeof(float))
        {
            return 1.5f;
        }
        if (actual == typeof(double))
        {
            return 2.5d;
        }
        if (actual == typeof(bool))
        {
            return true;
        }
        if (actual == typeof(List<string>))
        {
            return new List<string> { "entry" };
        }
        throw new NotSupportedException($"Add a sample value for {actual} to the test.");
    }

    #endregion

    #region Validation

    [Fact]
    public void StreamGeneration_ValidatesEagerly_AtCallTime()
    {
        GenerationEndpoint endpoint = Create();
        // No enumeration happens here — the throw must occur at the call itself.
        Assert.Throws<ArgumentNullException>(() => endpoint.StreamGenerationAsync(null!));
        Assert.Throws<ArgumentException>(() => endpoint.StreamGenerationAsync(new GenerationRequest { Prompt = " " }));
        Assert.Throws<ArgumentException>(() => endpoint.StreamGenerationAsync(new GenerationRequest { Prompt = "p", Images = 0 }));
        Assert.Throws<ArgumentException>(() => endpoint.StreamGenerationAsync(new GenerationRequest { Prompt = "p", Images = 10001 }));
        Assert.Throws<ArgumentException>(() => endpoint.StreamGenerationAsync(new GenerationRequest { Prompt = "p", BatchSize = 101 }));
        Assert.Throws<ArgumentException>(() => endpoint.StreamGenerationAsync(new GenerationRequest { Prompt = "p", Width = 0 }));
    }

    #endregion

    #region Stream semantics

    private static async Task<List<GenerationUpdate>> RunStreamAsync(IEnumerable<JObject> frames, string sessionKey = SwarmSessionKeys.Default)
    {
        FakeSwarmWebSocketClient ws = new() { FrameScript = (_, _) => frames };
        GenerationEndpoint endpoint = Create(ws: ws, sessionKey: sessionKey);
        List<GenerationUpdate> updates = [];
        await foreach (GenerationUpdate update in endpoint.StreamGenerationAsync(ValidRequest()))
        {
            updates.Add(update);
        }
        return updates;
    }

    [Fact]
    public async Task Stream_SocketIntentionClose_EmitsExactlyOneCompleteUpdate()
    {
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"status":{"waiting_gens":1,"live_gens":0,"loading_models":0,"waiting_backends":0}}"""),
            JObject.Parse("""{"gen_progress":{"batch_index":"0","overall_percent":0.5,"current_percent":0.2}}"""),
            JObject.Parse("""{"image":"View/local/raw/img.png","batch_index":"0","metadata":null}"""),
            JObject.Parse("""{"discard_indices":[]}"""),
            JObject.Parse("""{"socket_intention":"close"}"""),
            // Anything after socket_intention must be ignored (the server sends one final status frame).
            JObject.Parse("""{"status":{"waiting_gens":0,"live_gens":0}}""")
        ]);
        Assert.Equal(new[] { "status", "progress", "image", "discard", "complete" }, updates.Select(u => u.Type).ToArray());
        GenerationUpdate complete = updates[^1];
        Assert.True(complete.Completion!.Succeeded);
        Assert.Equal(1, complete.Completion.ImagesReceived);
        Assert.Empty(complete.Completion.Errors);
    }

    [Fact]
    public async Task Stream_ErrorFrame_MarksCompletionFailed_ButStreamStillCompletes()
    {
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"error":"Generation session interrupted."}"""),
            JObject.Parse("""{"discard_indices":[0,1]}"""),
            JObject.Parse("""{"socket_intention":"close"}""")
        ]);
        Assert.Equal(new[] { "error", "discard", "complete" }, updates.Select(u => u.Type).ToArray());
        CompletionInfo completion = updates[^1].Completion!;
        Assert.False(completion.Succeeded);
        Assert.Equal("Generation session interrupted.", Assert.Single(completion.Errors).Message);
        Assert.Equal(new[] { 0, 1 }, completion.DiscardedIndices.ToArray());
    }

    [Fact]
    public async Task Stream_NoImageCounting_ExtraAndNegativeIndicesDoNotHang()
    {
        // 3 images arrive for a 1-image request (grid composite -1 and an intermediate) — old counter logic would never terminate.
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"image":"a.png","batch_index":"0"}"""),
            JObject.Parse("""{"image":"b.png","batch_index":"-1"}"""),
            JObject.Parse("""{"image":"c.png","batch_index":"-10"}"""),
            JObject.Parse("""{"socket_intention":"close"}""")
        ]);
        Assert.Equal(3, updates.Count(u => u.Type == "image"));
        Assert.Equal(3, updates[^1].Completion!.ImagesReceived);
        Assert.True(updates[^1].Completion!.Succeeded);
    }

    [Fact]
    public async Task Stream_ServerClosedEarly_StillEmitsOneComplete()
    {
        // No socket_intention frame at all — server died. Exactly one complete, marked failed (zero images).
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"status":{"waiting_gens":1}}""")
        ]);
        Assert.Equal(new[] { "status", "complete" }, updates.Select(u => u.Type).ToArray());
        Assert.False(updates[^1].Completion!.Succeeded);
    }

    [Fact]
    public async Task Stream_CombinedKeysInOneFrame_EmitBothUpdates()
    {
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"image":"a.png","batch_index":"0","error":"partial failure"}"""),
            JObject.Parse("""{"socket_intention":"close"}""")
        ]);
        Assert.Contains(updates, u => u.Type == "image");
        Assert.Contains(updates, u => u.Type == "error");
    }

    [Fact]
    public async Task Stream_ImageInfo_ExposesDataUrlAndPathForms()
    {
        List<GenerationUpdate> updates = await RunStreamAsync(
        [
            JObject.Parse("""{"image":"data:image/png;base64,abc","batch_index":"0"}"""),
            JObject.Parse("""{"image":"View/local/raw/img.png","batch_index":"1"}"""),
            JObject.Parse("""{"socket_intention":"close"}""")
        ]);
        List<GenerationUpdate> images = updates.Where(u => u.Type == "image").ToList();
        Assert.True(images[0].Image!.IsDataUrl);
        Assert.False(images[1].Image!.IsDataUrl);
    }

    [Fact]
    public async Task Stream_UsesConfiguredSessionKey()
    {
        FakeSwarmWebSocketClient ws = new() { FrameScript = (_, _) => [JObject.Parse("""{"socket_intention":"close"}""")] };
        GenerationEndpoint endpoint = Create(ws: ws, sessionKey: "user-99");
        await foreach (GenerationUpdate _ in endpoint.StreamGenerationAsync(ValidRequest()))
        {
        }
        Assert.Equal("user-99", Assert.Single(ws.Streams).SessionKey);
    }

    #endregion

    #region HTTP operations

    [Fact]
    public async Task InterruptAll_SendsOtherSessionsFlag_OnConfiguredSessionKey()
    {
        FakeSwarmHttpClient http = new();
        GenerationEndpoint endpoint = Create(http: http, sessionKey: "user-7");
        await endpoint.InterruptAllAsync(otherSessions: false);
        (string ep, JObject? payload, string key) = Assert.Single(http.Requests);
        Assert.Equal("InterruptAll", ep);
        Assert.False(payload!["other_sessions"]!.Value<bool>());
        Assert.Equal("user-7", key);
    }

    #endregion
}
