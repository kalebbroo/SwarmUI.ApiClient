using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SwarmUI.ApiClient.Contracts.Requests;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Contracts.Requests
{
    /// <summary>Enforces the rule stated at the top of <see cref="GenerationRequest"/>: every wire name it sends is
    /// one SwarmUI actually registers. SwarmUI drops names it does not recognise without erroring, so a typo costs a
    /// silently ignored parameter rather than a failure, and only a diff against the server's own list catches it.</summary>
    public class GenerationRequestWireNameTests
    {
        /// <summary>Names that are deliberately absent from the parameter registry.</summary>
        /// <remarks><c>loras</c> is serialized by <c>GenerationEndpoint.CreateGenerationPayload</c> in a post-pass
        /// that expands it into the parallel <c>loras</c> and <c>loraweights</c> arrays, and <c>presets</c> is a
        /// top-level field of the generation API rather than a registered parameter.</remarks>
        private static readonly HashSet<string> NotRegisteredParameters = new(StringComparer.Ordinal) { "presets" };

        private static HashSet<string> LoadSnapshot()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Snapshots", "t2i-param-ids.txt");
            Assert.True(File.Exists(path), $"Parameter id snapshot missing at {path}");
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    ids.Add(trimmed);
                }
            }
            return ids;
        }

        private static IEnumerable<(string WireName, string PropertyName)> WireNames()
        {
            foreach (PropertyInfo property in typeof(GenerationRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                JsonPropertyAttribute? attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (attribute?.PropertyName is not null)
                {
                    yield return (attribute.PropertyName, property.Name);
                }
            }
        }

        [Fact]
        public void EveryWireName_IsRegisteredOnTheServer()
        {
            HashSet<string> registered = LoadSnapshot();
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
        public void Snapshot_ContainsExtensionRegisteredParameters()
        {
            HashSet<string> registered = LoadSnapshot();
            foreach (string id in new[] { "songlyrics", "grokaspectratio", "refinermodel", "vae" })
            {
                Assert.True(registered.Contains(id), $"Snapshot is missing '{id}', so it was captured without the extensions loaded and this suite would pass vacuously.");
            }
        }
    }
}
