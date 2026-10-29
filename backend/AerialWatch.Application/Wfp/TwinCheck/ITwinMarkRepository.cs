using AerialWatch.Application.Common;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.TwinCheck;

public interface ITwinMarkRepository : IRepository<TwinMark>
{
    /// <summary>Marks on one counted photo (checked pigs and twin sightings), oldest first, with their detection (read-only).</summary>
    Task<IReadOnlyList<TwinMark>> ListByImageAsync(Guid imageId, CancellationToken ct = default);

    /// <summary>All marks in a survey, with their detection (read-only): the matching points for suggestions and the summary.</summary>
    Task<IReadOnlyList<TwinMark>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>The mark of one detection, tracked for update; null if not checked yet.</summary>
    Task<TwinMark?> GetByDetectionAsync(Guid detectionId, CancellationToken ct = default);
}
