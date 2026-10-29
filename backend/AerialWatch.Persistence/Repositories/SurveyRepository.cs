using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Abstractions;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Repositories;

public sealed class SurveyRepository : EfRepository<Survey>, ISurveyRepository
{
    public SurveyRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public async Task<IReadOnlyList<SurveyListItem>> ListWithImageCountsAsync(CancellationToken ct = default)
    {
        var rows = await Set.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { Survey = s, Count = s.Images.Count })
            .ToListAsync(ct);
        return rows.Select(r => new SurveyListItem(r.Survey, r.Count)).ToList();
    }

    public Task<int> CountImagesAsync(Guid surveyId, CancellationToken ct = default) =>
        Db.SurveyImages.CountAsync(i => i.SurveyId == surveyId, ct);
}
