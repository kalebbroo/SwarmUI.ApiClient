using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's ListBackends endpoint containing configured GPU backends and their metadata.</summary>
/// <remarks>Backends are worker processes (typically ComfyUI instances) that handle generation workloads. See the SwarmUI Backends documentation for the full response schema.</remarks>
public class BackendsListResponse
{
    /// <summary>Collection of backend entries returned by the ListBackends endpoint.</summary>
    /// <remarks>Each dictionary contains backend fields such as identifier, type, address, status, enabled flags, and other metadata.</remarks>
    [JsonProperty("backends")]
    public List<Dictionary<string, object>>? Backends { get; set; } = new List<Dictionary<string, object>>();

    /// <summary>Additional raw data from the response not mapped to specific properties. SwarmUI may include metadata or statistics about the backend pool. This ensures forward compatibility when new fields are added to the API.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? RawData { get; set; }

    /// <summary>Gets a specific property value from a backend object with type-safe access.</summary>
    /// <typeparam name="T">Expected type of the property value.</typeparam>
    /// <param name="backend">Backend dictionary to extract from.</param>
    /// <param name="key">Property key to retrieve (e.g., "status", "enabled").</param>
    /// <param name="defaultValue">Value to return if key is missing or conversion fails.</param>
    /// <returns>Property value converted to type T, or <paramref name="defaultValue"/> if not found.</returns>
    /// <remarks>Helper for working with flexible backend dictionaries. Returns the provided default when a key is missing or cannot be converted.</remarks>
    public static T GetBackendProperty<T>(Dictionary<string, object> backend, string key, T defaultValue)
    {
        if (backend == null || !backend.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }
        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)(value?.ToString() ?? string.Empty);
            }
            if (typeof(T) == typeof(bool) && value != null)
            {
                if (bool.TryParse(value.ToString(), out bool boolValue))
                {
                    return (T)(object)boolValue;
                }
                if (int.TryParse(value.ToString(), out int intValue))
                {
                    return (T)(object)(intValue != 0);
                }
            }
            if (typeof(T) == typeof(int) && value != null)
            {
                if (int.TryParse(value.ToString(), out int intValue))
                {
                    return (T)(object)intValue;
                }
                if (value is long longValue)
                {
                    return (T)(object)(int)longValue;
                }
            }
            if (value is null)
            {
                return defaultValue;
            }
            object? converted = Convert.ChangeType(value, typeof(T));
            if (converted is T typed)
            {
                return typed;
            }
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
