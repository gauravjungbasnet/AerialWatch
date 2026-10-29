using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.Infrastructure.Persistence.Common;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class SurveyImageRepository : EfRepository<SurveyImage>, ISurveyImageRepository
{
    public SurveyImageRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public async Task<IReadOnlyList<SurveyImage>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(i => i.SurveyId == surveyId)
            .OrderBy(i => i.TakenAt == null)   // photos with a capture time first…
            .ThenBy(i => i.TakenAt)            // …in flight order
            .ThenBy(i => i.FileName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SurveyImage>> ListByPairKeyAsync(Guid surveyId, string pairKey, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(i => i.SurveyId == surveyId && i.PairKey == pairKey).ToListAsync(ct);

    public async Task<IReadOnlyCollection<string>> ListHashesAsync(Guid surveyId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(i => i.SurveyId == surveyId).Select(i => i.Sha256).ToListAsync(ct);
}
