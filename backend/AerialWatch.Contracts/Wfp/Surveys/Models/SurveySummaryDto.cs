using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Surveys.Models;

public sealed record SurveySummaryDto(
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
    DateTimeOffset CreatedAt);
