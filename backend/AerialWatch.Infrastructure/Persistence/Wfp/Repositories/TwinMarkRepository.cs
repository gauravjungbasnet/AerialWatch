using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.TwinCheck;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.Infrastructure.Persistence.Common;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class TwinMarkRepository : EfRepository<TwinMark>, ITwinMarkRepository
{
    public TwinMarkRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public async Task<IReadOnlyList<TwinMark>> ListByImageAsync(Guid imageId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Include(m => m.Detection).Where(m => m.CountedImageId == imageId)
            .OrderBy(m => m.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<TwinMark>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Include(m => m.Detection)
            .Where(m => Db.SurveyImages.Any(i => i.Id == m.CountedImageId && i.SurveyId == surveyId))
            .ToListAsync(ct);

    public Task<TwinMark?> GetByDetectionAsync(Guid detectionId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(m => m.DetectionId == detectionId, ct);
}
