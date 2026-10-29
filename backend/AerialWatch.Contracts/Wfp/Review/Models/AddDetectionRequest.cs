namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>A box drawn by a ranger (a pig the AI missed), in original-image pixels.</summary>
public sealed record AddDetectionRequest(double X, double Y, double Width, double Height);
