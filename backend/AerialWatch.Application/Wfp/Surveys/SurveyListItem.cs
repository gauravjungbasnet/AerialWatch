using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Surveys;

/// <summary>A survey with its photo count, for list views (avoids loading every image).</summary>
public sealed record SurveyListItem(Survey Survey, int ImageCount);
