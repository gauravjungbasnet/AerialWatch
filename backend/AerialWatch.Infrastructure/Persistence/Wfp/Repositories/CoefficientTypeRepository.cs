using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

public sealed class CoefficientTypeRepository : ICoefficientTypeRepository
{
    private readonly AerialWatchDbContext _db;

    public CoefficientTypeRepository(AerialWatchDbContext db) => _db = db;

    public async Task<IReadOnlyList<CoefficientType>> ListAsync(CancellationToken ct = default) =>
        await _db.CoefficientTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(ct);
}
