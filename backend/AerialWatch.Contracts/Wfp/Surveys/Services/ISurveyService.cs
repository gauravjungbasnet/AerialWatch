using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Surveys.Models;

namespace AerialWatch.Contracts.Wfp.Surveys.Services;

public interface ISurveyService
{
    /// <summary>One page of surveys matching the filters, sorted (server-side paging).</summary>
    Task<PagedResult<SurveySummaryDto>> ListAsync(SurveyListQuery query, CancellationToken ct = default);

    /// <summary>Totals for the dashboard.</summary>
    Task<SurveyStatsDto> GetStatsAsync(CancellationToken ct = default);

    Task<SurveyDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<SurveyDetailDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
