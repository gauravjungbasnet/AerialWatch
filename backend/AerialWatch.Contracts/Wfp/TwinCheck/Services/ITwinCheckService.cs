using AerialWatch.Contracts.Wfp.TwinCheck.Models;

namespace AerialWatch.Contracts.Wfp.TwinCheck.Services;

/// <summary>Checking pigs on the other camera's twin photo, both ways (D-040, D-041).</summary>
public interface ITwinCheckService
{
    Task<TwinCheckDto> GetAsync(Guid imageId, CancellationToken ct = default);

    // ---- counted pig → twin ----

    Task<TwinMarkDto> SetAsync(Guid detectionId, SetTwinMarkRequest request, string? user, CancellationToken ct = default);

    /// <summary>Back to the suggestion (removes the ranger's answer).</summary>
    Task ClearAsync(Guid detectionId, CancellationToken ct = default);

    // ---- twin → counted photo ----

    /// <summary>A pig drawn first on the twin of counted photo <paramref name="imageId"/>.</summary>
    Task<TwinSightingDto> AddSightingAsync(Guid imageId, TwinBoxRequest request, string? user, CancellationToken ct = default);

    /// <summary>The sighting is on the counted photo at this box: creates the counted pig (a ranger box) and links it.</summary>
    Task<TwinMarkDto> ConfirmSightingAsync(Guid markId, TwinBoxRequest request, string? user, CancellationToken ct = default);

    Task<TwinSightingDto> SetSightingVisibilityAsync(Guid markId, SetSightingVisibilityRequest request, string? user, CancellationToken ct = default);

    Task RemoveSightingAsync(Guid markId, CancellationToken ct = default);

    Task<TwinCheckSummaryDto> GetSummaryAsync(Guid surveyId, CancellationToken ct = default);
}
