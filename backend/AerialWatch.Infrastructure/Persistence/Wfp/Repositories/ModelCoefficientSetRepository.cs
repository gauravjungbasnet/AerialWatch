using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Infrastructure.Persistence.Common;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class ModelCoefficientSetRepository : EfRepository<ModelCoefficientSet>, IModelCoefficientSetRepository
{
    public ModelCoefficientSetRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public Task<ModelCoefficientSet?> GetLatestAsync(CancellationToken ct = default) =>
        WithValues().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ModelCoefficientSet>> ListHistoryAsync(int take, CancellationToken ct = default) =>
        await WithValues().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(take).ToListAsync(ct);

    /// <summary>Sets with their values and each value's coefficient type (needed to rebuild the parameters).</summary>
    private IQueryable<ModelCoefficientSet> WithValues() =>
        Set.AsNoTracking().Include(x => x.Values).ThenInclude(v => v.CoefficientType).AsSplitQuery();
}
