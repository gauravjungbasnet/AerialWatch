using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Planner.Models;

public sealed record ProbabilityResponse(
    double Probability,
    DetectionModelKind Model,
    string ModelVersion,
    TimeWindow? Window,
    Season? Season,
    IReadOnlyList<Assumption> Assumptions);
