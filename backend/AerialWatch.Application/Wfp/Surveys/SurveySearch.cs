using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Surveys;

/// <summary>Columns the survey list can be sorted by (whitelist: the API never sorts by arbitrary text).</summary>
public enum SurveySortField
{
    CreatedAt,
    Name,
    SiteName,
    SurveyDate,
    Status,
    ImageCount
}

/// <summary>Validated list criteria handed to the repository, which turns them into one SQL query.</summary>
public sealed record SurveySearch(
    string? Text,
    DateOnly? Date,
    CameraType? Camera,
    SurveyStatus? Status,
    SurveySortField Sort,
    bool Descending,
    int Skip,
    int Take);

/// <summary>A page of surveys (with photo counts) and how many match in total.</summary>
public sealed record SurveySearchResult(IReadOnlyList<SurveyListItem> Items, int Total);

/// <summary>Dashboard totals.</summary>
public sealed record SurveyStats(int Surveys, int AwaitingReview, int Photos);
