namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>
/// Published coefficients from:
/// Hvala A., Rogers R.M., Alazab M., Campbell H.A. (2023). Supplementing aerial drone surveys with biotelemetry
/// data validates wildlife detection probabilities. Frontiers in Conservation Science 4:1203736.
/// https://doi.org/10.3389/fcosc.2023.1203736
///
/// Binomial GLMs (logit link). Reference level for time of day = early evening.
///
/// ASSUMPTIONS (documented in docs/02 and docs/00 D-005; to confirm with the authors):
///  - Season is coded 0 = wet, 1 = dry (the negative season coefficient matches the reported ~22% higher wet-season detection).
///  - "Time under canopy" is a proportion from 0 to 1.
///  - Only point estimates are used: the published CI column in Table 1 appears misaligned.
/// </summary>
public static class Hvala2023Coefficients
{
    public const string ModelVersion = "hvala2023-v1";

    public const string Citation =
        "Hvala A., Rogers R.M., Alazab M., Campbell H.A. (2023). Supplementing aerial drone surveys with biotelemetry " +
        "data validates wildlife detection probabilities. Frontiers in Conservation Science, 4, 1203736.";

    public const string Doi = "https://doi.org/10.3389/fcosc.2023.1203736";

    /// <summary>Time/season model coefficients for one camera type.</summary>
    public sealed record TimeSeasonCoefficients(
        double Intercept,
        double SeasonDry,
        double MidAfternoon,
        double MidMorning,
        double EarlyMorning)
    {
        public double WindowEffect(TimeWindow window) => window switch
        {
            TimeWindow.EarlyEvening => 0.0, // reference level
            TimeWindow.EarlyMorning => EarlyMorning,
            TimeWindow.MidMorning => MidMorning,
            TimeWindow.MidAfternoon => MidAfternoon,
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, null)
        };
    }

    /// <summary>Environment model coefficients for one camera type. Temperature coefficient is null when not in the model.</summary>
    public sealed record EnvironmentCoefficients(
        double Intercept,
        double? AmbientTemperatureC,
        double TimeUnderCanopy);

    public static readonly TimeSeasonCoefficients ThermalTimeSeason = new(
        Intercept: 0.951,
        SeasonDry: -1.258,
        MidAfternoon: -2.728,
        MidMorning: -1.043,
        EarlyMorning: -0.427);

    public static readonly TimeSeasonCoefficients RgbTimeSeason = new(
        Intercept: 2.335,
        SeasonDry: -1.8016,
        MidAfternoon: -3.729,
        MidMorning: -2.3308,
        EarlyMorning: -2.811);

    public static readonly EnvironmentCoefficients ThermalEnvironment = new(
        Intercept: 5.869,
        AmbientTemperatureC: -0.175,
        TimeUnderCanopy: -3.442);

    public static readonly EnvironmentCoefficients RgbEnvironment = new(
        Intercept: 1.2789,
        AmbientTemperatureC: null,
        TimeUnderCanopy: -3.9433);

    /// <summary>The published coefficient set: the default for every calculation.</summary>
    public static readonly DetectionModelParameters Published = new(
        ModelVersion, ThermalTimeSeason, RgbTimeSeason, ThermalEnvironment, RgbEnvironment);

    public static TimeSeasonCoefficients TimeSeasonFor(CameraType camera) =>
        camera == CameraType.Thermal ? ThermalTimeSeason : RgbTimeSeason;

    public static EnvironmentCoefficients EnvironmentFor(CameraType camera) =>
        camera == CameraType.Thermal ? ThermalEnvironment : RgbEnvironment;

    /// <summary>Conditions under which the model was fitted; shown to users wherever an estimate is shown.</summary>
    public static readonly IReadOnlyList<string> StudyConditions = new[]
    {
        "Fitted on feral pigs in a 20-acre enclosure at CDU Katherine Rural Campus, NT.",
        "7 GPS-collared pigs per season; wet season Feb 2021, dry season Jun 2022.",
        "Fixed-wing drone (SenseFly eBee X) at about 52 m above ground; thermal 640×512 and RGB 5472×3648 cameras.",
        "Detections in the study were made by trained analysts reviewing each image twice."
    };
}
