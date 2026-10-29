using AerialWatch.Domain.Common;
using AerialWatch.Domain.Common.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Surveys.Models;

public sealed record SurveyDetailDto(
    Guid Id,
    string Name,
    string SiteName,
    string SurveyDate,
    string StartTime,
    CameraType Camera,
    Season Season,
    TimeWindow? TimeWindow,
    SurveyStatus Status,
    int ImageCount,
    DateTimeOffset CreatedAt,
    double? AltitudeM,
    double? AreaKm2,
    GroundCover GroundCover,
    string? Notes,
    double? DetectionProbability,
    IReadOnlyList<Assumption> Assumptions,
    TrainingSplit? TrainingSplit = null);
