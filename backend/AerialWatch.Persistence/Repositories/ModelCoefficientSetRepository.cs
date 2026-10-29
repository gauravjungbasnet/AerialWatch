using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Abstractions;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Repositories;

public sealed class ModelCoefficientSetRepository : EfRepository<ModelCoefficientSet>, IModelCoefficientSetRepository
{
    public ModelCoefficientSetRepository(AerialWatchDbContext db) : base(db)
    {
    }

    public Task<ModelCoefficientSet?> GetLatestAsync(CancellationToken ct = default) =>
        Set.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ModelCoefficientSet>> ListHistoryAsync(int take, CancellationToken ct = default) =>
        await Set.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(take).ToListAsync(ct);
}
