using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>One box, in original-image pixels (top-left origin).</summary>
public sealed record DetectionDto(
    Guid Id,
    Guid ImageId,
    double X,
    double Y,
    double Width,
    double Height,
    string Label,
    double? Confidence,
    DetectionSource Source,
    DetectionStatus Status,
    string? ModelVersion,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt);
