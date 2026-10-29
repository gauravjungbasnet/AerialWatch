using AerialWatch.Application.Common;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.SurveyImages;

public interface ISurveyImageRepository : IRepository<SurveyImage>
{
    /// <summary>Photos of a survey, by time taken then file name.</summary>
    Task<IReadOnlyList<SurveyImage>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>Photos of a survey with the given pair key (a photo and its thermal/colour twin, D-039), read-only.</summary>
    Task<IReadOnlyList<SurveyImage>> ListByPairKeyAsync(Guid surveyId, string pairKey, CancellationToken ct = default);

    /// <summary>SHA-256 hashes already stored for the survey (duplicate check).</summary>
    Task<IReadOnlyCollection<string>> ListHashesAsync(Guid surveyId, CancellationToken ct = default);
}
