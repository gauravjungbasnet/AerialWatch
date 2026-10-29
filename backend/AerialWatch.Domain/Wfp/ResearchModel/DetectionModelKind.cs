namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>Which published model was used to compute detection probability.</summary>
public enum DetectionModelKind
{
    /// <summary>Time of day + season (main model; inputs always known for a survey). See decision D-005.</summary>
    TimeSeason,

    /// <summary>Ambient temperature + time under canopy (optional; canopy was measured with GPS collars in the study, so it is approximate in normal use).</summary>
    Environment
}
