namespace AerialWatch.Core.Detection;

public sealed record PopulationEstimate(
    int ConfirmedCount,
    double DetectionProbability,
    double Estimate,
    int EstimateRounded,
    int Low,
    int High,
    double? DensityPerKm2,
    string Method);

/// <summary>
/// Corrects a confirmed count for animals that were present but not visible.
///
/// N̂ = C / p  (Horvitz–Thompson style correction; the paper describes a correction factor but does not print a formula,
/// so this standard estimator is our documented assumption).
///
/// 95% range: treats detection of each animal as an independent Bernoulli(p) event, giving Var(N̂) ≈ C(1 − p) / p².
/// Range = N̂ ± 1.96·SD, never below the confirmed count. It does NOT include uncertainty in the published coefficients,
/// so the true range is wider.
///
/// When C = 0: the estimate is 0, and the upper bound is the largest N for which seeing zero animals still has ≥ 5% chance:
/// (1 − p)^N ≥ 0.05  →  N ≤ ln(0.05) / ln(1 − p).
/// </summary>
public static class PopulationEstimator
{
    private const double Z95 = 1.959964;

    public static PopulationEstimate Estimate(int confirmedCount, double detectionProbability, double? areaKm2 = null)
    {
        if (confirmedCount < 0)
            throw new ArgumentOutOfRangeException(nameof(confirmedCount), "Count cannot be negative.");
        if (detectionProbability is <= 0 or > 1 || double.IsNaN(detectionProbability))
            throw new ArgumentOutOfRangeException(nameof(detectionProbability), "Detection probability must be in (0, 1].");
        if (areaKm2 is <= 0)
            throw new ArgumentOutOfRangeException(nameof(areaKm2), "Area must be greater than 0.");

        const string method =
            "Estimate = confirmed count ÷ chance a pig is visible. Range = 95% interval from detection chance alone " +
            "(excludes uncertainty in the published model, so the true range is wider).";

        if (confirmedCount == 0)
        {
            var high = detectionProbability >= 1
                ? 0
                : (int)Math.Floor(Math.Log(0.05) / Math.Log(1 - detectionProbability));
            return new PopulationEstimate(0, detectionProbability, 0, 0, 0, high,
                areaKm2 is null ? null : 0, method + " No pigs were confirmed; this does not prove none are present.");
        }

        var estimate = confirmedCount / detectionProbability;
        var sd = Math.Sqrt(confirmedCount * (1 - detectionProbability)) / detectionProbability;
        var low = Math.Max(confirmedCount, (int)Math.Floor(estimate - Z95 * sd));
        var highBound = (int)Math.Ceiling(estimate + Z95 * sd);
        var rounded = (int)Math.Round(estimate, MidpointRounding.AwayFromZero);

        return new PopulationEstimate(
            confirmedCount,
            detectionProbability,
            estimate,
            rounded,
            Math.Min(low, rounded),
            Math.Max(highBound, rounded),
            areaKm2 is null ? null : Math.Round(estimate / areaKm2.Value, 2),
            method);
    }
}
