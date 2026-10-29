namespace AerialWatch.Domain.Wfp.ResearchModel;

public sealed record PlannerWindow(
    TimeWindow Window,
    string Label,
    string StudyHours,
    double Probability,
    bool IsBest,
    bool IsWorst,
    string? Note);
