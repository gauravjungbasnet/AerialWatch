using AerialWatch.Application.Common;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Review;

public interface IDetectionRepository : IRepository<AnimalDetection>
{
    /// <summary>Boxes on one photo, largest-confidence AI boxes first, then ranger boxes by creation time (read-only).</summary>
    Task<IReadOnlyList<AnimalDetection>> ListByImageAsync(Guid imageId, CancellationToken ct = default);

    /// <summary>Box counts per photo, source and status for a whole survey, in one grouped query.</summary>
    Task<IReadOnlyList<DetectionTally>> TallyBySurveyAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>Pending AI boxes in the survey with confidence ≥ <paramref name="minConfidence"/>, tracked for update.</summary>
    Task<IReadOnlyList<AnimalDetection>> ListPendingAiAsync(Guid surveyId, double minConfidence, CancellationToken ct = default);
}
