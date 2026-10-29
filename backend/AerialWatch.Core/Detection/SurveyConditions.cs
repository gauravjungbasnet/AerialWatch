namespace AerialWatch.Core.Detection;

/// <summary>Maps real-world survey date/time to the study's seasons and time windows.</summary>
public static class SurveyConditions
{
    /// <summary>Exact hours flown in the study, for display.</summary>
    public static string StudyHours(TimeWindow window) => window switch
    {
        TimeWindow.EarlyMorning => "05:00–06:00",
        TimeWindow.MidMorning => "09:00–10:00",
        TimeWindow.MidAfternoon => "14:00–15:00",
        TimeWindow.EarlyEvening => "18:00–19:00",
        _ => throw new ArgumentOutOfRangeException(nameof(window), window, null)
    };

    public static string Label(TimeWindow window) => window switch
    {
        TimeWindow.EarlyMorning => "Early morning",
        TimeWindow.MidMorning => "Mid-morning",
        TimeWindow.MidAfternoon => "Mid-afternoon",
        TimeWindow.EarlyEvening => "Early evening",
        _ => throw new ArgumentOutOfRangeException(nameof(window), window, null)
    };

    /// <summary>
    /// NT convention: wet season November–April, dry season May–October.
    /// (Assumption: the build-up months are ambiguous; users can override the season.)
    /// </summary>
    public static Season SeasonFromDate(DateOnly date) =>
        date.Month is >= 5 and <= 10 ? Season.Dry : Season.Wet;

    /// <summary>
    /// Maps a clock time to the nearest study window.
    /// Bands (our assumption): 04:00–07:29 early morning, 07:30–11:59 mid-morning,
    /// 12:00–16:29 mid-afternoon, 16:30–19:59 early evening. Night (20:00–03:59) returns null: not covered by the study.
    /// </summary>
    public static TimeWindowMatch MatchTime(TimeOnly time)
    {
        TimeWindow? window = time switch
        {
            _ when time >= new TimeOnly(4, 0) && time < new TimeOnly(7, 30) => TimeWindow.EarlyMorning,
            _ when time >= new TimeOnly(7, 30) && time < new TimeOnly(12, 0) => TimeWindow.MidMorning,
            _ when time >= new TimeOnly(12, 0) && time < new TimeOnly(16, 30) => TimeWindow.MidAfternoon,
            _ when time >= new TimeOnly(16, 30) && time < new TimeOnly(20, 0) => TimeWindow.EarlyEvening,
            _ => null
        };

        if (window is null)
        {
            return new TimeWindowMatch(null, false,
                Assumption.Warning("This time is outside the study's survey windows (night). The CDU model cannot estimate detection for it."));
        }

        var insideExactHours = window.Value switch
        {
            TimeWindow.EarlyMorning => time.Hour == 5,
            TimeWindow.MidMorning => time.Hour == 9,
            TimeWindow.MidAfternoon => time.Hour == 14,
            TimeWindow.EarlyEvening => time.Hour == 18,
            _ => false
        };

        var note = insideExactHours
            ? null
            : Assumption.Warning(
                $"{time:HH\\:mm} is mapped to the nearest study window ({Label(window.Value)}, flown {StudyHours(window.Value)}). Accuracy is lower outside the exact study hours.");

        return new TimeWindowMatch(window, insideExactHours, note);
    }
}

public sealed record TimeWindowMatch(TimeWindow? Window, bool InsideStudyHours, Assumption? Note);
