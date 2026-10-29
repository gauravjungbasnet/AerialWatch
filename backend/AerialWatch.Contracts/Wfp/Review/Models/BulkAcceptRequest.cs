namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>Accept every pending AI suggestion in the survey with confidence at or above MinConfidence (0–1).</summary>
public sealed record BulkAcceptRequest(double MinConfidence);
