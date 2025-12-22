using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Models.Common;

/// <summary>Represents SwarmUI's standardized metadata format for generated images.</summary>
/// <remarks>Exposes generation parameters, extra data, and models used for reproducibility. See the SwarmUI metadata documentation for the full JSON structure.</remarks>
public class SwarmUIMetadata
{
    /// <summary>Generation parameters keyed by name (for example, "prompt", "model", "steps").</summary>
    /// <remarks>Use <see cref="GetImageParam{T}"/> for type-safe retrieval with automatic type conversion.</remarks>
    [JsonProperty("sui_image_params")]
    public Dictionary<string, object> ImageParams { get; set; } = new Dictionary<string, object>();

    /// <summary>Optional additional data generated during image processing. May contain workflow information, intermediate results, or other implementation-specific data. Structure varies based on generation backend.</summary>
    [JsonProperty("sui_extra_data")]
    public Dictionary<string, object>? ExtraData { get; set; }

    /// <summary>List of models used in generation with their identifiers and hashes. Each model entry includes the model name, which parameter it was used for (e.g., "model", "lora"), and optionally a hash for verification. Useful for tracking exact model versions for reproducibility.</summary>
    [JsonProperty("sui_models")]
    public List<SwarmUIModelInfo>? Models { get; set; }

    /// <summary>Parses a JSON metadata string into a SwarmUIMetadata object.</summary>
    /// <param name="jsonMetadata">JSON string containing metadata to parse.</param>
    /// <returns>Parsed SwarmUIMetadata object, or null if the string is empty or invalid JSON.</returns>
    /// <remarks>Attempts to deserialize using the standard SwarmUIMetadata structure and, if that fails, as a flat dictionary wrapped into <see cref="ImageParams"/>. Returns null if both attempts fail.</remarks>
    public static SwarmUIMetadata? FromJson(string jsonMetadata)
    {
        if (string.IsNullOrEmpty(jsonMetadata))
        {
            return null;
        }
        try
        {
            SwarmUIMetadata? result = JsonConvert.DeserializeObject<SwarmUIMetadata>(jsonMetadata);
            return result;
        }
        catch (JsonException)
        {
            try
            {
                Dictionary<string, object>? dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMetadata);
                if (dict != null)
                {
                    return new SwarmUIMetadata
                    {
                        ImageParams = dict
                    };
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>Retrieves a typed parameter value from <see cref="ImageParams"/> with automatic type conversion.</summary>
    /// <typeparam name="T">Expected type of the parameter value.</typeparam>
    /// <param name="key">Parameter key to retrieve (e.g., "prompt", "steps", "cfgscale").</param>
    /// <param name="defaultValue">Value to return if key is missing or conversion fails.</param>
    /// <returns>Value converted to type T, or <paramref name="defaultValue"/> if not found or conversion fails.</returns>
    /// <remarks>Provides safe, typed access to metadata parameters and handles common conversions between strings, numbers, and booleans.</remarks>
    public T GetImageParam<T>(string key, T defaultValue)
    {
        if (ImageParams == null || !ImageParams.TryGetValue(key, out object? value))
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

    /// <summary>Generates a short, human-readable description of key generation parameters.</summary>
    /// <returns>A multi-line string including prompt, model, size, steps, CFG scale, seed, and sampler.</returns>
    public string GetDescription()
    {
        List<string> desc = [];
        string prompt = GetImageParam<string>("prompt", "");
        if (!string.IsNullOrEmpty(prompt))
        {
            if (prompt.Length > 100)
            {
                prompt = prompt.Substring(0, 97) + "...";
            }
            desc.Add($"Prompt: {prompt}");
        }
        string model = GetImageParam<string>("model", "");
        if (!string.IsNullOrEmpty(model))
        {
            string modelName = model.Contains('/') ? model.Split('/').Last() : model;
            desc.Add($"Model: {modelName}");
        }
        desc.Add($"Size: {GetImageParam("width", 0)}×{GetImageParam("height", 0)}");
        desc.Add($"Steps: {GetImageParam("steps", 0)}");
        desc.Add($"CFG: {GetImageParam("cfgscale", 7.0f)}");
        desc.Add($"Seed: {GetImageParam("seed", "-1")}");
        desc.Add($"Sampler: {GetImageParam("sampler", "euler")}");
        return string.Join("\n", desc);
    }
}

/// <summary>Represents a model that was used during image generation, including name, role, and optional hash.</summary>
/// <remarks>Tracks the main model and any auxiliary models (such as LoRAs or VAEs) with enough information to reproduce a generation or verify model versions.</remarks>
public class SwarmUIModelInfo
{
    /// <summary>Filename of the model that was used. This is just the file name, not the full path.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The parameter this model was attached to during generation.
    /// Common values: "model" (main model), "lora" (LoRA), "vae" (VAE model).
    /// Identifies the role this model played in the generation pipeline.</summary>
    [JsonProperty("param")]
    public string Param { get; set; } = string.Empty;

    /// <summary>SHA256 hash of the model's tensor data, used for exact version identification. May be null if the hash wasn't computed.</summary>
    [JsonProperty("hash")]
    public string? Hash { get; set; }
}
