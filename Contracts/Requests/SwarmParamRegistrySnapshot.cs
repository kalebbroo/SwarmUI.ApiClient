using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SwarmUI.ApiClient.Contracts.Requests;

/// <summary>The registered parameter ids this package was built against, shipped so a consumer can check its own
/// wire names against the same fixture this library checks its own against.</summary>
/// <remarks>
/// <para>SwarmUI drops a parameter name it does not recognise without erroring, so a typo costs a parameter that
/// never applies and never complains. The only way to catch that is a diff against the server's registered list,
/// and the only way for two codebases to agree is to diff against the same one — two snapshots of one registry
/// drift apart and then disagree about which is right.</para>
/// <para>Captured from a live <c>ListT2IParams</c> with the API-Backends, AudioLab and HartsyInference extensions
/// loaded, so extension-registered parameters are present. A server running a different extension set, or a newer
/// SwarmUI, registers a different list: treat an id missing from here as "not in this snapshot" rather than "not
/// real", and refresh the snapshot rather than working around it.</para>
/// </remarks>
public static class SwarmParamRegistrySnapshot
{
    private static readonly Lazy<IReadOnlySet<string>> LazyIds = new(Load);

    /// <summary>Every parameter id in the snapshot.</summary>
    public static IReadOnlySet<string> ParameterIds => LazyIds.Value;

    /// <summary>Whether the snapshot contains this parameter id.</summary>
    /// <param name="parameterId">Registered parameter id, compared exactly.</param>
    public static bool Contains(string parameterId) => LazyIds.Value.Contains(parameterId);

    private static IReadOnlySet<string> Load()
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        using Stream? stream = typeof(SwarmParamRegistrySnapshot).GetTypeInfo().Assembly.GetManifestResourceStream("SwarmUI.ApiClient.Snapshots.t2i-param-ids.txt");
        if (stream is null)
        {
            return ids;
        }
        using StreamReader reader = new(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                ids.Add(trimmed);
            }
        }
        return ids;
    }
}
