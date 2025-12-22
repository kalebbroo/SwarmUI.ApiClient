using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Responses;

/// <summary>Response from SwarmUI's GetUserSettings endpoint containing persistent user preferences.</summary>
/// <remarks>Includes theme, UI preferences, default parameters, and behavior options. The structure can vary by SwarmUI version, so settings are exposed as a flexible dictionary.</remarks>
public class UserSettingsResponse
{
    /// <summary>Dictionary of user settings keyed by name (for example, "theme" or "default_steps").</summary>
    /// <remarks>Value types vary by key and may be strings, numbers, booleans, or complex objects. Prefer using <see cref="GetSetting{T}"/> for typed access.</remarks>
    [JsonProperty("settings")]
    public Dictionary<string, object>? Settings { get; set; }

    /// <summary>Additional raw data from the response not mapped to specific properties. SwarmUI may include metadata or other fields alongside the settings dictionary. This ensures forward compatibility and prevents data loss when the API evolves.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? RawData { get; set; }

    /// <summary>Gets a strongly-typed setting value from <see cref="Settings"/> with a fallback default.</summary>
    /// <typeparam name="T">Expected type of the setting value.</typeparam>
    /// <param name="key">Setting key to retrieve (e.g., "theme", "default_steps").</param>
    /// <param name="defaultValue">Value to return if key is missing or conversion fails.</param>
    /// <returns>Setting value converted to type T, or defaultValue if not found/convertible.</returns>
    /// <remarks>Provides safe, typed access to settings and handles common conversions between strings, numbers, and booleans. Returns <paramref name="defaultValue"/> if the key is missing or cannot be converted.</remarks>
    public T GetSetting<T>(string key, T defaultValue)
    {
        if (Settings is null || !Settings.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }
        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)(value?.ToString() ?? string.Empty);
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
                if (value is double doubleValue)
                {
                    return (T)(object)(int)doubleValue;
                }
            }
            if (typeof(T) == typeof(float) && value != null)
            {
                if (float.TryParse(value.ToString(), out float floatValue))
                {
                    return (T)(object)floatValue;
                }
                if (value is double doubleValue)
                {
                    return (T)(object)(float)doubleValue;
                }
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
