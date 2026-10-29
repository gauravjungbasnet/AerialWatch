using AerialWatch.Core.Detection;

namespace AerialWatch.Core.Entities;

/// <summary>
/// One saved version of the detection-model coefficients (table ModelCoefficientSets). Every save or reset adds a
/// row, so the table is an audit history; the newest row is the set in use.
/// </summary>
public class ModelCoefficientSet : EntityBase
{
    public const int VersionMaxLength = 40;
    public const int CreatedByMaxLength = 80;

    public string Version { get; set; } = string.Empty;

    /// <summary>True when every value equals the published Hvala et al. (2023) coefficients.</summary>
    public bool IsPublished { get; set; }

    public string? CreatedBy { get; set; }

    // Time + season model, thermal camera
    public double ThermalIntercept { get; set; }
    public double ThermalSeasonDry { get; set; }
    public double ThermalMidAfternoon { get; set; }
    public double ThermalMidMorning { get; set; }
    public double ThermalEarlyMorning { get; set; }

    // Time + season model, RGB camera
    public double RgbIntercept { get; set; }
    public double RgbSeasonDry { get; set; }
    public double RgbMidAfternoon { get; set; }
    public double RgbMidMorning { get; set; }
    public double RgbEarlyMorning { get; set; }

    // Temperature + ground-cover model, thermal camera
    public double ThermalEnvIntercept { get; set; }
    public double? ThermalEnvTemperatureC { get; set; }
    public double ThermalEnvCanopy { get; set; }

    // Temperature + ground-cover model, RGB camera (no temperature term in the published model)
    public double RgbEnvIntercept { get; set; }
    public double? RgbEnvTemperatureC { get; set; }
    public double RgbEnvCanopy { get; set; }

    public DetectionModelParameters ToParameters() => new(
        Version,
        new(ThermalIntercept, ThermalSeasonDry, ThermalMidAfternoon, ThermalMidMorning, ThermalEarlyMorning),
        new(RgbIntercept, RgbSeasonDry, RgbMidAfternoon, RgbMidMorning, RgbEarlyMorning),
        new(ThermalEnvIntercept, ThermalEnvTemperatureC, ThermalEnvCanopy),
        new(RgbEnvIntercept, RgbEnvTemperatureC, RgbEnvCanopy));

    public static ModelCoefficientSet From(DetectionModelParameters p, DateTimeOffset now, string? createdBy) => new()
    {
        Version = p.Version,
        IsPublished = p.MatchesPublished(),
        CreatedBy = createdBy,
        CreatedAt = now,
        UpdatedAt = now,
        ThermalIntercept = p.ThermalTimeSeason.Intercept,
        ThermalSeasonDry = p.ThermalTimeSeason.SeasonDry,
        ThermalMidAfternoon = p.ThermalTimeSeason.MidAfternoon,
        ThermalMidMorning = p.ThermalTimeSeason.MidMorning,
        ThermalEarlyMorning = p.ThermalTimeSeason.EarlyMorning,
        RgbIntercept = p.RgbTimeSeason.Intercept,
        RgbSeasonDry = p.RgbTimeSeason.SeasonDry,
        RgbMidAfternoon = p.RgbTimeSeason.MidAfternoon,
        RgbMidMorning = p.RgbTimeSeason.MidMorning,
        RgbEarlyMorning = p.RgbTimeSeason.EarlyMorning,
        ThermalEnvIntercept = p.ThermalEnvironment.Intercept,
        ThermalEnvTemperatureC = p.ThermalEnvironment.AmbientTemperatureC,
        ThermalEnvCanopy = p.ThermalEnvironment.TimeUnderCanopy,
        RgbEnvIntercept = p.RgbEnvironment.Intercept,
        RgbEnvTemperatureC = p.RgbEnvironment.AmbientTemperatureC,
        RgbEnvCanopy = p.RgbEnvironment.TimeUnderCanopy,
    };
}
