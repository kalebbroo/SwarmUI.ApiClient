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

namespace SwarmUI.ApiClient.Tests
{
    /// <summary>Capture tool for <c>Tests/Fixtures/wire/*.json</c>, the golden fixtures
    /// <see cref="SwarmUI.ApiClient.Tests.Contracts.Requests.GenerationRequestWireCompatibilityTests"/> checks
    /// against. Not a regression test -- its one <see cref="Fact"/> is permanently <see cref="FactAttribute.Skip"/>'d
    /// so it never runs in CI, and it is committed (rather than deleted after use) so the fixtures' provenance is
    /// a real, buildable file instead of a claim in a comment.</summary>
    /// <remarks>
    /// <para><b>Provenance of the fixtures already on disk:</b> they were produced by an equivalent capture
    /// script run against commit <c>07a8300</c> -- the last commit before the GenerationRequest/extension-params
    /// restructuring, where every property this class's scenarios set was still declared directly on the
    /// single, partial-class <see cref="GenerationRequest"/> (eg <c>request.AudioOutputFormat</c> rather than
    /// <c>request.Extensions.AudioLab.AudioOutputFormat</c>) -- in an isolated worktree, then copied into this
    /// tree's <c>Tests/Fixtures/wire/</c> before being committed. This file is written against *today's* composed
    /// shape instead, so it actually compiles and runs at HEAD rather than only existing as an artifact from a
    /// commit that can no longer build; <see cref="SwarmUI.ApiClient.Tests.Contracts.Requests.GenerationRequestWireCompatibilityTests"/>
    /// is the independent proof that the two shapes produce byte-identical JSON, which is exactly why running this
    /// tool today reproduces the same fixtures the pre-refactor script originally captured.</para>
    /// <para><b>To (re)capture:</b> remove the <see cref="FactAttribute.Skip"/> below, run
    /// <c>dotnet test --filter "FullyQualifiedName~_FixtureCapture"</c>, check the resulting
    /// <c>Tests/Fixtures/wire/*.json</c> diff is the change you intended (a deliberate, reviewed wire-format
    /// change -- never anything else), then put <see cref="FactAttribute.Skip"/> back. This method is a tool
    /// invoked deliberately, not something that should ever run unattended.</para>
    /// </remarks>
    public class _FixtureCapture
    {
        private static string FixturesDir([CallerFilePath] string here = "")
            => Path.Combine(Path.GetDirectoryName(here)!, "Fixtures", "wire");

        private static void Write(string name, GenerationRequest request)
        {
            JObject payload = GenerationEndpoint.CreateGenerationPayload(request);
            File.WriteAllText(Path.Combine(FixturesDir(), name + ".json"), Canonicalize(payload));
        }

        /// <summary>Recursively sorts object keys so the on-disk form is stable regardless of declaration/merge order.</summary>
        internal static string Canonicalize(JToken token) => Sort(token).ToString(Formatting.Indented);

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

        private static object SampleValue(Type type)
        {
            Type actual = Nullable.GetUnderlyingType(type) ?? type;
            if (actual == typeof(string)) return "sample";
            if (actual == typeof(int)) return 7;
            if (actual == typeof(long)) return 42L;
            if (actual == typeof(float)) return 1.5f;
            if (actual == typeof(double)) return 2.5d;
            if (actual == typeof(bool)) return true;
            if (actual == typeof(List<string>)) return new List<string> { "entry" };
            throw new NotSupportedException($"Add a sample value for {actual}.");
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

        [Fact(Skip = "Capture tool, not a regression test. See the class remarks for how and when to run it " +
            "(with this Skip removed) to (re)generate Tests/Fixtures/wire/*.json.")]
        public void CaptureGoldenFixtures()
        {
            Directory.CreateDirectory(FixturesDir());

            Write("core-minimal", new GenerationRequest { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 });

            Write("core-rich", new GenerationRequest
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
            });

