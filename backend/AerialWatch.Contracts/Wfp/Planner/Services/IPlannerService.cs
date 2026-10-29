using AerialWatch.Contracts.Wfp.Planner.Models;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Planner.Services;

public interface IPlannerService
{
    /// <summary>Probability for each study window; camera/season given as strings from the query string.</summary>
    Task<PlannerResult> GetWindowsAsync(string? camera, string? season, string? date, CancellationToken ct = default);

    Task<ProbabilityResponse> GetProbabilityAsync(ProbabilityRequest request, CancellationToken ct = default);
}
