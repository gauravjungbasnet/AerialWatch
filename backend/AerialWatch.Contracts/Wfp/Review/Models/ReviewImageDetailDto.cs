namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>A photo with all its boxes (for the viewer), plus its thermal/colour twin when one was uploaded (D-039).</summary>
public sealed record ReviewImageDetailDto(ReviewImageDto Image, IReadOnlyList<DetectionDto> Detections, TwinImageDto? Twin = null);