            GenerationRequest withAudioLab = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            withAudioLab.Extensions.AudioLab.AudioOutputFormat = "wav_16";
            withAudioLab.Extensions.AudioLab.AudioQuality = "high";
            withAudioLab.Extensions.AudioLab.AceGuidance = 7.5f;
            withAudioLab.Extensions.AudioLab.MusicStyle = "upbeat indie pop";
            withAudioLab.Extensions.AudioLab.MaxDuration = 30f;
            withAudioLab.Extensions.AudioLab.ReferenceAudio = "data:audio/wav;base64,abc";
            withAudioLab.Extensions.AudioLab.Yue2Guidance = 1.2f;
            withAudioLab.Extensions.AudioLab.HeartLibCfgScale = 1.5f;
            Write("with-audiolab", withAudioLab);

            GenerationRequest withApiBackends = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            withApiBackends.Extensions.APIBackends.SafetyTolerance = 2;
            withApiBackends.Extensions.APIBackends.OutputFormat = "png";
            withApiBackends.Extensions.APIBackends.KlingVideoDuration = "5";
            withApiBackends.Extensions.APIBackends.VeoVideoAspectRatio = "16:9";
            withApiBackends.Extensions.APIBackends.IdeogramAspectRatio = "1:1";
            withApiBackends.Extensions.APIBackends.GoogleAspectRatio = "16:9";
            withApiBackends.Extensions.APIBackends.OpenAIQuality = "high";
            Write("with-apibackends", withApiBackends);

            GenerationRequest withHartsyInference = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            withHartsyInference.Extensions.HartsyInference.CfgRescale = 0.7f;
            withHartsyInference.Extensions.HartsyInference.InitImageMode = "reference";
            withHartsyInference.Extensions.HartsyInference.RestoreModel = "seedvr2-3b";
            withHartsyInference.Extensions.HartsyInference.VramMode = "Balanced";
            withHartsyInference.Extensions.HartsyInference.AnimateReferenceImage = "data:image/png;base64,abc";
            withHartsyInference.Extensions.HartsyInference.AceStepSourceAudio = "data:audio/wav;base64,abc";
            withHartsyInference.Extensions.HartsyInference.FaceIDV2Weight = 1.0f;
            Write("with-hartsyinference", withHartsyInference);

            GenerationRequest withLlmAssistant = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            withLlmAssistant.Extensions.LLMAssistant.LlmUseCache = true;
            withLlmAssistant.Extensions.LLMAssistant.LlmGenerateWildcardSeed = false;
            withLlmAssistant.Extensions.LLMAssistant.LlmModelId = "default";
            withLlmAssistant.Extensions.LLMAssistant.LlmInstructions = "prompt";
            withLlmAssistant.Extensions.LLMAssistant.LlmAssistantId = "jarvis";
            Write("with-llmassistant", withLlmAssistant);

            GenerationRequest withAllExtensions = new() { Prompt = "a cat", Model = "TestModel", Width = 1024, Height = 1024 };
            withAllExtensions.Extensions.AudioLab.AudioOutputFormat = "wav_16";
            withAllExtensions.Extensions.AudioLab.MaxDuration = 30f;
            withAllExtensions.Extensions.APIBackends.SafetyTolerance = 2;
            withAllExtensions.Extensions.APIBackends.KlingVideoDuration = "5";
            withAllExtensions.Extensions.HartsyInference.CfgRescale = 0.7f;
            withAllExtensions.Extensions.HartsyInference.FaceIDV2Weight = 1.0f;
            withAllExtensions.Extensions.LLMAssistant.LlmUseCache = true;
            withAllExtensions.Extensions.LLMAssistant.LlmAssistantId = "jarvis";
            Write("with-all-extensions", withAllExtensions);

            // Exhaustive: every serializable property on GenerationRequest plus every extension slot, set at once.
            GenerationRequest exhaustive = new() { Prompt = "p" };
            FillAllSerializableProperties(exhaustive);
            FillAllSerializableProperties(exhaustive.Extensions.AudioLab);
            FillAllSerializableProperties(exhaustive.Extensions.APIBackends);
            FillAllSerializableProperties(exhaustive.Extensions.HartsyInference);
            FillAllSerializableProperties(exhaustive.Extensions.LLMAssistant);
            exhaustive.InitImage = "data:image/png;base64,abc";
            exhaustive.Seed = 42;
            Write("exhaustive-every-property", exhaustive);
        }
    }
}
