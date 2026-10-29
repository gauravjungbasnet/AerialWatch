namespace AerialWatch.Core.Detection;

public sealed record PlannerWindow(
    TimeWindow Window,
    string Label,
    string StudyHours,
    double Probability,
    bool IsBest,
    bool IsWorst,
    string? Note);

public sealed record PlannerResult(
    Season Season,
    CameraType Camera,
    IReadOnlyList<PlannerWindow> Windows,
    string Recommendation,
    string ModelVersion,
    IReadOnlyList<Assumption> Assumptions);

/// <summary>Recommends the survey window with the highest chance of detecting feral pigs.</summary>
public static class SurveyPlanner
{
    private static readonly TimeWindow[] Order =
    {
        TimeWindow.EarlyMorning, TimeWindow.MidMorning, TimeWindow.MidAfternoon, TimeWindow.EarlyEvening
    };

    public static PlannerResult Plan(Season season, CameraType camera, DetectionModelParameters? parameters = null)
    {
        var set = parameters ?? Hvala2023Coefficients.Published;
        var results = Order
            .Select(w => (Window: w, Result: DetectionProbabilityModel.TimeSeason(w, season, camera, set)))
            .ToList();

        var best = results.MaxBy(r => r.Result.Probability).Window;
        var worst = results.MinBy(r => r.Result.Probability).Window;

        var windows = results.Select(r => new PlannerWindow(
            r.Window,
            SurveyConditions.Label(r.Window),
            SurveyConditions.StudyHours(r.Window),
            Math.Round(r.Result.Probability, 4),
            r.Window == best,
            r.Window == worst,
            r.Result.Assumptions.FirstOrDefault(a => a.Level == AssumptionLevel.Warning)?.Message)).ToList();

        var bestP = results.First(r => r.Window == best).Result.Probability;
        var worstP = results.First(r => r.Window == worst).Result.Probability;

        var recommendation =
            $"Fly in the {SurveyConditions.Label(best).ToLowerInvariant()} (about {SurveyConditions.StudyHours(best)}): " +
            $"roughly {bestP * 100:0}% of pigs present are expected to be visible, versus {worstP * 100:0}% in the {SurveyConditions.Label(worst).ToLowerInvariant()}.";

        var assumptions = new List<Assumption>();
        assumptions.AddRange(Hvala2023Coefficients.StudyConditions.Select(Assumption.Info));
        assumptions.Add(Assumption.Info("Season coded wet = 0, dry = 1 (our reading of the paper; awaiting author confirmation)."));

        return new PlannerResult(season, camera, windows, recommendation, set.Version, assumptions);
    }
}
