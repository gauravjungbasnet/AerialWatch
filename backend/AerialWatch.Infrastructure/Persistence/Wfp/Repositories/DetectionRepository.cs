using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.Infrastructure.Persistence.Common;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class DetectionRepository : EfRepository<AnimalDetection>, IDetectionRepository
{
    public DetectionRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public async Task<IReadOnlyList<AnimalDetection>> ListByImageAsync(Guid imageId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(d => d.ImageId == imageId)
            .OrderBy(d => d.Source)                    // AI suggestions first ("Ai" < "Ranger")…
            .ThenByDescending(d => d.Confidence)       // …most confident first
            .ThenBy(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DetectionTally>> TallyBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(d => d.Image!.SurveyId == surveyId)
            .GroupBy(d => new { d.ImageId, d.Source, d.Status })
            .Select(g => new DetectionTally(g.Key.ImageId, g.Key.Source, g.Key.Status, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AnimalDetection>> ListPendingAiAsync(Guid surveyId, double minConfidence, CancellationToken ct = default) =>
        await Set
            .Where(d => d.Image!.SurveyId == surveyId
                && d.Source == DetectionSource.Ai
                && d.Status == DetectionStatus.Pending
                && d.Confidence >= minConfidence)
            .ToListAsync(ct);
}
