namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>A box in a photo's original pixels: a new sighting on the twin, or a sighting confirmed on the counted photo (D-041).</summary>
public sealed record TwinBoxRequest(double X, double Y, double Width, double Height);
