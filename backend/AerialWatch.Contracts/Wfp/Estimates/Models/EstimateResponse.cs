using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Estimates.Models;

public sealed record EstimateResponse(
    int ConfirmedCount,
    double DetectionProbability,
    double Estimate,
    int EstimateRounded,
    int Low,
    int High,
    double? DensityPerKm2,
    string Method,
    DetectionModelKind Model,
    string ModelVersion,
    TimeWindow? Window,
    Season Season,
    bool Provisional,
    IReadOnlyList<Assumption> Assumptions);
