using System.Globalization;
using AerialWatch.Application.Common;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.Common;

/// <summary>Turns user inputs (window/time, season/date) into research-model inputs, collecting errors and notes.</summary>
public static class ConditionsResolver
{
    public static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, new[] { "HH:mm", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);

    public static Season? ResolveSeason(Season? season, string? date, ValidationErrors errors, List<Assumption> notes, string dateField = "date")
    {
        if (season is not null) return season;
        if (date is null)
        {
            errors.Add(dateField, "Provide either season or a date (yyyy-MM-dd).");
            return null;
        }
        if (!TryParseDate(date, out var d))
        {
            errors.Add(dateField, "Date must be in yyyy-MM-dd format.");
            return null;
        }
        var s = SurveyConditions.SeasonFromDate(d);
        notes.Add(Assumption.Info($"Season set to {s.ToString().ToLowerInvariant()} from the date (wet = Nov–Apr, dry = May–Oct)."));
        return s;
    }

    /// <summary>Resolves a study window. When <paramref name="required"/> is false, night times return null with a warning instead of an error.</summary>
    public static TimeWindow? ResolveWindow(TimeWindow? window, string? time, ValidationErrors errors, List<Assumption> notes,
        string timeField = "time", bool required = true)
    {
        if (window is not null) return window;
        if (time is null)
        {
            errors.Add(timeField, "Provide either window or a time (HH:mm).");
            return null;
        }
        if (!TryParseTime(time, out var t))
        {
            errors.Add(timeField, "Time must be in HH:mm format.");
            return null;
        }
        var match = SurveyConditions.MatchTime(t);
        if (match.Note is not null) notes.Add(match.Note);
        if (match.Window is null && required)
            errors.Add(timeField, match.Note!.Message);
        return match.Window;
    }

    /// <summary>Runs the requested published model; adds errors and returns null if inputs are invalid.</summary>
    public static DetectionProbabilityResult? Compute(
        DetectionModelKind model, CameraType camera, TimeWindow? window, Season? season,
        double? temperatureC, double? canopy, ValidationErrors errors, DetectionModelParameters? parameters = null)
    {
        if (model == DetectionModelKind.TimeSeason)
        {
            if (window is null || season is null) return null;
            return DetectionProbabilityModel.TimeSeason(window.Value, season.Value, camera, parameters);
        }

        if (canopy is null) errors.Add("canopyProportion", "Canopy proportion (0–1) is required for the environment model.");
        else if (canopy is < 0 or > 1) errors.Add("canopyProportion", "Canopy proportion must be between 0 and 1.");

        if (camera == CameraType.Thermal && temperatureC is null)
            errors.Add("ambientTemperatureC", "Ambient temperature is required for the thermal environment model.");
        else if (temperatureC is < 0 or > 50)
            errors.Add("ambientTemperatureC", "Ambient temperature must be between 0 and 50 °C.");

        if (errors.HasErrors) return null;
        return DetectionProbabilityModel.Environment(camera, canopy!.Value, temperatureC, parameters);
    }
}
