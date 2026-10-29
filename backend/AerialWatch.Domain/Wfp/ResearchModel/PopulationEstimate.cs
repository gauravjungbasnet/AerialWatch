namespace AerialWatch.Domain.Wfp.ResearchModel;

public sealed record PopulationEstimate(
    int ConfirmedCount,
    double DetectionProbability,
    double Estimate,
    int EstimateRounded,
    int Low,
    int High,
    double? DensityPerKm2,
    string Method);
