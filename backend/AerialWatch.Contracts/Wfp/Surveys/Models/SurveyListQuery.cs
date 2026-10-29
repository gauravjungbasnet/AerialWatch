using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Surveys.Models;

/// <summary>
/// Filters, sort and page for GET surveys/list, all optional, e.g.
/// ?search=north&amp;camera=thermal&amp;status=draft&amp;sort=surveyDate&amp;dir=desc&amp;page=2&amp;pageSize=20
/// </summary>
public sealed record SurveyListQuery
{
    /// <summary>Text contained in the survey name or site (case-insensitive).</summary>
    public string? Search { get; init; }

    /// <summary>Flight date, yyyy-MM-dd.</summary>
    public string? Date { get; init; }

    public CameraType? Camera { get; init; }
    public SurveyStatus? Status { get; init; }

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Rows per page, 1–100.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>name, siteName, surveyDate, status, imageCount or createdAt (default: newest first).</summary>
    public string? Sort { get; init; }

    /// <summary>asc or desc (default asc when Sort is given).</summary>
    public string? Dir { get; init; }
}
