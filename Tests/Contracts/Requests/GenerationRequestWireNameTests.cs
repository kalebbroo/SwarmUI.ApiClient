using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SwarmUI.ApiClient.Contracts.Requests;
using SwarmUI.ApiClient.Extensions.APIBackends.Contracts;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using SwarmUI.ApiClient.Extensions.HartsyInference.Contracts;
using SwarmUI.ApiClient.Extensions.LLMAssistant.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Contracts.Requests
{
    /// <summary>Enforces the rule stated at the top of <see cref="GenerationRequest"/>: every wire name it sends is
    /// one SwarmUI actually registers. SwarmUI drops names it does not recognise without erroring, so a typo costs a
    /// silently ignored parameter rather than a failure, and only a diff against the server's own list catches it.</summary>
    /// <remarks>Scans <see cref="GenerationRequest"/> itself plus every extension's own params type (everything
    /// reachable through <see cref="GenerationRequest.Extensions"/>) -- the compiler no longer rejects two
    /// properties on two different types claiming the same wire name the way one merged partial class did, so
    /// <see cref="NoTwoPropertiesClaimTheSameWireName"/> is now the only thing that catches that.</remarks>
    public class GenerationRequestWireNameTests
    {
        /// <summary>Names that are deliberately absent from the parameter registry.</summary>
        /// <remarks><c>loras</c> is serialized by <c>GenerationEndpoint.CreateGenerationPayload</c> in a post-pass
        /// that expands it into the parallel <c>loras</c> and <c>loraweights</c> arrays, and <c>presets</c> is a
        /// top-level field of the generation API rather than a registered parameter.</remarks>
        private static readonly HashSet<string> NotRegisteredParameters = new(StringComparer.Ordinal) { "presets" };

        /// <summary><see cref="GenerationRequest"/> and every type reachable through <see cref="GenerationRequest.Extensions"/> --
        /// together, the complete set of types a wire name can be declared on.</summary>
        private static readonly Type[] RequestAndExtensionParamTypes =
        [
            typeof(GenerationRequest),
            typeof(AudioLabGenerationParams),
            typeof(APIBackendsGenerationParams),
            typeof(HartsyInferenceGenerationParams),
            typeof(LLMAssistantGenerationParams)
        ];

        private static IReadOnlySet<string> LoadSnapshot()
        {
            IReadOnlySet<string> ids = SwarmParamRegistrySnapshot.ParameterIds;
            Assert.True(ids.Count > 0, "Parameter id snapshot is empty, so the embedded resource did not load");
            return ids;
        }

        private static IEnumerable<(string WireName, string PropertyName)> WireNames()
        {
            foreach (Type type in RequestAndExtensionParamTypes)
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    JsonPropertyAttribute? attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                    if (attribute?.PropertyName is not null)
                    {
                        yield return (attribute.PropertyName, property.Name);
                    }
                }
            }
        }

        [Fact]
        public void EveryWireName_IsRegisteredOnTheServer()
        {
            IReadOnlySet<string> registered = LoadSnapshot();
            List<string> unknown = [.. WireNames()
                .Where(entry => !registered.Contains(entry.WireName) && !NotRegisteredParameters.Contains(entry.WireName))
                .Select(entry => $"{entry.PropertyName} -> \"{entry.WireName}\"")
                .OrderBy(text => text, StringComparer.Ordinal)];
            Assert.True(unknown.Count == 0, "These wire names are not registered parameters, so the server would drop them:\n  " + string.Join("\n  ", unknown));
        }

        [Fact]
        public void NoTwoPropertiesClaimTheSameWireName()
        {
            List<string> duplicates = [.. WireNames()
                .GroupBy(entry => entry.WireName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => $"\"{group.Key}\" claimed by {string.Join(", ", group.Select(entry => entry.PropertyName))}")
                .OrderBy(text => text, StringComparer.Ordinal)];
            Assert.True(duplicates.Count == 0, "Duplicate wire names:\n  " + string.Join("\n  ", duplicates));
        }

        [Fact]
        public void PostPassProperties_CarryNoWireName()
        {
            PropertyInfo loras = typeof(GenerationRequest).GetProperty(nameof(GenerationRequest.Loras))!;
            Assert.Null(loras.GetCustomAttribute<JsonPropertyAttribute>());
        }

        [Fact]
        public void ExtensionsSlot_CarriesNoWireNameItself()
        {
            // Extensions is never a nested JSON object on the wire -- CreateGenerationPayload flattens its
            // slots' own properties in instead -- so the slot property itself must stay [JsonIgnore], not
            // [JsonProperty("extensions")] or similar.
            PropertyInfo extensions = typeof(GenerationRequest).GetProperty(nameof(GenerationRequest.Extensions))!;
            Assert.Null(extensions.GetCustomAttribute<JsonPropertyAttribute>());
            Assert.NotNull(extensions.GetCustomAttribute<JsonIgnoreAttribute>());
        }

        [Fact]
        public void EveryParameterWithAnOffValue_IsARegisteredParameter()
        {
            IReadOnlySet<string> registered = LoadSnapshot();
            List<string> unknown = [.. SwarmParamOffValues.All.Keys
                .Where(id => !registered.Contains(id))
                .OrderBy(id => id, StringComparer.Ordinal)];
            Assert.True(unknown.Count == 0, "Off-value table names parameters that are not registered:\n  " + string.Join("\n  ", unknown));
        }

        [Fact]
        public void OffValue_IsNotAlwaysTheDefault()
        {
            Assert.Equal("0", SwarmParamOffValues.All["maskblur"]);
            Assert.Equal("(Use Base)", SwarmParamOffValues.All["refinermodel"]);
            Assert.True(SwarmParamOffValues.IsOff("refinerupscale", "1"));
            Assert.False(SwarmParamOffValues.IsOff("refinerupscale", "1.5"));
            Assert.False(SwarmParamOffValues.IsOff("steps", "0"));
        }

        [Fact]
        public void EveryTextToAudioParameter_IsCarried()
        {
            HashSet<string> carried = [.. WireNames().Select(entry => entry.WireName)];
            string[] required = ["textaudioduration", "textaudiostyle", "textaudiobpm", "textaudiokeyscale", "textaudiotimesignature", "textaudiolanguage", "audioformat"];
            List<string> absent = [.. required.Where(id => !carried.Contains(id))];
            Assert.True(absent.Count == 0, "The stock Text2Audio group is the whole music contract, and these are not carried: " + string.Join(", ", absent));
        }

        [Fact]
        public void Snapshot_ContainsExtensionRegisteredParameters()
        {
            IReadOnlySet<string> registered = LoadSnapshot();
            foreach (string id in new[] { "acousticsteps", "grokaspectratio", "refinermodel", "vae" })
            {
                Assert.True(registered.Contains(id), $"Snapshot is missing '{id}', so it was captured without the extensions loaded and this suite would pass vacuously.");
            }
        }
    }
}

