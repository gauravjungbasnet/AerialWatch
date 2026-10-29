using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Surveys.Models;

public sealed record CreateSurveyRequest(
    string? Name,
    string? SiteName,
    string? SurveyDate,
    string? StartTime,
    CameraType Camera,
    Season? Season = null,
    double? AltitudeM = null,
    double? AreaKm2 = null,
    GroundCover GroundCover = GroundCover.Unknown,
    string? Notes = null);
