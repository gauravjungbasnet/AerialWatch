using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>
/// A survey photo's metadata. The file and thumbnail are fetched with surveyImages/original/{id} and surveyImages/thumbnail/{id}.
/// Camera / IsReference / TwinId describe thermal + colour pairs (D-039): reference photos are shown, never counted.
/// </summary>
public sealed record SurveyImageDto(
    Guid Id,
    Guid SurveyId,
    string FileName,
    string ContentType,
    long SizeBytes,
    int? WidthPx,
    int? HeightPx,
    DateTimeOffset? TakenAt,
    double? Latitude,
    double? Longitude,
    bool HasThumbnail,
    bool Reviewed,
    DateTimeOffset UploadedAt,
    CameraType Camera,
    bool IsReference,
    Guid? TwinId,
    bool LocationFromTwin);
