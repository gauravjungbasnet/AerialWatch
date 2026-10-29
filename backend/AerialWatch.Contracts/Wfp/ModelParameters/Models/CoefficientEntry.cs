namespace AerialWatch.Contracts.Wfp.ModelParameters.Models;

/// <summary>A coefficient value by code, e.g. ("thermal.timeSeason.intercept", 0.951). Used to save and in the history.</summary>
public sealed record CoefficientEntry(string Code, double Value);
