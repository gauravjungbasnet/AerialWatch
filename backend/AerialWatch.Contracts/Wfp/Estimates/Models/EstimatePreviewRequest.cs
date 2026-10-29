using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Estimates.Models;

/// <summary>Preview a corrected population estimate from a confirmed count and survey conditions.</summary>
public sealed record EstimatePreviewRequest(
    int ConfirmedCount,
    CameraType Camera,
    string SurveyDate,
    string StartTime,
    Season? Season = null,
    double? AreaKm2 = null,
    DetectionModelKind Model = DetectionModelKind.TimeSeason,
    double? AmbientTemperatureC = null,
    double? CanopyProportion = null);
