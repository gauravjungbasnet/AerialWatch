namespace AerialWatch.Domain.Wfp.ResearchModel;

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

    /// <summary>One coefficient, or null when the model has no such term (e.g. temperature in the RGB environment model).</summary>
    public double? ValueOf(CameraType camera, DetectionModelKind model, CoefficientTerm term)
    {
        if (model == DetectionModelKind.TimeSeason)
        {
            var t = TimeSeasonFor(camera);
            return term switch
            {
                CoefficientTerm.Intercept => t.Intercept,
                CoefficientTerm.SeasonDry => t.SeasonDry,
                CoefficientTerm.MidAfternoon => t.MidAfternoon,
                CoefficientTerm.MidMorning => t.MidMorning,
                CoefficientTerm.EarlyMorning => t.EarlyMorning,
                _ => null,
            };
        }

        var e = EnvironmentFor(camera);
        return term switch
        {
            CoefficientTerm.Intercept => e.Intercept,
            CoefficientTerm.AmbientTemperatureC => e.AmbientTemperatureC,
            CoefficientTerm.TimeUnderCanopy => e.TimeUnderCanopy,
            _ => null,
        };
    }

    /// <summary>
    /// Builds parameters from individual values (as stored in wfp.ModelCoefficientValues). Every term is required except
    /// the environment-model temperature, which is null when absent.
    /// </summary>
    public static DetectionModelParameters FromValues(string version, IEnumerable<CoefficientValue> values)
    {
        var map = new Dictionary<(CameraType, DetectionModelKind, CoefficientTerm), double>();
        foreach (var v in values)
            if (!map.TryAdd((v.Camera, v.Model, v.Term), v.Value))
                throw new InvalidOperationException($"Coefficient set {version} has {CoefficientType.CodeFor(v.Camera, v.Model, v.Term)} twice.");

        double Get(CameraType c, DetectionModelKind m, CoefficientTerm t) =>
            map.TryGetValue((c, m, t), out var value)
                ? value
                : throw new InvalidOperationException($"Coefficient set {version} is missing {CoefficientType.CodeFor(c, m, t)}.");

        double? Optional(CameraType c, DetectionModelKind m, CoefficientTerm t) =>
            map.TryGetValue((c, m, t), out var value) ? value : null;

        Hvala2023Coefficients.TimeSeasonCoefficients TimeSeason(CameraType c) => new(
            Get(c, DetectionModelKind.TimeSeason, CoefficientTerm.Intercept),
            Get(c, DetectionModelKind.TimeSeason, CoefficientTerm.SeasonDry),
            Get(c, DetectionModelKind.TimeSeason, CoefficientTerm.MidAfternoon),
            Get(c, DetectionModelKind.TimeSeason, CoefficientTerm.MidMorning),
            Get(c, DetectionModelKind.TimeSeason, CoefficientTerm.EarlyMorning));

        Hvala2023Coefficients.EnvironmentCoefficients Environment(CameraType c) => new(
            Get(c, DetectionModelKind.Environment, CoefficientTerm.Intercept),
            Optional(c, DetectionModelKind.Environment, CoefficientTerm.AmbientTemperatureC),
            Get(c, DetectionModelKind.Environment, CoefficientTerm.TimeUnderCanopy));

        return new DetectionModelParameters(
            version,
            TimeSeason(CameraType.Thermal),
            TimeSeason(CameraType.Rgb),
            Environment(CameraType.Thermal),
            Environment(CameraType.Rgb));
    }

    /// <summary>True when every coefficient equals the published values (version is ignored).</summary>
    public bool MatchesPublished() =>
        ThermalTimeSeason == Hvala2023Coefficients.ThermalTimeSeason &&
        RgbTimeSeason == Hvala2023Coefficients.RgbTimeSeason &&
        ThermalEnvironment == Hvala2023Coefficients.ThermalEnvironment &&
        RgbEnvironment == Hvala2023Coefficients.RgbEnvironment;
}
