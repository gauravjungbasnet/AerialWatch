using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.Infrastructure.Persistence.Common;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class SurveyRepository : EfRepository<Survey>, ISurveyRepository
{
    public SurveyRepository(AerialWatchDbContext db) : base(db)
    {
    }

    /// <summary>One COUNT query and one page query; filters, sort and paging all run in SQL Server.</summary>
    public async Task<SurveySearchResult> SearchAsync(SurveySearch search, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking();

        if (search.Text is { } text)
            query = query.Where(s => s.Name.Contains(text) || s.SiteName.Contains(text)); // LIKE; case-insensitive collation
        if (search.Date is { } date)
            query = query.Where(s => s.SurveyDate == date);
        if (search.Camera is { } camera)
            query = query.Where(s => s.Camera == camera);
        if (search.Status is { } status)
            query = query.Where(s => s.Status == status);

        var total = await query.CountAsync(ct);

        var ordered = search.Sort switch
        {
            SurveySortField.Name => By(query, s => s.Name, search.Descending),
            SurveySortField.SiteName => By(query, s => s.SiteName, search.Descending),
            SurveySortField.SurveyDate => By(query, s => s.SurveyDate, search.Descending).ThenBy(s => s.StartTime),
            SurveySortField.Status => By(query, s => s.Status, search.Descending),
            SurveySortField.ImageCount => By(query, s => s.Images.Count, search.Descending),
            _ => By(query, s => s.CreatedAt, search.Descending),
        };

        var rows = await ordered
            .ThenByDescending(s => s.CreatedAt).ThenBy(s => s.Id) // stable order so pages never overlap
            .Skip(search.Skip)
            .Take(search.Take)
            .Select(s => new { Survey = s, Count = s.Images.Count })
            .ToListAsync(ct);

        return new SurveySearchResult(rows.Select(r => new SurveyListItem(r.Survey, r.Count)).ToList(), total);
    }

    public async Task<SurveyStats> GetStatsAsync(CancellationToken ct = default) =>
        new(
            await Set.CountAsync(ct),
            await Set.CountAsync(s => s.Status == SurveyStatus.InReview, ct),
            await Db.SurveyImages.CountAsync(ct));

    public Task<int> CountImagesAsync(Guid surveyId, CancellationToken ct = default) =>
        Db.SurveyImages.CountAsync(i => i.SurveyId == surveyId, ct);

    private static IOrderedQueryable<Survey> By<TKey>(IQueryable<Survey> query, Expression<Func<Survey, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
