namespace AerialWatch.Core.Detection;

/// <summary>
/// A complete set of detection-model coefficients. The published set (<see cref="Hvala2023Coefficients.Published"/>)
/// is the default; an admin can supply an edited set (e.g. after recalibration with new NT data).
/// </summary>
public sealed record DetectionModelParameters(
    string Version,
    Hvala2023Coefficients.TimeSeasonCoefficients ThermalTimeSeason,
    Hvala2023Coefficients.TimeSeasonCoefficients RgbTimeSeason,
    Hvala2023Coefficients.EnvironmentCoefficients ThermalEnvironment,
    Hvala2023Coefficients.EnvironmentCoefficients RgbEnvironment)
{
    public Hvala2023Coefficients.TimeSeasonCoefficients TimeSeasonFor(CameraType camera) =>
        camera == CameraType.Thermal ? ThermalTimeSeason : RgbTimeSeason;

    public Hvala2023Coefficients.EnvironmentCoefficients EnvironmentFor(CameraType camera) =>
        camera == CameraType.Thermal ? ThermalEnvironment : RgbEnvironment;

    /// <summary>True when every coefficient equals the published values (version is ignored).</summary>
    public bool MatchesPublished() =>
        ThermalTimeSeason == Hvala2023Coefficients.ThermalTimeSeason &&
        RgbTimeSeason == Hvala2023Coefficients.RgbTimeSeason &&
        ThermalEnvironment == Hvala2023Coefficients.ThermalEnvironment &&
        RgbEnvironment == Hvala2023Coefficients.RgbEnvironment;
}
