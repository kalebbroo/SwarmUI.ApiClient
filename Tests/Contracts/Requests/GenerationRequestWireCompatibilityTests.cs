using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Endpoints.Generation;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Contracts.Requests
{
    /// <summary>Proves the extension-parameter restructuring (moving AudioLab/APIBackends/HartsyInference/
    /// LLMAssistant parameters off <see cref="GenerationRequest"/> and onto
    /// <c>GenerationRequestExtensionParams</c>) left the wire format unchanged.</summary>
    /// <remarks>Each scenario here is built with TODAY's composed shape (<c>request.Extensions.X.Foo</c>) and
    /// compared against a fixture captured from the PRE-refactor shape (<c>request.Foo</c> directly on the
    /// merged partial class, at commit 07a8300). See <see cref="SwarmUI.ApiClient.Tests._FixtureCapture"/> for
    /// the capture tool and exactly how/when the fixtures were produced. A fixture mismatch here means the
    /// restructuring changed what reaches the server, not just how the C# is organized.</remarks>
    public class GenerationRequestWireCompatibilityTests
    {
        private static string FixturesDir([CallerFilePath] string here = "")
            => Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Fixtures", "wire");

        private static string LoadFixture(string name) => File.ReadAllText(Path.Combine(FixturesDir(), name + ".json"));

        /// <summary>Recursively sorts object keys. Declaration/merge order is not part of the wire contract
        /// (SwarmUI reads by key), so this is what makes the "same JSON" comparison meaningful rather than
        /// accidentally order-sensitive.</summary>
        private static JToken Sort(JToken token)
        {
            if (token is JObject obj)
            {
                JObject result = new();
                foreach (JProperty prop in obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    result.Add(prop.Name, Sort(prop.Value));
                }
                return result;
            }
            if (token is JArray arr)
            {
                return new JArray(arr.Select(Sort));
            }
            return token;
        }

        private static string Canonicalize(JToken token) => Sort(token).ToString(Formatting.Indented);

        private static void AssertMatchesFixture(string fixtureName, GenerationRequest request)
        {
            JObject actual = GenerationEndpoint.CreateGenerationPayload(request);
            JObject golden = JObject.Parse(LoadFixture(fixtureName));
            string actualCanonical = Canonicalize(actual);
            string goldenCanonical = Canonicalize(golden);
            // Reparse both sides from their canonical text before DeepEquals, not just before the string
            // assert below: `actual` holds float-typed values (eg CfgRescale) straight from C#, while `golden`
            // was parsed from JSON text and so holds the nearest *double* for the same decimal literal -- for a
            // value with no exact binary representation (0.7, 0.4, 0.3, ...) those two in-memory numbers differ
            // in their last bits even though both stringify to "0.7". Parsing `actual`'s own canonical text back
            // into a JToken puts it through the identical double-parse golden already went through, so the
            // comparison is never sensitive to a precision difference that has nothing to do with the wire
            // format -- the server only ever sees the string either way.
            Assert.True(JToken.DeepEquals(JObject.Parse(goldenCanonical), JObject.Parse(actualCanonical)), $"{fixtureName}: payload no longer matches the pre-refactor fixture.\nExpected:\n{goldenCanonical}\nActual:\n{actualCanonical}");
            Assert.Equal(goldenCanonical, actualCanonical);
        }

        [Fact]
        public void CoreMinimal_MatchesPreRefactorFixture()
        {
            AssertMatchesFixture("core-minimal", new GenerationRequest { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 });
        }

        [Fact]
        public void CoreRich_MatchesPreRefactorFixture()
        {
            GenerationRequest request = new()
            {
                Prompt = "a cat",
                NegativePrompt = "blurry",
                Model = "TestModel",
                Width = 1024,
                Height = 1024,
                Seed = 123456,
                Presets = ["myPreset"],
                InitImage = "data:image/png;base64,abc",
                InitImageCreativity = 0.4f,
                RefinerModel = "refiner.safetensors",
                RefinerSteps = 10,
                RefinerControlPercentage = 0.3f,
                ControlNetModel = "canny.safetensors",
                ControlNetStrength = 0.8f,
                VideoFrames = 25,
                VideoFps = 24,
                Loras =
                [
                    new LoraModel { Name = "style, with comma", Weight = 0.85f },
                    new LoraModel { Name = "detail", Weight = 1.25f }
                ]
            };
            AssertMatchesFixture("core-rich", request);
        }

        [Fact]
        public void WithAudioLab_MatchesPreRefactorFixture()
        {
            GenerationRequest request = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            request.Extensions.AudioLab.AudioOutputFormat = "wav_16";
            request.Extensions.AudioLab.AudioQuality = "high";
            request.Extensions.AudioLab.AceGuidance = 7.5f;
            request.Extensions.AudioLab.MusicStyle = "upbeat indie pop";
            request.Extensions.AudioLab.MaxDuration = 30f;
            request.Extensions.AudioLab.ReferenceAudio = "data:audio/wav;base64,abc";
            request.Extensions.AudioLab.Yue2Guidance = 1.2f;
            request.Extensions.AudioLab.HeartLibCfgScale = 1.5f;
            AssertMatchesFixture("with-audiolab", request);
        }

        [Fact]
        public void WithAPIBackends_MatchesPreRefactorFixture()
        {
            GenerationRequest request = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            request.Extensions.APIBackends.SafetyTolerance = 2;
            request.Extensions.APIBackends.OutputFormat = "png";
            request.Extensions.APIBackends.KlingVideoDuration = "5";
            request.Extensions.APIBackends.VeoVideoAspectRatio = "16:9";
            request.Extensions.APIBackends.IdeogramAspectRatio = "1:1";
            request.Extensions.APIBackends.GoogleAspectRatio = "16:9";
            request.Extensions.APIBackends.OpenAIQuality = "high";
            AssertMatchesFixture("with-apibackends", request);
        }

        [Fact]
        public void WithHartsyInference_MatchesPreRefactorFixture()
        {
            GenerationRequest request = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            request.Extensions.HartsyInference.CfgRescale = 0.7f;
            request.Extensions.HartsyInference.InitImageMode = "reference";
            request.Extensions.HartsyInference.RestoreModel = "seedvr2-3b";
            request.Extensions.HartsyInference.VramMode = "Balanced";
            request.Extensions.HartsyInference.AnimateReferenceImage = "data:image/png;base64,abc";
            request.Extensions.HartsyInference.AceStepSourceAudio = "data:audio/wav;base64,abc";
            request.Extensions.HartsyInference.FaceIDV2Weight = 1.0f;
            AssertMatchesFixture("with-hartsyinference", request);
        }

        [Fact]
        public void WithLLMAssistant_MatchesPreRefactorFixture()
        {
            GenerationRequest request = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            request.Extensions.LLMAssistant.LlmUseCache = true;
            request.Extensions.LLMAssistant.LlmGenerateWildcardSeed = false;
            request.Extensions.LLMAssistant.LlmModelId = "default";
            request.Extensions.LLMAssistant.LlmInstructions = "prompt";
            request.Extensions.LLMAssistant.LlmAssistantId = "jarvis";
            AssertMatchesFixture("with-llmassistant", request);
        }

        [Fact]
        public void WithAllExtensionsAtOnce_MatchesPreRefactorFixture()
        {
            // Proves the slot shape composes: several extensions set simultaneously on one request still
            // flattens to one non-colliding payload, matching a fixture captured the same way pre-refactor.
            GenerationRequest request = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            request.Extensions.AudioLab.AudioOutputFormat = "wav_16";
            request.Extensions.AudioLab.MaxDuration = 30f;
            request.Extensions.APIBackends.SafetyTolerance = 2;
            request.Extensions.APIBackends.KlingVideoDuration = "5";
            request.Extensions.HartsyInference.CfgRescale = 0.7f;
            request.Extensions.HartsyInference.FaceIDV2Weight = 1.0f;
            request.Extensions.LLMAssistant.LlmUseCache = true;
            request.Extensions.LLMAssistant.LlmAssistantId = "jarvis";
            AssertMatchesFixture("with-all-extensions", request);
        }

        [Fact]
        public void Exhaustive_EveryPropertySet_MatchesPreRefactorFixture()
        {
            // Mirrors GenerationEndpointTests.CreateGenerationPayload_EveryPropertyReachesTheWire's own fill
            // logic, so this walks the exact same (id, type) surface the fixture was captured from.
            GenerationRequest request = new() { Prompt = "p" };
            FillAllSerializableProperties(request);
            FillAllSerializableProperties(request.Extensions.AudioLab);
            FillAllSerializableProperties(request.Extensions.APIBackends);
            FillAllSerializableProperties(request.Extensions.HartsyInference);
            FillAllSerializableProperties(request.Extensions.LLMAssistant);
            request.InitImage = "data:image/png;base64,abc";
            request.Seed = 42;

            AssertMatchesFixture("exhaustive-every-property", request);
        }

        private static void FillAllSerializableProperties(object target)
        {
            foreach (PropertyInfo property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                {
                    continue;
                }
                JsonPropertyAttribute? attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (attribute?.PropertyName is not { Length: > 0 })
                {
                    continue;
                }
                property.SetValue(target, SampleValue(property.PropertyType));
            }
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
            throw new NotSupportedException($"Add a sample value for {actual} to this test.");
        }
    }
}
