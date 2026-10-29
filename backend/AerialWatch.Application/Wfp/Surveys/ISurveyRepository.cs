using AerialWatch.Application.Common;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Surveys;

public interface ISurveyRepository : IRepository<Survey>
{
    /// <summary>Filter, sort and page in the database; returns the page with photo counts and the total match count.</summary>
    Task<SurveySearchResult> SearchAsync(SurveySearch search, CancellationToken ct = default);

    Task<SurveyStats> GetStatsAsync(CancellationToken ct = default);

    Task<int> CountImagesAsync(Guid surveyId, CancellationToken ct = default);
}
