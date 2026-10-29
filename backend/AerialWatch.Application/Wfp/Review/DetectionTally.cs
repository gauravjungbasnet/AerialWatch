using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Review;

/// <summary>Number of boxes on one photo with a given source and status (one row of a GROUP BY).</summary>
public sealed record DetectionTally(Guid ImageId, DetectionSource Source, DetectionStatus Status, int Count);
