using Newtonsoft.Json;

namespace SwarmUI.ApiClient.Extensions.AudioLab.Contracts;

/// <summary>Response contract for saving a DAW project.</summary>
public class DawProjectSaveResponse : AudioLabResponse
{
    /// <summary>Name the project was saved under.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Serialized length of the stored project.</summary>
    [JsonProperty("size")]
    public int Size { get; set; }
}

/// <summary>Response contract for loading a DAW project.</summary>
public class DawProjectResponse : AudioLabResponse
{
    /// <summary>Name of the loaded project.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Serialized project arrangement, including embedded base64 clip audio.</summary>
    [JsonProperty("project_json")]
    public string ProjectJson { get; set; } = string.Empty;
}

/// <summary>Response contract listing the caller's saved DAW projects.</summary>
public class DawProjectListResponse : AudioLabResponse
{
    /// <summary>Names of the saved projects.</summary>
    [JsonProperty("projects")]
    public string[] Projects { get; set; } = [];
}

/// <summary>Response contract for deleting a DAW project.</summary>
public class DawProjectDeleteResponse : AudioLabResponse
{
    /// <summary>Name of the deleted project.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}
