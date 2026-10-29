using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>
/// Computes the probability that a feral pig present in the surveyed area appears (is detectable) in drone imagery,
/// using the published GLMs of Hvala et al. (2023).
/// </summary>
public static class DetectionProbabilityModel
{
    public static double Logistic(double logit) => 1.0 / (1.0 + Math.Exp(-logit));

    /// <summary>Main model (D-005): time of day + season, per camera type.</summary>
    public static DetectionProbabilityResult TimeSeason(TimeWindow window, Season season, CameraType camera,
        DetectionModelParameters? parameters = null)
    {
        var set = parameters ?? Hvala2023Coefficients.Published;
        var c = set.TimeSeasonFor(camera);
        var logit = c.Intercept
                    + (season == Season.Dry ? c.SeasonDry : 0.0)
                    + c.WindowEffect(window);
        var p = Logistic(logit);

        var assumptions = new List<Assumption>
        {
            Assumption.Info($"Time/season model, {CameraLabel(camera)} camera, {SurveyConditions.Label(window)}, {season.ToString().ToLowerInvariant()} season."),
        };

        if (camera == CameraType.Rgb && season == Season.Dry && window == TimeWindow.EarlyMorning)
        {
            assumptions.Add(Assumption.Warning(
                "In the study, the colour (RGB) camera detected no pigs in dry-season early mornings because light was too low. Use a thermal camera or fly later."));
        }

        return new DetectionProbabilityResult(p, DetectionModelKind.TimeSeason, set.Version, assumptions);
    }

    /// <summary>
    /// Optional model: ambient temperature (thermal only) + proportion of time animals spend under canopy (0–1).
    /// In the study canopy time came from GPS collars, so in normal use it is an approximation (e.g. from ground-cover type).
    /// </summary>
    public static DetectionProbabilityResult Environment(CameraType camera, double canopyProportion, double? ambientTemperatureC,
        DetectionModelParameters? parameters = null)
    {
        var set = parameters ?? Hvala2023Coefficients.Published;
        if (canopyProportion is < 0 or > 1 || double.IsNaN(canopyProportion))
            throw new ArgumentOutOfRangeException(nameof(canopyProportion), "Canopy proportion must be between 0 and 1.");

        var c = set.EnvironmentFor(camera);
        var logit = c.Intercept + c.TimeUnderCanopy * canopyProportion;

        var assumptions = new List<Assumption>
        {
            Assumption.Warning("Approximate: the environment model needs 'time under canopy', which the study measured with GPS collars. Here it is estimated from the ground cover you entered."),
        };

        if (c.AmbientTemperatureC is { } tempCoef)
        {
            if (ambientTemperatureC is null)
                throw new ArgumentException("Ambient temperature is required for the thermal environment model.", nameof(ambientTemperatureC));
            if (ambientTemperatureC is < 0 or > 50)
                throw new ArgumentOutOfRangeException(nameof(ambientTemperatureC), "Ambient temperature must be between 0 and 50 °C.");

            logit += tempCoef * ambientTemperatureC.Value;

            if (ambientTemperatureC is < 15 or > 36)
                assumptions.Add(Assumption.Warning($"{ambientTemperatureC:0.#} °C is outside the temperatures typical of the study; the estimate is less reliable."));
        }

        return new DetectionProbabilityResult(Logistic(logit), DetectionModelKind.Environment, set.Version, assumptions);
    }

    public static string CameraLabel(CameraType camera) => camera == CameraType.Thermal ? "thermal" : "colour (RGB)";
}
