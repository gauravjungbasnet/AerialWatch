namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>A photo in the review queue, with its box counts. Photo files come from surveyImages/original|thumbnail/{id}.</summary>
public sealed record ReviewImageDto(
    Guid Id,
    string FileName,
    int? WidthPx,
    int? HeightPx,
    DateTimeOffset? TakenAt,
    double? Latitude,
    double? Longitude,
    bool HasThumbnail,
    bool Reviewed,
    bool Flagged,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    // Confirmed pigs: accepted AI boxes plus boxes drawn by a ranger.
    int Confirmed,
    // AI suggestions still to check.
    int Pending,
    int Rejected);
