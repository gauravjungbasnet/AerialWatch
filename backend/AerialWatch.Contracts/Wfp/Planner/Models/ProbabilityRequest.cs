using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Planner.Models;

/// <summary>Request to compute detection probability. Provide either Window or Time, and either Season or Date.</summary>
public sealed record ProbabilityRequest(
    CameraType Camera,
    TimeWindow? Window,
    string? Time,
    Season? Season,
    string? Date,
    DetectionModelKind Model = DetectionModelKind.TimeSeason,
    double? AmbientTemperatureC = null,
    double? CanopyProportion = null);
