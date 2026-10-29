namespace AerialWatch.Domain.Wfp.ResearchModel;

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
