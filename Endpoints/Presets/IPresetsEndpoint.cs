using System;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Requests;

namespace SwarmUI.ApiClient.Endpoints.Presets;

/// <summary>Provides access to SwarmUI preset management endpoints. Presets are saved parameter configurations for image generation.</summary>
/// <remarks>
/// This endpoint exposes the core preset management methods:
/// - AddNewPreset - Create a new preset (or edit existing if IsEdit=true)
/// - DeletePreset - Remove a preset
/// - DuplicatePreset - Clone a preset
/// - Listing presets is typically handled via the user-data endpoints (for example GetMyUserData).
///
/// Refer to your existing SwarmAPI.cs lines 247-365 for the original behaviour this interface models.
/// </remarks>
public interface IPresetsEndpoint
{
    /// <summary>Adds a new parameter preset or edits an existing one using SwarmUI's preset system.</summary>
    /// <param name="request">
    /// High-level preset definition including title, description, parameter map, and edit flags.
    /// The underlying HTTP call shapes this into the <c>AddNewPreset</c> API format.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// This method maps to SwarmUI's <c>/API/AddNewPreset</c> endpoint.
    ///
    /// Use cases:
    /// - <b>Create</b> a new preset by setting <see cref="PresetRequest.IsEdit"/> to <c>false</c>.
    /// - <b>Edit</b> an existing preset by setting <see cref="PresetRequest.IsEdit"/> to <c>true</c>
    ///   and <see cref="PresetRequest.EditingName"/> to the existing preset name.
    ///
    /// SwarmUI stores presets per-user; listing presets is typically done via the user-data
    /// endpoints (for example, <c>GetMyUserData</c>) exposed on the user endpoint group.
    /// </remarks>
    Task AddNewPresetAsync(PresetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an existing preset by name.</summary>
    /// <param name="presetName">The exact name of the preset to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// This method maps to SwarmUI's <c>/API/DeletePreset</c> endpoint.
    ///
    /// Use this when you want to permanently remove a preset from the user's account.
    /// The operation is irreversible from SwarmUI's perspective; callers should
    /// implement any higher-level confirmation UI if needed.
    /// </remarks>
    Task DeletePresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>Duplicates an existing preset, creating a copy with a new automatically generated name.</summary>
    /// <param name="presetName">The name of the preset to duplicate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// This method maps to SwarmUI's <c>/API/DuplicatePreset</c> endpoint.
    /// The server returns a new preset whose name is derived from the original,
    /// typically by appending or incrementing a suffix.
    ///
    /// Callers can then fetch the updated preset list via the user-data endpoints
    /// and allow users to rename or modify the duplicated preset as desired.
    /// </remarks>
    Task DuplicatePresetAsync(string presetName, CancellationToken cancellationToken = default);

    // Additional preset-related endpoints can be added here if SwarmUI introduces them in the future.
}
