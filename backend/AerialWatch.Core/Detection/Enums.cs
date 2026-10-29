namespace AerialWatch.Core.Detection;

/// <summary>NT seasons as used in Hvala et al. (2023): wet (survey Feb 2021) and dry (survey Jun 2022).</summary>
public enum Season
{
    Wet,
    Dry
}

/// <summary>Camera used on the drone. The paper fitted separate models for thermal infrared and visual (RGB).</summary>
public enum CameraType
{
    Thermal,
    Rgb
}

/// <summary>
/// The four survey windows flown in the study:
/// early morning 05:00–06:00, mid-morning 09:00–10:00, mid-afternoon 14:00–15:00, early evening 18:00–19:00.
/// Early evening is the model's reference level.
/// </summary>
public enum TimeWindow
{
    EarlyMorning,
    MidMorning,
    MidAfternoon,
    EarlyEvening
}

/// <summary>Which published model was used to compute detection probability.</summary>
public enum DetectionModelKind
{
    /// <summary>Time of day + season (main model; inputs always known for a survey). See decision D-005.</summary>
    TimeSeason,

    /// <summary>Ambient temperature + time under canopy (optional; canopy was measured with GPS collars in the study, so it is approximate in normal use).</summary>
    Environment
}

public enum AssumptionLevel
{
    Info,
    Warning
}

public sealed record Assumption(AssumptionLevel Level, string Message)
{
    public static Assumption Info(string message) => new(AssumptionLevel.Info, message);
    public static Assumption Warning(string message) => new(AssumptionLevel.Warning, message);
}
