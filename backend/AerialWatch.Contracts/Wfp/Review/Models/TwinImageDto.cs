using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>The other camera's photo of the same moment (D-039), shown beside the photo under review for reference only.</summary>
public sealed record TwinImageDto(
    Guid Id,
    string FileName,
    CameraType Camera,
    int? WidthPx,
    int? HeightPx,
    DateTimeOffset? TakenAt,
    bool HasThumbnail);
