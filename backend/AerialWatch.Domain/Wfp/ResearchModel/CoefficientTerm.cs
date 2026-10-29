namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>Which term of a detection model a coefficient belongs to (Hvala et al. 2023, Tables 1 and 2).</summary>
public enum CoefficientTerm
{
    /// <summary>Baseline: early evening, wet season (time + season model) or open ground (environment model).</summary>
    Intercept,
    SeasonDry,
    MidAfternoon,
    MidMorning,
    EarlyMorning,
    AmbientTemperatureC,
    TimeUnderCanopy
}
