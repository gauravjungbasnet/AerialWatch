using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.ResearchModel;

public sealed record PlannerResult(
    Season Season,
    CameraType Camera,
    IReadOnlyList<PlannerWindow> Windows,
    string Recommendation,
    string ModelVersion,
    IReadOnlyList<Assumption> Assumptions);
