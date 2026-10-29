using AerialWatch.Contracts.Wfp.Results.Models;

namespace AerialWatch.Contracts.Wfp.Results.Services;

public interface ISurveyResultsService
{
    Task<SurveyResultsDto> GetAsync(Guid surveyId, CancellationToken ct = default);
}
