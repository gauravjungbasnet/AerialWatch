using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>Move/resize a box and/or accept, reject or reset it. Omitted fields stay as they are; geometry needs all four values.</summary>
public sealed record UpdateDetectionRequest(
    double? X = null,
    double? Y = null,
    double? Width = null,
    double? Height = null,
    DetectionStatus? Status = null);
