using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.ResearchModel;

public sealed record DetectionProbabilityResult(
    double Probability,
    DetectionModelKind Model,
    string ModelVersion,
    IReadOnlyList<Assumption> Assumptions);
